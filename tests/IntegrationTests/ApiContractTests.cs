using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Challenge.IntegrationTests.Infrastructure;

namespace Challenge.IntegrationTests;

public sealed class ApiContractTests
{
    [Fact]
    public async Task GetBalance_WithNoMovements_ReturnsZero()
    {
        using var context = new ApiTestContext();

        var response = await context.Client.GetAsync("/api/v1/balance");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(0m, body.GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task PostCredit_ReturnsCreatedMovementAndUpdatesBalance()
    {
        using var context = new ApiTestContext();

        var response = await PostMovementAsync(context.Client, "credit", 100m);
        var movement = await response.Content.ReadFromJsonAsync<JsonElement>();
        var balance = await context.Client.GetFromJsonAsync<JsonElement>("/api/v1/balance");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.NotEqual(Guid.Empty, movement.GetProperty("id").GetGuid());
        Assert.Equal("credit", movement.GetProperty("type").GetString());
        Assert.Equal(100m, movement.GetProperty("amount").GetDecimal());
        Assert.Equal(TimeSpan.Zero, movement.GetProperty("occurredAt").GetDateTimeOffset().Offset);
        Assert.Equal(100m, balance.GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task PostDebit_WithAvailableBalance_ReturnsCreatedAndUpdatesBalance()
    {
        using var context = new ApiTestContext();
        await PostMovementAsync(context.Client, "credit", 100m);

        var response = await PostMovementAsync(context.Client, "debit", 40m);
        var balance = await context.Client.GetFromJsonAsync<JsonElement>("/api/v1/balance");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(60m, balance.GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task GetMovements_ReturnsNewestFirst()
    {
        using var context = new ApiTestContext();
        await PostMovementAsync(context.Client, "credit", 100m);
        await PostMovementAsync(context.Client, "debit", 25m);

        var history = await context.Client.GetFromJsonAsync<JsonElement[]>("/api/v1/movements");

        Assert.NotNull(history);
        Assert.Equal(2, history.Length);
        Assert.Equal("debit", history[0].GetProperty("type").GetString());
        Assert.Equal("credit", history[1].GetProperty("type").GetString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task PostMovement_WithNonPositiveAmount_ReturnsProblemDetails(decimal amount)
    {
        using var context = new ApiTestContext();

        var response = await PostMovementAsync(context.Client, "credit", amount);

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostDebit_WithInsufficientFunds_ReturnsConflictProblemDetails()
    {
        using var context = new ApiTestContext();

        var response = await PostMovementAsync(context.Client, "debit", 1m);

        await AssertProblemDetailsAsync(response, HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData("{\"type\":\"unknown\",\"amount\":10}")]
    [InlineData("{\"type\":0,\"amount\":10}")]
    [InlineData("{\"type\":1,\"amount\":10}")]
    [InlineData("{\"type\":2,\"amount\":10}")]
    [InlineData("{\"type\":\"credit\"}")]
    [InlineData("{\"amount\":10}")]
    [InlineData("{\"type\":\"credit\",\"amount\":10,\"unexpected\":true}")]
    [InlineData("{ invalid")]
    public async Task PostMovement_WithStructurallyInvalidJson_ReturnsBadRequestProblemDetails(string json)
    {
        using var context = new ApiTestContext();
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await context.Client.PostAsync("/api/v1/movements", content);

        await AssertProblemDetailsAsync(response, HttpStatusCode.BadRequest);
    }

    private static Task<HttpResponseMessage> PostMovementAsync(HttpClient client, string type, decimal amount) =>
        client.PostAsJsonAsync("/api/v1/movements", new { type, amount });

    private static async Task AssertProblemDetailsAsync(HttpResponseMessage response, HttpStatusCode status)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)status, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("title", out _));
    }
}

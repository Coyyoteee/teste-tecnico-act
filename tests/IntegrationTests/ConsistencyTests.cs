using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Challenge.IntegrationTests.Infrastructure;

namespace Challenge.IntegrationTests;

public sealed class ConsistencyTests
{
    [Fact]
    public async Task ConcurrentIncompatibleDebits_ConfirmOnlyOneAndKeepStorageConsistent()
    {
        using var context = new ApiTestContext();
        var credit = await context.Client.PostAsJsonAsync(
            "/api/v1/movements", new { type = "credit", amount = 100m });
        Assert.Equal(HttpStatusCode.Created, credit.StatusCode);

        var responses = await Task.WhenAll(
            context.Client.PostAsJsonAsync("/api/v1/movements", new { type = "debit", amount = 80m }),
            context.Client.PostAsJsonAsync("/api/v1/movements", new { type = "debit", amount = 80m }));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        var balance = await context.Client.GetFromJsonAsync<JsonElement>("/api/v1/balance");
        var history = await context.Client.GetFromJsonAsync<JsonElement[]>("/api/v1/movements");
        Assert.Equal(20m, balance.GetProperty("balance").GetDecimal());
        Assert.NotNull(history);
        Assert.Equal(2, history.Length);
        Assert.Single(history, movement => movement.GetProperty("type").GetString() == "debit");

        await using var stream = File.OpenRead(context.StoragePath);
        var persisted = await JsonSerializer.DeserializeAsync<JsonElement[]>(stream);
        Assert.NotNull(persisted);
        Assert.Equal(2, persisted.Length);
    }
}

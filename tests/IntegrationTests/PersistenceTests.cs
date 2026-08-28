using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Challenge.IntegrationTests.Infrastructure;

namespace Challenge.IntegrationTests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task Movements_SurviveApplicationRestart()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"challenge-restart-tests-{Guid.NewGuid():N}");
        var storagePath = Path.Combine(directory, "movements.json");
        try
        {
            using (var firstFactory = new ChallengeApiFactory(storagePath))
            using (var firstClient = firstFactory.CreateClient())
            {
                var created = await firstClient.PostAsJsonAsync(
                    "/api/v1/movements", new { type = "credit", amount = 125m });
                Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            }

            using var secondFactory = new ChallengeApiFactory(storagePath);
            using var secondClient = secondFactory.CreateClient();
            var balance = await secondClient.GetFromJsonAsync<JsonElement>("/api/v1/balance");
            var history = await secondClient.GetFromJsonAsync<JsonElement[]>("/api/v1/movements");

            Assert.Equal(125m, balance.GetProperty("balance").GetDecimal());
            Assert.NotNull(history);
            Assert.Single(history);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task CorruptedStorage_ReturnsSafeProblemDetailsAndDoesNotOverwriteFile()
    {
        using var context = new ApiTestContext();
        Directory.CreateDirectory(context.DirectoryPath);
        const string corrupted = "{ invalid";
        await File.WriteAllTextAsync(context.StoragePath, corrupted);

        var response = await context.Client.GetAsync("/api/v1/balance");
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(500, problem.GetProperty("status").GetInt32());
        Assert.DoesNotContain(context.StoragePath, await response.Content.ReadAsStringAsync());
        Assert.Equal(corrupted, await File.ReadAllTextAsync(context.StoragePath));
    }
}

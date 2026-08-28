using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Challenge.IntegrationTests;

public sealed class SpaFallbackTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), $"challenge-spa-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Fallback_PreservesApiStatusCodesAndServesFrontendRoute()
    {
        var webRoot = Path.Combine(_directory, "wwwroot");
        Directory.CreateDirectory(webRoot);
        await File.WriteAllTextAsync(Path.Combine(webRoot, "index.html"), "<html>challenge spa</html>");

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseWebRoot(webRoot);
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Storage:FilePath"] = Path.Combine(_directory, "movements.json")
                    });
                });
            });
        using var client = factory.CreateClient();

        var frontendResponse = await client.GetAsync("/dashboard");
        var unknownApiResponse = await client.GetAsync("/api/v1/unknown");
        var unsupportedBalanceMethodResponse = await client.PostAsync("/api/v1/balance", null);
        var unsupportedMovementsMethodResponse = await client.PutAsync("/api/v1/movements", null);

        Assert.Equal(HttpStatusCode.OK, frontendResponse.StatusCode);
        Assert.Equal("text/html", frontendResponse.Content.Headers.ContentType?.MediaType);
        Assert.Contains("challenge spa", await frontendResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, unknownApiResponse.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, unsupportedBalanceMethodResponse.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, unsupportedMovementsMethodResponse.StatusCode);
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Challenge.IntegrationTests.Infrastructure;

internal sealed class ChallengeApiFactory : WebApplicationFactory<Program>
{
    public ChallengeApiFactory(string storagePath)
    {
        StoragePath = storagePath;
    }

    public string StoragePath { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:FilePath"] = StoragePath
            });
        });
    }
}

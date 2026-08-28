namespace Challenge.IntegrationTests.Infrastructure;

internal sealed class ApiTestContext : IDisposable
{
    public ApiTestContext(string? storagePath = null)
    {
        DirectoryPath = Path.Combine(Path.GetTempPath(), $"challenge-api-tests-{Guid.NewGuid():N}");
        StoragePath = storagePath ?? Path.Combine(DirectoryPath, "movements.json");
        Factory = new ChallengeApiFactory(StoragePath);
        Client = Factory.CreateClient();
    }

    public string DirectoryPath { get; }
    public string StoragePath { get; }
    public ChallengeApiFactory Factory { get; }
    public HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        if (Directory.Exists(DirectoryPath))
        {
            Directory.Delete(DirectoryPath, recursive: true);
        }
    }
}

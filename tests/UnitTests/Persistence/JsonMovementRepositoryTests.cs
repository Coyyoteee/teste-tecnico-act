using Challenge.Api.Domain;
using Challenge.Api.Persistence;
using Challenge.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Challenge.UnitTests.Persistence;

public sealed class JsonMovementRepositoryTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"challenge-tests-{Guid.NewGuid():N}");

    [Fact]
    public async Task GetAllAsync_WhenFileDoesNotExist_ReturnsEmptyHistory()
    {
        var repository = CreateRepository();

        var movements = await repository.GetAllAsync();

        Assert.Empty(movements);
    }

    [Fact]
    public async Task AddAsync_CreatesDirectoryAndPersistsRoundTrip()
    {
        var repository = CreateRepository();
        var movement = Credit(100m);

        await repository.AddAsync(movement);
        var stored = Assert.Single(await repository.GetAllAsync());

        Assert.Equal(movement.Id, stored.Id);
        Assert.Equal(movement.Type, stored.Type);
        Assert.Equal(movement.Amount, stored.Amount);
        Assert.Equal(movement.OccurredAt, stored.OccurredAt);
    }

    [Fact]
    public async Task AddAsync_AppendsMovements()
    {
        var repository = CreateRepository();

        await repository.AddAsync(Credit(100m));
        await repository.AddAsync(Credit(50m));

        Assert.Equal(2, (await repository.GetAllAsync()).Count);
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidJson_ThrowsWithoutOverwritingFile()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "movements.json");
        const string invalidJson = "{ invalid";
        await File.WriteAllTextAsync(path, invalidJson);
        var repository = CreateRepository(path);

        await Assert.ThrowsAsync<InvalidDataException>(() => repository.GetAllAsync());

        Assert.Equal(invalidJson, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyFile_ThrowsWithoutOverwritingFile()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "movements.json");
        await File.WriteAllTextAsync(path, string.Empty);
        var repository = CreateRepository(path);

        await Assert.ThrowsAsync<InvalidDataException>(() => repository.GetAllAsync());

        Assert.Equal(string.Empty, await File.ReadAllTextAsync(path));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("2")]
    public async Task GetAllAsync_WithNumericMovementType_ThrowsWithoutOverwritingFile(string type)
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "movements.json");
        var json = $$"""
            [
              {
                "id": "{{Guid.NewGuid()}}",
                "type": {{type}},
                "amount": 10,
                "occurredAt": "2026-08-26T15:30:00Z"
              }
            ]
            """;
        await File.WriteAllTextAsync(path, json);
        var repository = CreateRepository(path);

        await Assert.ThrowsAsync<InvalidDataException>(() => repository.GetAllAsync());

        Assert.Equal(json, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyMovementId_ThrowsWithoutOverwritingFile()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "movements.json");
        var json = """
            [
              {
                "id": "00000000-0000-0000-0000-000000000000",
                "type": "credit",
                "amount": 10,
                "occurredAt": "2026-08-26T15:30:00Z"
              }
            ]
            """;
        await File.WriteAllTextAsync(path, json);
        var repository = CreateRepository(path);

        await Assert.ThrowsAsync<InvalidDataException>(() => repository.GetAllAsync());

        Assert.Equal(json, await File.ReadAllTextAsync(path));
    }

    [Fact]
    public async Task AddAsync_WhenCancelled_DoesNotChangeExistingFile()
    {
        var path = Path.Combine(_directory, "movements.json");
        var repository = CreateRepository(path);
        await repository.AddAsync(Credit(100m));
        var original = await File.ReadAllTextAsync(path);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.AddAsync(Credit(50m), source.Token));

        Assert.Equal(original, await File.ReadAllTextAsync(path));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    private JsonMovementRepository CreateRepository(string? path = null)
    {
        path ??= Path.Combine(_directory, "nested", "movements.json");
        return new JsonMovementRepository(
            Options.Create(new JsonMovementRepositoryOptions { FilePath = path }),
            new TestHostEnvironment(_directory),
            NullLogger<JsonMovementRepository>.Instance);
    }

    private static Movement Credit(decimal amount) =>
        new(
            Guid.NewGuid(),
            MovementType.Credit,
            amount,
            new DateTimeOffset(2026, 8, 26, 15, 30, 0, TimeSpan.Zero));
}

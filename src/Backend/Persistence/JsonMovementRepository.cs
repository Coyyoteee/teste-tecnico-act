using System.Text.Json;
using System.Text.Json.Serialization;
using Challenge.Api.Domain;
using Challenge.Api.Exceptions;
using Microsoft.Extensions.Options;

namespace Challenge.Api.Persistence;

public sealed class JsonMovementRepository : IMovementRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
        }
    };

    private readonly string _filePath;
    private readonly ILogger<JsonMovementRepository> _logger;

    public JsonMovementRepository(
        IOptions<JsonMovementRepositoryOptions> options,
        IHostEnvironment environment,
        ILogger<JsonMovementRepository> logger)
    {
        var configuredPath = options.Value.FilePath;
        _filePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath);
        _logger = logger;
    }

    public async Task<IReadOnlyCollection<Movement>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<Movement>();
        }

        try
        {
            await using var stream = new FileStream(
                _filePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                bufferSize: 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);

            if (stream.Length == 0)
            {
                throw new InvalidDataException("The movement storage file is empty.");
            }

            return await JsonSerializer.DeserializeAsync<List<Movement>>(stream, SerializerOptions, cancellationToken)
                ?? throw new InvalidDataException("The movement storage does not contain a JSON collection.");
        }
        catch (JsonException exception)
        {
            _logger.LogError(exception, "The movement storage contains invalid JSON.");
            throw new InvalidDataException("The movement storage contains invalid JSON.", exception);
        }
        catch (InvalidAmountException exception)
        {
            _logger.LogError(exception, "The movement storage contains invalid movement data.");
            throw new InvalidDataException("The movement storage contains invalid movement data.", exception);
        }
        catch (ArgumentException exception)
        {
            _logger.LogError(exception, "The movement storage contains invalid movement data.");
            throw new InvalidDataException("The movement storage contains invalid movement data.", exception);
        }
        catch (InvalidDataException exception)
        {
            _logger.LogError(exception, "The movement storage contains invalid data.");
            throw;
        }
        catch (IOException exception)
        {
            _logger.LogError(exception, "Unable to read the movement storage.");
            throw;
        }
    }

    public async Task AddAsync(Movement movement, CancellationToken cancellationToken = default)
    {
        var movements = (await GetAllAsync(cancellationToken)).ToList();
        movements.Add(movement);

        var directory = Path.GetDirectoryName(_filePath)
            ?? throw new InvalidOperationException("The movement storage path has no directory.");
        Directory.CreateDirectory(directory);

        var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                bufferSize: 4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, movements, SerializerOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, _filePath, overwrite: true);
        }
        catch (IOException exception)
        {
            _logger.LogError(exception, "Unable to write the movement storage.");
            throw;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException exception)
                {
                    _logger.LogWarning(exception, "Unable to remove a temporary movement storage file.");
                }
            }
        }
    }
}

using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;

namespace QueryCat.Backend.Auth;

/// <summary>
/// Stores authentication tokens in JSON file that is readable only by the current user.
/// The file is re-read on every call, so changes from other processes are visible.
/// </summary>
public sealed class FileAuthTokenStore : IAuthTokenStore, IDisposable
{
    private readonly string _file;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(FileAuthTokenStore));

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="file">Tokens file.</param>
    public FileAuthTokenStore(string file)
    {
        _file = file;
    }

    /// <inheritdoc />
    public async Task<StoredAuthToken?> GetAsync(string providerId, CancellationToken cancellationToken = default)
    {
        var tokens = await GetAllAsync(cancellationToken);
        return tokens.GetValueOrDefault(providerId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<string, StoredAuthToken>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            return await ReadAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task SetAsync(string providerId, StoredAuthToken token, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var tokens = await ReadAsync(cancellationToken);
            token.UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            tokens[providerId] = token;
            await WriteAsync(tokens, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<bool> RemoveAsync(string providerId, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var tokens = await ReadAsync(cancellationToken);
            if (!tokens.Remove(providerId))
            {
                return false;
            }
            await WriteAsync(tokens, cancellationToken);
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<Dictionary<string, StoredAuthToken>> ReadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_file))
        {
            return new Dictionary<string, StoredAuthToken>(StringComparer.OrdinalIgnoreCase);
        }
        await using var stream = File.OpenRead(_file);
        try
        {
            var tokens = await JsonSerializer.DeserializeAsync(stream, SourceGenerationContext.Default.DictionaryStringStoredAuthToken,
                cancellationToken);
            return new Dictionary<string, StoredAuthToken>(tokens ?? [], StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException e)
        {
            var targetBakFile = _file + ".bak";
            File.Copy(_file, targetBakFile, true);
            _logger.LogWarning(
                "File '{File}' is not a valid JSON file. No auth tokens loaded. The backup file is '{TargetFile}'. Error: {Error}",
                _file, targetBakFile, e.Message);
            return new Dictionary<string, StoredAuthToken>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task WriteAsync(Dictionary<string, StoredAuthToken> tokens, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_file);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write to temp file first and then replace, so the file is never partially written.
        var tempFile = _file + ".tmp";
        File.Delete(tempFile);
        var options = new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
        };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }
        await using (var stream = new FileStream(tempFile, options))
        {
            await JsonSerializer.SerializeAsync(stream, tokens, SourceGenerationContext.Default.DictionaryStringStoredAuthToken,
                cancellationToken);
        }
        File.Move(tempFile, _file, overwrite: true);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _semaphore.Dispose();
    }
}

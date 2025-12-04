namespace QueryCat.Backend.AssemblyPlugins;

internal sealed class FilePluginLoadStrategy : IPluginLoadStrategy
{
    private readonly string _file;

    public FilePluginLoadStrategy(string file)
    {
        _file = file;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<string>> GetAllFilesAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_file);
        if (string.IsNullOrEmpty(directory))
        {
            return Task.FromResult<IReadOnlyCollection<string>>([]);
        }

        var files = Directory.GetFiles(directory, string.Empty, SearchOption.AllDirectories);
        return Task.FromResult<IReadOnlyCollection<string>>(files);
    }

    /// <inheritdoc />
    public Task<Stream> GetFileAsync(string file, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(file))
        {
            return Task.FromResult(Stream.Null);
        }
        return Task.FromResult<Stream>(File.OpenRead(file));
    }

    /// <inheritdoc />
    public Task<long> GetFileSizeAsync(string file, CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(file);
        var filesSize = fileInfo.Exists ? file.Length : 0;
        return Task.FromResult<long>(filesSize);
    }
}

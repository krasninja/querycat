namespace QueryCat.Backend.AssemblyPlugins;

internal sealed class FilePluginLoadStrategy : IPluginLoadStrategy
{
    private readonly string _file;
    private readonly string _directory;

    public FilePluginLoadStrategy(string file)
    {
        _file = file;
        _directory = Path.GetDirectoryName(file) ?? string.Empty;
    }

    /// <inheritdoc />
    public Task<IReadOnlyCollection<string>> GetAllFilesAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_directory))
        {
            return Task.FromResult<IReadOnlyCollection<string>>([]);
        }

        var files = Directory.GetFiles(_directory, "*", SearchOption.AllDirectories);
        return Task.FromResult<IReadOnlyCollection<string>>(files);
    }

    /// <inheritdoc />
    public Task<Stream> GetFileAsync(string file, CancellationToken cancellationToken = default)
    {
        file = ResolvePath(file);
        if (!File.Exists(file))
        {
            return Task.FromResult(Stream.Null);
        }
        return Task.FromResult<Stream>(File.OpenRead(file));
    }

    /// <inheritdoc />
    public Task<long> GetFileSizeAsync(string file, CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(ResolvePath(file));
        var filesSize = fileInfo.Exists ? fileInfo.Length : 0;
        return Task.FromResult(filesSize);
    }

    private string ResolvePath(string file)
        => Path.IsPathRooted(file) || string.IsNullOrEmpty(_directory) ? file : Path.Combine(_directory, file);
}

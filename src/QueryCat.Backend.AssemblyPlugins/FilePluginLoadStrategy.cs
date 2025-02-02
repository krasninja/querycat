namespace QueryCat.Backend.AssemblyPlugins;

internal sealed class FilePluginLoadStrategy : IPluginLoadStrategy
{
    private readonly string _file;

    public FilePluginLoadStrategy(string file)
    {
        _file = file;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllFiles()
    {
        var directory = Path.GetDirectoryName(_file);
        if (string.IsNullOrEmpty(directory))
        {
            return [];
        }

        return Directory.GetFiles(directory, string.Empty, SearchOption.AllDirectories);
    }

    /// <inheritdoc />
    public Stream GetFile(string file) => File.OpenRead(file);
}

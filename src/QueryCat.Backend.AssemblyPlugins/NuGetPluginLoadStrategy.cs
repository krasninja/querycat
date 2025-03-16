using System.IO.Compression;

namespace QueryCat.Backend.AssemblyPlugins;

internal sealed class NuGetPluginLoadStrategy : IPluginLoadStrategy
{
    private const string NuGetExtensions = ".nupkg";

    private readonly string _file;
    private ZipArchive? _zip;

    public NuGetPluginLoadStrategy(string file)
    {
        _file = file;
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAllFiles()
    {
        if (!Path.GetExtension(_file).Equals(NuGetExtensions, StringComparison.InvariantCultureIgnoreCase)
            || !File.Exists(_file))
        {
            yield break;
        }

        _zip = ZipFile.OpenRead(_file);
        try
        {
            foreach (var entry in _zip.Entries)
            {
                yield return entry.FullName;
            }
        }
        finally
        {
            _zip.Dispose();
            _zip = null;
        }
    }

    /// <inheritdoc />
    public Stream GetFile(string file)
    {
        var zip = _zip ?? ZipFile.OpenRead(_file);
        var entry = zip.GetEntry(file);
        return entry == null ? Stream.Null : entry.Open();
    }

    /// <inheritdoc />
    public long GetFileSize(string file)
    {
        var zip = _zip ?? ZipFile.OpenRead(_file);
        var entry = zip.GetEntry(file);
        if (entry == null)
        {
            return 0;
        }
        return entry.Length;
    }
}

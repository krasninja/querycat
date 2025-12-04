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
    public async Task<IReadOnlyCollection<string>> GetAllFilesAsync(CancellationToken cancellationToken = default)
    {
        if (!Path.GetExtension(_file).Equals(NuGetExtensions, StringComparison.InvariantCultureIgnoreCase)
            || !File.Exists(_file))
        {
            return [];
        }

        _zip = await ZipFile.OpenReadAsync(_file, cancellationToken);
        try
        {
            return _zip.Entries.Select(e => e.FullName).ToArray();
        }
        finally
        {
            await _zip.DisposeAsync();
            _zip = null;
        }
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(string file, CancellationToken cancellationToken = default)
    {
        var zip = _zip ?? await ZipFile.OpenReadAsync(_file, cancellationToken);
        var entry = zip.GetEntry(file);
        return entry == null ? Stream.Null : await entry.OpenAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> GetFileSizeAsync(string file, CancellationToken cancellationToken = default)
    {
        var zip = _zip ?? await ZipFile.OpenReadAsync(_file, cancellationToken);
        var entry = zip.GetEntry(file);
        if (entry == null)
        {
            return 0;
        }
        return entry.Length;
    }
}

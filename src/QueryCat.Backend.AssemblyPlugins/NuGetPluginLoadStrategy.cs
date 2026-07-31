using System.IO.Compression;

namespace QueryCat.Backend.AssemblyPlugins;

internal sealed class NuGetPluginLoadStrategy : IPluginLoadStrategy
{
    private const string NuGetExtensions = ".nupkg";

    private readonly string _file;

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

        var zip = await ZipFile.OpenReadAsync(_file, cancellationToken);
        try
        {
            return zip.Entries.Select(e => e.FullName).ToArray();
        }
        finally
        {
            await zip.DisposeAsync();
        }
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileAsync(string file, CancellationToken cancellationToken = default)
    {
        var zip = await ZipFile.OpenReadAsync(_file, cancellationToken);
        file = FixFilePath(file);
        var entry = zip.GetEntry(file);
        return entry == null
            ? Stream.Null
            : new ZipStreamWrapper(await entry.OpenAsync(cancellationToken), zip);
    }

    /// <inheritdoc />
    public async Task<long> GetFileSizeAsync(string file, CancellationToken cancellationToken = default)
    {
        await using var zip = await ZipFile.OpenReadAsync(_file, cancellationToken);
        file = FixFilePath(file);
        var entry = zip.GetEntry(file);
        if (entry == null)
        {
            return 0;
        }
        return entry.Length;
    }

    private static string FixFilePath(string file)
    {
        return file.Replace('\\', '/');
    }
}

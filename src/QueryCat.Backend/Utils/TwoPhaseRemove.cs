namespace QueryCat.Backend.Utils;

/// <summary>
/// The class implements files removing in two phase with ability to rollback.
/// </summary>
internal sealed class TwoPhaseRemove : IDisposable
{
    private const string DownloadExtension = "temp";

    private readonly List<(string Original, string Renamed)> _files = new();

    public bool RenameBeforeRemove { get; }

    public TwoPhaseRemove(bool renameBeforeRemove = true)
    {
        RenameBeforeRemove = renameBeforeRemove;
    }

    /// <summary>
    /// Add file to remove list.
    /// </summary>
    /// <param name="file">File path.</param>
    public void Add(string file)
    {
        if (!File.Exists(file))
        {
            throw new InvalidOperationException(
                string.Format(Resources.Errors.FileNoExists, file));
        }

        if (RenameBeforeRemove)
        {
            var fileName = Path.GetFileName(file);
            var filePath = Path.GetDirectoryName(file)!;
            var renamedFile = Path.Combine(filePath, $".{fileName}.{Guid.NewGuid():N}.{DownloadExtension}");
            File.Move(file, renamedFile);
            _files.Add((file, renamedFile));
        }
        else
        {
            _files.Add((file, file));
        }
    }

    /// <summary>
    /// Add files to remove list.
    /// </summary>
    /// <param name="files">Files.</param>
    public void AddRange(IEnumerable<string> files)
    {
        foreach (var file in files)
        {
            Add(file);
        }
    }

    /// <summary>
    /// Remove all files.
    /// </summary>
    public void Remove()
    {
        var files = _files.ToList();
        foreach (var file in files)
        {
            File.Delete(file.Renamed);
            _files.Remove(file);
        }
    }

    /// <summary>
    /// Clean up not downloaded files.
    /// </summary>
    /// <param name="path">Path.</param>
    public static void Cleanup(string path)
    {
        foreach (var file in Directory.EnumerateFiles(path, $"*.{DownloadExtension}", SearchOption.TopDirectoryOnly))
        {
            File.Delete(file);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!RenameBeforeRemove)
        {
            return;
        }

        foreach (var (original, renamed) in _files)
        {
            if (File.Exists(renamed))
            {
                File.Move(renamed, original, overwrite: true);
            }
        }
        _files.Clear();
    }
}

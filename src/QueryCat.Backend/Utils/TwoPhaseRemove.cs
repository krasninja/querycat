namespace QueryCat.Backend.Utils;

/// <summary>
/// The class implements files removing in two phase with ability to rollback.
/// </summary>
internal sealed class TwoPhaseRemove : IDisposable
{
    private const string Suffix = ".old";

    private readonly List<string> _files = new();
    private readonly Random _random = new();

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
            throw new InvalidOperationException($"File '{file}' not exists");
        }

        if (RenameBeforeRemove)
        {
            var fileName = Path.GetFileName(file);
            var filePath = Path.GetDirectoryName(file)!;
            var newFile = Path.Combine(filePath, $".{fileName}.old{_random.Next(0, 99)}");
            File.Move(file, newFile);
            _files.Add(newFile);
        }
        else
        {
            _files.Add(file);
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
        for (var i = 0; i < files.Count; i++)
        {
            File.Delete(files[i]);
            _files.RemoveAt(i);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (RenameBeforeRemove)
        {
            foreach (var file in _files)
            {
                var oldFileName = file.Substring(1, file.Length - 7);
                File.Move(file, oldFileName);
            }
        }
    }
}

namespace QueryCat.Backend.ThriftPlugins;

/// <summary>
/// IO path utilities.
/// </summary>
internal static class PathUtils
{
    /// <summary>
    /// The method expands the system PATH variable and tries to find the file within it.
    /// </summary>
    /// <param name="fileName">File name.</param>
    /// <returns><c>True</c> if file was found, <c>false</c> otherwise.</returns>
    public static bool IsExecutableExistInPath(string fileName) =>
        !string.IsNullOrEmpty(ResolveExecutableFullPath(fileName));

    /// <summary>
    /// Resolves the path to the file name according to PATH environment variable.
    /// </summary>
    /// <param name="fileName">The executable name.</param>
    /// <returns>Resolved full path or empty if not found.</returns>
    /// <example>google-chrome-stable -> /usr/bin/google-chrome-stable.</example>
    public static string ResolveExecutableFullPath(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException("Cannot find PATH environment variable.");
        }

        // Append .exe for Windows.
        if (OperatingSystem.IsWindows()
            && !".exe".Equals(Path.GetExtension(fileName), StringComparison.OrdinalIgnoreCase))
        {
            fileName += ".exe";
        }

        // Try to resolve in current executable directory.
        var candidate = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(candidate))
        {
            return candidate;
        }

        // Try to resolve within working directory.
        candidate = Path.Combine(Directory.GetCurrentDirectory(), fileName);
        if (File.Exists(candidate))
        {
            return candidate;
        }

        // Try to find in PATH.
        foreach (var pathItem in path.Split(Path.PathSeparator, StringSplitOptions.TrimEntries))
        {
            candidate = Path.Combine(pathItem, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return string.Empty;
    }
}

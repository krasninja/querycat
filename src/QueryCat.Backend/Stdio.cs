namespace QueryCat.Backend;

/// <summary>
/// Standard IO stream routines.
/// </summary>
public static class Stdio
{
    private static Stream? _outputStream;
    private static Stream? _inputStream;
#if NET9_0_OR_GREATER
    private static readonly Lock _objLock = new();
#else
    private static readonly object _objLock = new();
#endif

    /// <summary>
    /// Get the cached standard output stream.
    /// </summary>
    /// <returns>Standard output stream.</returns>
    public static Stream GetConsoleOutput()
    {
        if (_outputStream != null)
        {
            return _outputStream;
        }

        lock (_objLock)
        {
            return _outputStream = Console.OpenStandardOutput();
        }
    }

    /// <summary>
    /// Get the cached standard input stream.
    /// </summary>
    /// <returns>Standard input stream.</returns>
    internal static Stream GetConsoleInput()
    {
        if (_inputStream != null)
        {
            return _inputStream;
        }

        lock (_objLock)
        {
            return _inputStream = Console.OpenStandardInput();
        }
    }
}

namespace QueryCat.Backend;

/// <summary>
/// Standard IO stream routines.
/// </summary>
public static class Stdio
{
    private static Stream? outputStream;
    private static Stream? inputStream;
    private static readonly object ObjLock = new();

    /// <summary>
    /// Get the cached standard output stream.
    /// </summary>
    /// <returns>Standard output stream.</returns>
    public static Stream GetConsoleOutput()
    {
        if (outputStream != null)
        {
            return outputStream;
        }

        lock (ObjLock)
        {
            return outputStream = Console.OpenStandardOutput();
        }
    }

    /// <summary>
    /// Get the cached standard input stream.
    /// </summary>
    /// <returns>Standard input stream.</returns>
    internal static Stream GetConsoleInput()
    {
        if (inputStream != null)
        {
            return inputStream;
        }

        lock (ObjLock)
        {
            return inputStream = Console.OpenStandardInput();
        }
    }
}

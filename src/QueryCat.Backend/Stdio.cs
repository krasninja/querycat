namespace QueryCat.Backend;

/// <summary>
/// Standard IO stream routines.
/// </summary>
public static class Stdio
{
    private static Stream? outputStream;
    private static Stream? inputStream;
    private static readonly object ObjLock = new();

    public static Stream GetConsoleOutput()
    {
        if (outputStream != null)
        {
            return outputStream;
        }

        lock (ObjLock)
        {
            return outputStream ??= Console.OpenStandardOutput();
        }
    }

    internal static Stream CreateConsoleInput()
    {
        if (inputStream != null)
        {
            return inputStream;
        }

        lock (ObjLock)
        {
            return inputStream ??= Console.OpenStandardInput();
        }
    }
}

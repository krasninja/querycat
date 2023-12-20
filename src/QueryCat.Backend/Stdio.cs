namespace QueryCat.Backend;

public static class Stdio
{
    private static Stream? outputStream;
    private static Stream? inputStream;
    private static readonly object ObjLock = new();

    public static Stream GetConsoleOutput()
    {
        lock (ObjLock)
        {
            return outputStream ??= Console.OpenStandardOutput();
        }
    }

    internal static Stream CreateConsoleInput()
    {
        lock (ObjLock)
        {
            return inputStream ??= Console.OpenStandardInput();
        }
    }
}

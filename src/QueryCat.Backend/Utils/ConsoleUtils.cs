namespace QueryCat.Backend.Utils;

/// <summary>
/// Utils for system console.
/// </summary>
internal static class ConsoleUtils
{
    /// <summary>
    /// Reads the standard input stream to the end of line.
    /// </summary>
    /// <param name="stream">Standard stream.</param>
    /// <returns><c>True</c> if end of line reached, or <c>false</c> if there is end of stream.</returns>
    public static bool ReadToEndOfLine(Stream stream)
    {
        var isPosix = !System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows);

        var prevch = '\0';
        var arr = new byte[] { 0 };
        while (stream.Read(arr, 0, 1) > 0)
        {
            var ch = (char)arr[0];
            if ((isPosix && ch == '\n')
                || (!isPosix && prevch == '\r' && ch == '\n'))
            {
                return true;
            }
            prevch = ch;
        }
        return false;
    }
}

using System.ComponentModel;
using System.Diagnostics;

namespace QueryCat.OAuth;

/// <summary>
/// Opens URL in the default browser.
/// </summary>
public static class BrowserLauncher
{
    /// <summary>
    /// Try to open URL in the default browser.
    /// </summary>
    /// <param name="url">URL.</param>
    /// <returns>True if the browser has been started.</returns>
    public static bool TryOpen(string url)
    {
        try
        {
            // Uses xdg-open on Linux, open on macOS and shell on Windows.
            using var process = Process.Start(new ProcessStartInfo(url)
            {
                UseShellExecute = true,
            });
            return true;
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            return false;
        }
    }
}

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace QueryCat.Backend.Core.Utils;

/// <summary>
/// Simple tracer to measure execution time. Measures time between StartMethod and
/// EndMethod calls.
/// </summary>
internal static class Tracer
{
    private static readonly ConcurrentDictionary<string, long> _methods = new();

#if NET9_0_OR_GREATER
    private static readonly Lock _objLock = new();
#else
    private static readonly object _objLock = new();
#endif

    private static readonly StringBuilder _stringBuilder = new();

    /// <summary>
    /// Minimum execution time to log.
    /// </summary>
    public static int MinimumMillisecondsToLog { get; set; } = 0;

    /// <summary>
    /// Output logs to file.
    /// </summary>
    public static string File { get; set; } = string.Empty;

    /// <summary>
    /// Start measure.
    /// </summary>
    /// <param name="className">Class name.</param>
    /// <param name="methodName">Method name.</param>
    public static void StartMethod(string className, [CallerMemberName] string? methodName = null)
    {
        var key = string.Concat(className, ".", methodName);
        _methods.AddOrUpdate(key, Stopwatch.GetTimestamp(), (_, _) => Stopwatch.GetTimestamp());
    }

    /// <summary>
    /// End measure.
    /// </summary>
    /// <param name="className">Class name.</param>
    /// <param name="methodName">Method name.</param>
    public static void EndMethod(string className, [CallerMemberName] string? methodName = null)
    {
        var key = string.Concat(className, ".", methodName);
        if (_methods.TryRemove(key, out var starting))
        {
            var ending = Stopwatch.GetTimestamp();
            var ts = Stopwatch.GetElapsedTime(starting, ending);
            if (ts.TotalMilliseconds >= MinimumMillisecondsToLog)
            {
                var message = $"{DateTime.UtcNow}\t{ts}\t{key}";
                Write(message);
            }
        }
    }

    /// <summary>
    /// Write the message to the log.
    /// </summary>
    /// <param name="message">Message to write.</param>
    public static void Write(string message)
    {
        lock (_objLock)
        {
            _stringBuilder.AppendLine(message);
        }
    }

    /// <summary>
    /// Write all messages.
    /// </summary>
    public static void Flush()
    {
        if (string.IsNullOrEmpty(File))
        {
            return;
        }

        lock (_objLock)
        {
            System.IO.File.AppendAllText(File, _stringBuilder.ToString());
            _stringBuilder.Clear();
        }
    }
}

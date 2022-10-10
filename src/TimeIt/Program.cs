using System.Diagnostics;

namespace TimeIt;

/// <summary>
/// Program entry point.
/// </summary>
public class Program
{
    public static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Run the command and measure execution time.");
            Console.WriteLine("timeit <command>");
            return 0;
        }

        var process = new Process();
        process.StartInfo.FileName = args[0];
        if (args.Length > 1)
        {
            process.StartInfo.Arguments = string.Join(' ', args.Skip(1).ToArray());
        }

        GC.Collect();

        var stopwatch = new Stopwatch();
        stopwatch.Start();
        process.Start();
        process.WaitForExit();
        stopwatch.Stop();
        Console.WriteLine($"Execution time: {stopwatch.Elapsed}.");

        return 0;
    }
}

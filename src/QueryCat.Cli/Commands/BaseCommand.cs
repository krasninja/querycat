using System.CommandLine;
using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Formatters;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

internal abstract class BaseCommand : Command
{
    private static Option<LogLevel> LogLevelOption { get; } = new("--log-level")
    {
        Description = Resources.Messages.QueryCommand_LogLevelDescription,
        DefaultValueFactory = _ =>
        {
#if DEBUG
            return LogLevel.Debug;
#else
            return LogLevel.Information;
#endif
        }
    };

    private static Option<string[]> PluginDirectoriesOption { get; } = new("--plugin-dirs")
    {
        Description = Resources.Messages.QueryCommand_PluginDirectoriesCommand,
        AllowMultipleArgumentsPerToken = true,
#if !ENABLE_PLUGINS
        Hidden = true,
#endif
    };

    protected Option<TextTableOutput.Style> OutputStyleOption { get; } = new("--output-style")
    {
        Description = Resources.Messages.QueryCommand_OutputStyleDescription,
        DefaultValueFactory = _ => TextTableOutput.Style.Table1,
    };

    protected Option<string?> ColumnsSeparatorOption { get; } = new("--columns-separator")
    {
        Description = Resources.Messages.QueryCommand_ColumnsSeparatorDescription,
    };

    /// <inheritdoc />
    protected BaseCommand(string name, string? description = null) : base(name, description)
    {
        Add(LogLevelOption);
#if ENABLE_PLUGINS
        Add(PluginDirectoriesOption);
#endif
        Add(OutputStyleOption);
        Add(ColumnsSeparatorOption);
    }

    protected static ApplicationOptions GetApplicationOptions(ParseResult parseResult)
    {
        return new ApplicationOptions
        {
            LogLevel = parseResult.GetValue(LogLevelOption),
#if ENABLE_PLUGINS
            PluginDirectories = (parseResult.GetValue(PluginDirectoriesOption) ?? [])
                .SelectMany(d => d.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
                .Select(QueryCat.Backend.Functions.IOFunctions.ResolveHomeDirectory)
                .ToArray(),
#endif
        };
    }

    protected static async Task WriteAsync(IExecutionThread<ExecutionOptions> executionThread, VariantValue result,
        IRowsOutput rowsOutput, CancellationToken cancellationToken)
    {
        if (result.IsNull)
        {
            return;
        }
        var iterator = RowsIteratorConverter.Convert(result);
        if (result.Type == DataType.Object
            && result.AsObjectUnsafe is IRowsOutput alternateRowsOutput)
        {
            rowsOutput = alternateRowsOutput;
        }
        await rowsOutput.ResetAsync(cancellationToken);
        await WriteLoopAsync(executionThread.ConfigStorage, rowsOutput, iterator, executionThread.Options, cancellationToken);
        executionThread.Statistic.StopStopwatch();
    }

    private static async Task WriteLoopAsync(
        IConfigStorage configStorage,
        IRowsOutput rowsOutput,
        IRowsIterator rowsIterator,
        ExecutionOptions options,
        CancellationToken cancellationToken)
    {
        // For plain output let's adjust columns width first.
        if (rowsOutput.Options.RequiresColumnsLengthAdjust && options.AnalyzeRowsCount > 0)
        {
            rowsIterator = new AdjustColumnsLengthsIterator(rowsIterator, options.AnalyzeRowsCount);
        }
        if (options.TailCount > -1)
        {
            rowsIterator = new TailRowsIterator(rowsIterator, options.TailCount);
        }

        // Write the main data.
        var isOpened = false;
        await StartWriterLoop(cancellationToken);

        // Append grow data.
        if (options.FollowTimeout != TimeSpan.Zero)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var requestQuit = false;
                Thread.Sleep(options.FollowTimeout);
                await StartWriterLoop(cancellationToken);
                ProcessInput(ref requestQuit);
                if (requestQuit)
                {
                    break;
                }
            }
        }

        if (isOpened)
        {
            await rowsOutput.CloseAsync(cancellationToken);
        }

        async Task StartWriterLoop(CancellationToken ct)
        {
            while (await rowsIterator.MoveNextAsync(ct))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!isOpened)
                {
                    rowsOutput.QueryContext = new RowsOutputQueryContext(rowsIterator.Columns, configStorage);
                    rowsOutput.QueryContext.PrereadRowsCount = options.AnalyzeRowsCount;
                    rowsOutput.QueryContext.SkipIfNoColumns = options.SkipIfNoColumns;
                    await rowsOutput.OpenAsync(ct);
                    isOpened = true;
                }
                await rowsOutput.WriteValuesAsync(rowsIterator.Current.Values, ct);
            }
        }

        void ProcessInput(ref bool requestQuit)
        {
            if (!Environment.UserInteractive)
            {
                return;
            }
            while (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                }
                else if (key.Key == ConsoleKey.Q)
                {
                    requestQuit = true;
                }
                else if (key.Key == ConsoleKey.Subtract)
                {
                    Console.WriteLine(new string('-', 5));
                }
            }
        }
    }
}

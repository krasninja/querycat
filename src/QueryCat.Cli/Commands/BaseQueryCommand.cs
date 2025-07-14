using System.CommandLine;
using QueryCat.Backend;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Cli.Commands;

internal abstract class BaseQueryCommand : BaseCommand
{
    protected Argument<string> QueryArgument { get; } = new("query")
    {
        Description = Resources.Messages.QueryCommand_QueryDescription,
        DefaultValueFactory = _ => string.Empty,
    };

    protected Option<string[]> FilesOption { get; } = new("-f", "--files")
    {
        Description = Resources.Messages.QueryCommand_FilesDescription,
        AllowMultipleArgumentsPerToken = true,
    };

    protected Option<string[]> VariablesOption { get; } = new("--var")
    {
        Description = Resources.Messages.QueryCommand_VariablesDescription,
    };

    /// <inheritdoc />
    protected BaseQueryCommand(string name, string? description = null) : base(name, description)
    {
        Add(QueryArgument);
        Add(FilesOption);
        Add(VariablesOption);
    }

    internal static async Task RunQueryAsync(
        IExecutionThread<ExecutionOptions> executionThread,
        IRowsOutput rowsOutput,
        string? query,
        string[]? files,
        CancellationToken cancellationToken = default)
    {
        query ??= string.Empty;
        if (files != null && files.Length > 0)
        {
            foreach (var file in files.SelectMany(f => f.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)))
            {
                var text = await File.ReadAllTextAsync(file, cancellationToken);
                await RunWithPluginsInstall(executionThread, rowsOutput, text, cancellationToken: cancellationToken);
            }
        }
        else
        {
            await RunWithPluginsInstall(executionThread, rowsOutput, query, cancellationToken: cancellationToken);
        }
    }

    private static async Task RunWithPluginsInstall(
        IExecutionThread<ExecutionOptions> executionThread,
        IRowsOutput rowsOutput,
        string query,
        CancellationToken cancellationToken)
    {
        var result = VariantValue.Null;
#if ENABLE_PLUGINS && PLUGIN_THRIFT
        try
        {
            result = await executionThread.RunAsync(query, cancellationToken: cancellationToken);
        }
        catch (Backend.ThriftPlugins.ProxyNotFoundException)
        {
            var installed = await QueryCat.Cli.Commands.Options.ApplicationOptions.InstallPluginsProxyAsync(cancellationToken: cancellationToken);
            if (installed)
            {
                await executionThread.RunAsync(query, cancellationToken: cancellationToken);
            }
        }
#else
        result = await executionThread.RunAsync(query, cancellationToken: cancellationToken);
#endif

        await WriteAsync(executionThread, result, rowsOutput, cancellationToken);
    }

    private static async Task WriteAsync(IExecutionThread<ExecutionOptions> executionThread, VariantValue result,
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
                var requestQuit = false;
                Thread.Sleep(options.FollowTimeout);
                await StartWriterLoop(cancellationToken);
                ProcessInput(ref requestQuit);
                if (cancellationToken.IsCancellationRequested || requestQuit)
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
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
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

    internal static void AddVariables(IExecutionThread executionThread, string[]? variables = null)
    {
        if (variables == null || !variables.Any())
        {
            return;
        }

        foreach (var variable in variables)
        {
            var arr = StringUtils.GetFieldsFromLine(variable, '=');
            if (arr.Length != 2)
            {
                throw new QueryCatException(string.Format(Resources.Errors.InvalidVariableFormat, variable));
            }
            var name = arr[0];
            var stringValue = StringUtils.Unquote(arr[1], quoteChar: "\'");
            var targetType = arr[1].Length == stringValue.Length
                ? DataTypeUtils.DetermineTypeByValue(stringValue)
                : DataType.String;
            if (!VariantValue.TryCreateFromString(stringValue, targetType, out var value))
            {
                throw new QueryCatException(string.Format(Resources.Errors.CannotDefineVariable, name));
            }
            executionThread.TopScope.Variables[name] = value.Cast(targetType);
        }
    }
}

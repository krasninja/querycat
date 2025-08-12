using System.CommandLine;
using QueryCat.Backend;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Inputs;
using QueryCat.Backend.Storage;

namespace QueryCat.Cli.Commands;

internal abstract class BaseQueryCommand : BaseCommand
{
    protected Argument<string> QueryArgument { get; } = new("query")
    {
        Description = Resources.Messages.QueryCommand_QueryDescription,
        DefaultValueFactory = _ => string.Empty,
    };

    protected Option<string[]> FilesOption { get; } = new("--file", "-f")
    {
        Description = Resources.Messages.QueryCommand_FilesDescription,
        AllowMultipleArgumentsPerToken = true,
    };

    protected Option<string[]> VariablesOption { get; } = new("--var")
    {
        Description = Resources.Messages.QueryCommand_VariablesDescription,
    };

    protected Option<string[]> InputsOption { get; } = new("--input", "-i")
    {
        Description = Resources.Messages.QueryCommand_InputsDescription,
        AllowMultipleArgumentsPerToken = true,
    };

    /// <inheritdoc />
    protected BaseQueryCommand(string name, string? description = null) : base(name, description)
    {
        Add(QueryArgument);
        Add(FilesOption);
        Add(VariablesOption);
        Add(InputsOption);
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
                var resolvedFile = IOFunctions.ResolveHomeDirectory(file);
                var text = await File.ReadAllTextAsync(resolvedFile, cancellationToken);
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
        // ReSharper disable once RedundantAssignment
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

    internal static Task AddVariablesAsync(IExecutionThread executionThread, string[]? variables = null,
        CancellationToken cancellationToken = default)
    {
        if (variables == null || !variables.Any())
        {
            return Task.CompletedTask;
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

        return Task.CompletedTask;
    }

    internal static async Task AddInputsAsync(IExecutionThread executionThread, string[]? inputs = null,
        CancellationToken cancellationToken = default)
    {
        if (inputs == null || !inputs.Any())
        {
            return;
        }

        foreach (var input in inputs)
        {
            var rowsInputPair = await RowsInputConverter.ResolveInputAsync(executionThread, input, cancellationToken);
            if (rowsInputPair.Value != null)
            {
                executionThread.TopScope.Variables[rowsInputPair.Key] = VariantValue.CreateFromObject(rowsInputPair.Value);
            }
        }
    }
}

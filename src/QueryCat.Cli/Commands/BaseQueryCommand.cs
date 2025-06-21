using System.CommandLine;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Cli.Commands;

internal abstract class BaseQueryCommand : BaseCommand
{
    protected Argument<string?> QueryArgument { get; } = new("query")
    {
        Description = Resources.Messages.QueryCommand_QueryDescription,
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
        IExecutionThread executionThread,
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
                await RunWithPluginsInstall(executionThread, text, cancellationToken: cancellationToken);
            }
        }
        else
        {
            await RunWithPluginsInstall(executionThread, query, cancellationToken: cancellationToken);
        }
    }

    private static async Task RunWithPluginsInstall(IExecutionThread executionThread, string query, CancellationToken cancellationToken)
    {
#if ENABLE_PLUGINS && PLUGIN_THRIFT
        try
        {
            await executionThread.RunAsync(query, cancellationToken: cancellationToken);
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
        await executionThread.RunAsync(query, cancellationToken: cancellationToken);
#endif
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

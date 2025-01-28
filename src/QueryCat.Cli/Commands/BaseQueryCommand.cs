using System.CommandLine;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Cli.Commands;

internal abstract class BaseQueryCommand : BaseCommand
{
    public Argument<string> QueryArgument { get; } = new("query",
        description: "SQL-like query or command argument.",
        getDefaultValue: () => string.Empty);

    public Option<string[]> FilesOption { get; } = new(["-f", "--files"],
        description: "SQL files to execute.")
    {
        AllowMultipleArgumentsPerToken = true,
    };

    public Option<string[]> VariablesOption { get; } = new("--var",
        description: "Pass variables.");

    /// <inheritdoc />
    public BaseQueryCommand(string name, string? description = null) : base(name, description)
    {
        Add(QueryArgument);
        Add(FilesOption);
        Add(VariablesOption);
    }

    internal static async Task RunQueryAsync(
        IExecutionThread executionThread,
        string query,
        string[] files,
        CancellationToken cancellationToken = default)
    {
        if (files.Any())
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
            if (arr.Count != 2)
            {
                throw new QueryCatException(string.Format(Resources.Errors.InvalidVariableFormat, variable));
            }
            var name = arr[0];
            var stringValue = arr[1];
            var targetType = DataTypeUtils.DetermineTypeByValue(stringValue);
            if (!VariantValue.TryCreateFromString(stringValue, targetType, out var value))
            {
                throw new QueryCatException(string.Format(Resources.Errors.CannotDefineVariable, name));
            }
            executionThread.TopScope.Variables[name] = value.Cast(targetType);
        }
    }
}

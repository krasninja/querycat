using System.CommandLine;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

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

    public void RunQuery(
        IExecutionThread executionThread,
        string query,
        string[] files,
        CancellationToken cancellationToken = default)
    {
        if (files.Any())
        {
            foreach (var file in files.SelectMany(f => f.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)))
            {
                RunWithPluginsInstall(executionThread, File.ReadAllText(file), cancellationToken: cancellationToken);
            }
        }
        else
        {
            RunWithPluginsInstall(executionThread, query, cancellationToken: cancellationToken);
        }
    }

    private void RunWithPluginsInstall(IExecutionThread executionThread, string query, CancellationToken cancellationToken)
    {
#if ENABLE_PLUGINS && PLUGIN_THRIFT
        try
        {
            executionThread.Run(query, cancellationToken: cancellationToken);
        }
        catch (QueryCat.Backend.ThriftPlugins.ProxyNotFoundException)
        {
            if (QueryCat.Cli.Commands.Options.ApplicationOptions.InstallPluginsProxy())
            {
                executionThread.Run(query, cancellationToken: cancellationToken);
            }
        }
#else
        executionThread.Run(query, cancellationToken: cancellationToken);
#endif
    }

    public void AddVariables(IExecutionThread executionThread, string[]? variables = null)
    {
        if (variables == null || !variables.Any())
        {
            return;
        }

        foreach (var variable in variables)
        {
            var arr = variable.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length != 2)
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

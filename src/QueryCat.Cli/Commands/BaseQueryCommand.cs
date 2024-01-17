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

    public Option<string[]> FilesOption { get; } = new(new[] { "-f", "--files" },
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
            foreach (var file in files)
            {
                executionThread.Run(File.ReadAllText(file), cancellationToken: cancellationToken);
            }
        }
        else
        {
            executionThread.Run(query, cancellationToken: cancellationToken);
        }
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
                throw new QueryCatException($"Variable '{variable}' must have NAME=VALUE format.");
            }
            var name = arr[0];
            var stringValue = arr[1];
            var targetType = DataTypeUtils.DetermineTypeByValue(stringValue);
            if (!VariantValue.TryCreateFromString(stringValue, targetType, out var value))
            {
                throw new QueryCatException($"Cannot define variable '{name}'.");
            }
            executionThread.TopScope.Variables[name] = value.Cast(targetType);
        }
    }
}

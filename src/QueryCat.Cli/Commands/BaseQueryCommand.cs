using System.CommandLine;
using QueryCat.Backend;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Types;

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

    public void RunQuery(ExecutionThread executionThread, string query, string[]? files = null)
    {
        if (files != null && files.Any())
        {
            foreach (var file in files)
            {
                executionThread.Run(File.ReadAllText(file));
            }
        }
        else
        {
            executionThread.Run(query);
        }
    }

    public void AddVariables(ExecutionThread executionThread, string[]? variables = null)
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
            executionThread.TopScope.DefineVariable(name, targetType, value);
        }
    }
}

using System.CommandLine;
using QueryCat.Backend.Execution;

namespace QueryCat.Cli.Commands;

internal abstract class BaseQueryCommand : BaseCommand
{
    public Argument<string> QueryArgument { get; } = new("query",
        description: "SQL-like query or command argument.",
        getDefaultValue: () => string.Empty);

    public static Option<string[]> FilesOption { get; } = new(new[] { "-f", "--files" },
        description: "SQL files.")
    {
        AllowMultipleArgumentsPerToken = true,
    };

    /// <inheritdoc />
    public BaseQueryCommand(string name, string? description = null) : base(name, description)
    {
        Add(QueryArgument);
        Add(FilesOption);
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
}

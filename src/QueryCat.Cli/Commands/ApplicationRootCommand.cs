using System.CommandLine;

namespace QueryCat.Cli.Commands;

internal class ApplicationRootCommand : RootCommand
{
    /// <inheritdoc />
    public ApplicationRootCommand() : base("The simple text parsing, data query and transformation utility.")
    {
    }
}

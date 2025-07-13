using System.CommandLine;

namespace QueryCat.Cli.Commands;

internal class ApplicationRootCommand : RootCommand
{
    /// <inheritdoc />
    public ApplicationRootCommand() : base(Resources.Messages.RootCommand_Description)
    {
    }
}

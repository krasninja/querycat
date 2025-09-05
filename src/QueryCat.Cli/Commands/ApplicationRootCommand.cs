using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

namespace QueryCat.Cli.Commands;

internal sealed class ApplicationRootCommand : RootCommand
{
    /// <inheritdoc />
    public ApplicationRootCommand() : base(Resources.Messages.RootCommand_Description)
    {
        AddRange(
            new QueryCommand(),
            new ExplainCommand(),
            new AstCommand(),
            new SchemaCommand(),
            new CallFunctionCommand(),
            new ServeCommand(),
#if ENABLE_PLUGINS
            new Command("plugin", Resources.Messages.PluginCommand_Description)
            {
                new PluginInstallCommand(),
                new PluginListCommand(),
                new PluginRemoveCommand(),
                new PluginUpdateCommand(),
                new PluginDebugCommand(),
#if PLUGIN_THRIFT
                new PluginProxyCommand(),
#endif
            }
        );
#endif

        TreatUnmatchedTokensAsErrors = false;

        // Allow to query without "query" command. Fast way.
        var queryArgument = new Argument<string>("query")
        {
            Hidden = true,
        };
        Add(queryArgument);

        SetCustomHelpMessage();
    }

    private void AddRange(params ReadOnlySpan<Command> directives)
    {
        foreach (var directive in directives)
        {
            Add(directive);
        }
    }

    private sealed class UsageHelpSection(HelpAction action) : SynchronousCommandLineAction
    {
        /// <inheritdoc />
        public override int Invoke(ParseResult parseResult)
        {
            var result = action.Invoke(parseResult);
            if (parseResult.CommandResult.Command is ApplicationRootCommand)
            {
                var output = parseResult.InvocationConfiguration.Output;
                output.WriteLine(Resources.Messages.HelpText);
            }
            return result;
        }
    }

    private void SetCustomHelpMessage()
    {
        foreach (var rootCommandOption in Options)
        {
            // RootCommand has a default HelpOption, we need to update its Action.
            if (rootCommandOption is HelpOption defaultHelpOption)
            {
                defaultHelpOption.Action = new UsageHelpSection((HelpAction)defaultHelpOption.Action!);
                break;
            }
        }
    }
}

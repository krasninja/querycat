using System.CommandLine;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

internal sealed class AgentGetCommand : BaseAgentCommand
{
    /// <inheritdoc />
    public AgentGetCommand() : base("get", Resources.Messages.AgentGetCommand_Description)
    {
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            applicationOptions.InitializeLogger();

            // Plugins are not required to get the agent, so read the config directly.
            var agentCall = await GetAgentCallAsync(ApplicationOptions.CreateConfigStorage(), cancellationToken);
            if (string.IsNullOrEmpty(agentCall))
            {
                Console.WriteLine(Resources.Messages.AgentGet_NotSet);
                return;
            }
            Console.WriteLine(agentCall);
        });
    }
}

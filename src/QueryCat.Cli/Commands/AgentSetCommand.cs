using System.CommandLine;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Inputs;

namespace QueryCat.Cli.Commands;

internal sealed class AgentSetCommand : BaseAgentCommand
{
    /// <inheritdoc />
    public AgentSetCommand() : base("set", Resources.Messages.AgentSetCommand_Description)
    {
        var agentArgument = new Argument<string>("agent")
        {
            Description = Resources.Messages.AgentSetCommand_AgentDescription,
        };

        this.Add(agentArgument);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var agentCall = parseResult.GetRequiredValue(agentArgument).Trim();

            applicationOptions.InitializeLogger();
            await using var root = await applicationOptions.CreateApplicationRootAsync(
                cancellationToken: cancellationToken);

            await EnsureAgentFunctionExistsAsync(root.Thread, agentCall, cancellationToken);

            // The config is loaded lazily on the first query run, so load it explicitly. Otherwise,
            // the save would overwrite the config file with the empty storage.
            await root.Thread.ConfigStorage.LoadAsync(cancellationToken);
            await root.Thread.ConfigStorage.SetAsync(
                AIAssistant.DefaultAIAgentConfigKey, new VariantValue(agentCall), cancellationToken);
            await root.Thread.ConfigStorage.SaveAsync(cancellationToken);

            Console.WriteLine(Resources.Messages.AgentSet_Success, agentCall);
        });
    }
}

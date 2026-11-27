using QueryCat.Backend.Ast.Nodes.Function;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Inputs;
using QueryCat.Backend.Parser;

namespace QueryCat.Cli.Commands;

/// <summary>
/// Base command to work with the AI answer agents.
/// </summary>
internal abstract class BaseAgentCommand : BaseCommand
{
    /// <inheritdoc />
    protected BaseAgentCommand(string name, string? description = null) : base(name, description)
    {
    }

    /// <summary>
    /// Get the function name from the agent function call.
    /// </summary>
    /// <param name="agentCall">Agent function call. For example, "ollama_agent('qwen3:8b')".</param>
    /// <returns>Function name.</returns>
    protected static string GetAgentFunctionName(string agentCall)
    {
        if (string.IsNullOrWhiteSpace(agentCall))
        {
            throw new QueryCatException(Resources.Errors.AgentInvalidFunctionCall);
        }

        var programNode = new AstBuilder().BuildProgramFromString(agentCall);
        if (programNode.Body.Statements.Count != 1
            || programNode.Body.Statements[0].RootNode is not FunctionCallNode functionCallNode)
        {
            throw new QueryCatException(Resources.Errors.AgentInvalidFunctionCall);
        }
        return functionCallNode.FunctionName;
    }

    /// <summary>
    /// Check that the function call refers to the existing function that creates an AI answer agent.
    /// Executes the function call to ensure it returns an object of type <see cref="IAnswerAgent" />.
    /// </summary>
    /// <param name="thread">Execution thread.</param>
    /// <param name="agentCall">Agent function call.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected static async Task EnsureAgentFunctionExistsAsync(
        IExecutionThread thread,
        string agentCall,
        CancellationToken cancellationToken = default)
    {
        var functionName = GetAgentFunctionName(agentCall);
        var functions = thread.FunctionsManager.FindByName(functionName);
        if (functions.Length < 1)
        {
            throw new QueryCatException(string.Format(Resources.Errors.AgentFunctionNotFound, functionName));
        }
        if (!functions.Any(FunctionFormatter.IsAnswerAgentFunction))
        {
            throw new QueryCatException(string.Format(
                Resources.Errors.AgentFunctionInvalidType, functionName, nameof(IAnswerAgent)));
        }

        // Execute the function call to make sure it really returns an AI answer agent object.
        var result = await thread.RunAsync(agentCall, cancellationToken: cancellationToken);
        var isAnswerAgent = result.AsObjectUnsafe is IAnswerAgent;
        result.Close();
        if (result.AsObjectUnsafe is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        if (!isAnswerAgent)
        {
            throw new QueryCatException(string.Format(
                Resources.Errors.AgentFunctionInvalidType, functionName, nameof(IAnswerAgent)));
        }
    }

    /// <summary>
    /// Get the configured AI answer agent function call.
    /// </summary>
    /// <param name="configStorage">Config storage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Agent function call or empty string if it is not set.</returns>
    protected static async Task<string> GetAgentCallAsync(
        IConfigStorage configStorage,
        CancellationToken cancellationToken = default)
    {
        // The config is loaded lazily on the first query run, so load it explicitly. Otherwise,
        // the save would overwrite the config file with the empty storage.
        await configStorage.LoadAsync(cancellationToken);
        var value = await configStorage.GetOrDefaultAsync(
            AIAssistant.DefaultAIAgentConfigKey, cancellationToken: cancellationToken);
        return value.Type == DataType.String ? value.AsStringUnsafe.Trim() : string.Empty;
    }
}

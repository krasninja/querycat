using QueryCat.Backend.Inputs;

namespace QueryCat.Cli.Infrastructure;

/// <inheritdoc />
// ReSharper disable once InconsistentNaming
internal sealed class ConsoleAIAssistant : AIAssistant
{
    /// <inheritdoc />
    protected override Task<string> ClarifyAsync(string issue, CancellationToken cancellationToken)
    {
        Console.WriteLine(@"(agent)> " + issue);
        Console.WriteLine(@"(user)> " + Resources.Messages.AgentGetUserResponse);
        var answer = Console.ReadLine();
        return Task.FromResult(answer ?? string.Empty);
    }
}

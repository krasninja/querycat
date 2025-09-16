using QueryCat.Backend.Inputs;

namespace QueryCat.Cli.Infrastructure;

/// <inheritdoc />
// ReSharper disable once InconsistentNaming
internal sealed class ConsoleAIAssistant : AIAssistant
{
    /// <inheritdoc />
    protected override Task<string> ClarifyAsync(string issue, CancellationToken cancellationToken)
    {
        Console.WriteLine("(agent)> " + issue);
        var answer = Console.ReadLine();
        return Task.FromResult(answer ?? string.Empty);
    }
}

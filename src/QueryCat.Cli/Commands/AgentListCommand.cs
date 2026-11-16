using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Cli.Commands;

internal sealed class AgentListCommand : BaseAgentCommand
{
    /// <inheritdoc />
    public AgentListCommand() : base("list", Resources.Messages.AgentListCommand_Description)
    {
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);

            applicationOptions.InitializeLogger();
            await using var root = await applicationOptions.CreateStdoutApplicationRootAsync(
                columnsSeparator: parseResult.GetValue(ColumnsSeparatorOption),
                outputStyle: parseResult.GetValue(OutputStyleOption),
                cancellationToken: cancellationToken
            );

            var functions = root.Thread.FunctionsManager.GetFunctions()
                .Where(FunctionFormatter.IsAnswerAgentFunction)
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var input = EnumerableRowsInput<IFunction>.FromSource(functions,
                builder => builder
                    .AddProperty("name", p => p.Name, "Agent function name.")
                    .AddProperty("signature", FunctionFormatter.GetSignature, "Agent function signature.")
                    .AddProperty("description", p => p.Description, "Agent function description.")
            );

            var result = VariantValue.CreateFromObject(input);
            await WriteAsync(root.Thread, result, root.RowsOutput, cancellationToken);
            result.Close();

            var agentCall = await GetAgentCallAsync(root.Thread.ConfigStorage, cancellationToken);
            if (!string.IsNullOrEmpty(agentCall))
            {
                Console.WriteLine(Resources.Messages.AgentList_Current, agentCall);
            }
        });
    }
}

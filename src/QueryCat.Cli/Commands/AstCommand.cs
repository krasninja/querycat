using QueryCat.Backend.Execution;

namespace QueryCat.Cli.Commands;

internal sealed class AstCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public AstCommand() : base("ast", Resources.Messages.AstCommand_Description)
    {
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.InvocationConfiguration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var query = parseResult.GetValue(QueryArgument);
            var variables = parseResult.GetValue(VariablesOption);
            var inputs = parseResult.GetValue(InputsOption);
            var files = parseResult.GetValue(FilesOption);

            applicationOptions.InitializeLogger();
            applicationOptions.InitializeAIAssistant();
            await using var root = await applicationOptions.CreateApplicationRootAsync();
            root.Thread.StatementExecuting += async (sender, threadArgs) =>
            {
                if (sender is not DefaultExecutionThread executionThread)
                {
                    return;
                }
                Console.WriteLine(await executionThread.DumpAstAsync(threadArgs));
                threadArgs.ContinueExecution = false;
            };
            await AddVariablesAsync(root.Thread, variables, cancellationToken);
            await AddInputsAsync(root.Thread, inputs, cancellationToken);
            await RunQueryAsync(root.Thread, root.RowsOutput, query, files, cancellationToken);
        });
    }
}

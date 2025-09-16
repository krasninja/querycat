namespace QueryCat.Cli.Commands;

internal class AstCommand : BaseQueryCommand
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
            var root = await applicationOptions.CreateApplicationRootAsync();
            root.Thread.StatementExecuting += async (_, threadArgs) =>
            {
                Console.WriteLine(await root.Thread.DumpAstAsync(threadArgs));
                threadArgs.ContinueExecution = false;
            };
            await AddVariablesAsync(root.Thread, variables, cancellationToken);
            await AddInputsAsync(root.Thread, inputs, cancellationToken);
            await RunQueryAsync(root.Thread, root.RowsOutput, query, files, cancellationToken);
        });
    }
}

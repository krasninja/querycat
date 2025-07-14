namespace QueryCat.Cli.Commands;

internal class AstCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public AstCommand() : base("ast", Resources.Messages.AstCommand_Description)
    {
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.Configuration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var query = parseResult.GetValue(QueryArgument);
            var variables = parseResult.GetValue(VariablesOption);
            var files = parseResult.GetValue(FilesOption);

            applicationOptions.InitializeLogger();
            var root = await applicationOptions.CreateApplicationRootAsync();
            root.Thread.StatementExecuting += async (_, threadArgs) =>
            {
                Console.WriteLine(await root.Thread.DumpAstAsync(threadArgs));
                threadArgs.ContinueExecution = false;
            };
            AddVariables(root.Thread, variables);
            await RunQueryAsync(root.Thread, root.RowsOutput, query, files, cancellationToken);
        });
    }
}

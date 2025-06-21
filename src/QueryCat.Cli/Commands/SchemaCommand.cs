using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Functions;

namespace QueryCat.Cli.Commands;

internal class SchemaCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public SchemaCommand() : base("schema", Resources.Messages.SchemaCommand_Description)
    {
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.Configuration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var query = parseResult.GetValue(QueryArgument);
            var variables = parseResult.GetValue(VariablesOption);
            var files = parseResult.GetValue(FilesOption);

            applicationOptions.InitializeLogger();
            var root = await applicationOptions.CreateStdoutApplicationRootAsync();
            var thread = root.Thread;
            thread.StatementExecuted += async (_, args) =>
            {
                if (!args.Result.IsNull
                    && args.Result.Type == DataType.Object
                    && args.Result.AsObject is IRowsSchema rowsSchema)
                {
                    args.ContinueExecution = false;
                    var schema = await FunctionCaller.CallWithArgumentsAsync(InfoFunctions.Schema, thread, [rowsSchema]);
                    thread.TopScope.Variables["result"] = schema;
                    await thread.RunAsync("result", cancellationToken: cancellationToken);
                }
            };
            AddVariables(thread, variables);
            await RunQueryAsync(thread, query, files, cancellationToken);
        });
    }
}

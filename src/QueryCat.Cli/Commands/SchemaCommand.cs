using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
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
            var inputs = parseResult.GetValue(InputsOption);
            var files = parseResult.GetValue(FilesOption);

            applicationOptions.InitializeLogger();
            applicationOptions.InitializeAIAssistant();
            await using var root = await applicationOptions.CreateStdoutApplicationRootAsync(
                columnsSeparator: parseResult.GetValue(ColumnsSeparatorOption),
                outputStyle: parseResult.GetValue(OutputStyleOption)
            );
            var thread = root.Thread;
            var rowsOutput = root.RowsOutput;
            thread.StatementExecuted += (_, args) =>
            {
                if (!args.Result.IsNull
                    && args.Result.Type == DataType.Object
                    && args.Result.AsObject is IRowsSchema rowsSchema)
                {
                    AsyncUtils.RunSync(async () =>
                    {
                        var schema = await FunctionCaller.CallWithArgumentsAsync(InfoFunctions.Schema, thread, [rowsSchema]);
                        await WriteAsync(thread, schema, rowsOutput, cancellationToken);
                    });
                    args.ContinueExecution = false;
                }
            };
            await AddVariablesAsync(thread, variables, cancellationToken);
            await AddInputsAsync(thread, inputs, cancellationToken);
            await RunQueryAsync(thread, rowsOutput, query, files, cancellationToken);
        });
    }
}

using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Cli.Commands;

internal class ExplainCommand : BaseQueryCommand
{
    /// <inheritdoc />
    public ExplainCommand() : base("explain", Resources.Messages.ExplainCommand_Description)
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
            await using var root = await applicationOptions.CreateStdoutApplicationRootAsync(
                columnsSeparator: parseResult.GetValue(ColumnsSeparatorOption),
                outputStyle: parseResult.GetValue(OutputStyleOption)
            );
            root.Thread.StatementExecuted += (_, args) =>
            {
                if (args.Result.Type == DataType.Object
                    && args.Result.AsObject is IRowsIterator rowsIterator)
                {
                    var stringBuilder = new IndentedStringBuilder();
                    rowsIterator.Explain(stringBuilder);

                    Console.WriteLine(stringBuilder);
                    args.ContinueExecution = false;
                }
            };
            await AddVariablesAsync(root.Thread, variables, cancellationToken);
            await AddInputsAsync(root.Thread, inputs, cancellationToken);
            await RunQueryAsync(root.Thread, root.RowsOutput, query, files, cancellationToken);
        });
    }
}

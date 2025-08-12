using System.CommandLine;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Cli.Commands;

internal sealed class CallFunctionCommand : BaseCommand
{
    /// <inheritdoc />
    public CallFunctionCommand() : base("call", Resources.Messages.CallCommand_Description)
    {
        var functionNameArgument = new Argument<string>("name")
        {
            Description = Resources.Messages.CallCommand_FunctionDescription,
        };
        var functionArgumentsArgument = new Argument<string[]>("args")
        {
            Description = Resources.Messages.CallCommand_ArgumentsDescription
        };

        this.Add(functionNameArgument);
        this.Add(functionArgumentsArgument);
        this.SetAction(async (parseResult, cancellationToken) =>
        {
            parseResult.Configuration.EnableDefaultExceptionHandler = false;

            var applicationOptions = GetApplicationOptions(parseResult);
            var functionName = parseResult.GetRequiredValue(functionNameArgument);
            var functionArguments = parseResult.GetValue(functionArgumentsArgument) ?? [];

            applicationOptions.InitializeLogger();
            applicationOptions.InitializeAIAssistant();
            await using var root = await applicationOptions.CreateStdoutApplicationRootAsync(
                columnsSeparator: parseResult.GetValue(ColumnsSeparatorOption),
                outputStyle: parseResult.GetValue(OutputStyleOption)
            );
            var function = root.Thread.FunctionsManager.FindByNameFirst(functionName);
            var callArgs = new FunctionCallArguments();
            foreach (var arg in functionArguments)
            {
                callArgs.Add(new VariantValue(arg));
            }
            var result = await root.Thread.FunctionsManager.CallFunctionAsync(
                function, root.Thread, callArgs, cancellationToken);
            await WriteAsync(root.Thread, result, root.RowsOutput, cancellationToken);
        });
    }
}

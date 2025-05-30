using System.CommandLine;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Cli.Commands.Options;

namespace QueryCat.Cli.Commands;

internal sealed class CallFunctionCommand : BaseCommand
{
    /// <inheritdoc />
    public CallFunctionCommand() : base("call", "Call function.")
    {
        var functionNameArgument = new Argument<string>("name", "Function name.");
        var functionArgumentsArgument = new Argument<string[]>("args", "Function call arguments.");

        this.AddArgument(functionNameArgument);
        this.AddArgument(functionArgumentsArgument);
        this.SetHandler(async context =>
        {
            var applicationOptions = OptionsUtils.GetValueForOption(
                new ApplicationOptionsBinder(LogLevelOption, PluginDirectoriesOption), context);
            var functionName = OptionsUtils.GetValueForOption(functionNameArgument, context);
            var functionArguments = OptionsUtils.GetValueForOption(functionArgumentsArgument, context);

            applicationOptions.InitializeLogger();
            var root = await applicationOptions.CreateStdoutApplicationRootAsync();
            var function = root.Thread.FunctionsManager.FindByNameFirst(functionName);
            var callArgs = new FunctionCallArguments();
            foreach (var arg in functionArguments)
            {
                callArgs.Add(new VariantValue(arg));
            }
            var result = await root.Thread.FunctionsManager.CallFunctionAsync(function,
                root.Thread, callArgs, context.GetCancellationToken());
            root.Thread.TopScope.Variables["result"] = result;
            await root.Thread.RunAsync("result");
        });
    }
}

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Execution;
using QueryCat.Backend.Functions;
using QueryCat.Backend.Types;

namespace QueryCat.Build.Tasks;

[TaskName("Get-Inputs-Markdown")]
public class GetInputsInMarkdownTask : AsyncFrostingTask<BuildContext>
{
    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var assemblyFile = context.Arguments.GetArgument("Assembly");
        using var thread = new ExecutionThread(new ExecutionOptions
        {
            PluginDirectories = { assemblyFile }
        });
        new ExecutionThreadBootstrapper().Bootstrap(thread);
        var pluginFunctions = thread.FunctionsManager.GetFunctions()
            .Where(f =>
                f.ReturnType == DataType.Object
                && !f.IsAggregate
                && f.Arguments.Length == 0
                && f.ReturnObjectName == nameof(IRowsInput)
                && !(f.Delegate.Method.Module.Assembly.FullName ?? string.Empty).StartsWith("QueryCat.Backend"))
            .ToList();
        var sb = new StringBuilder();

        foreach (var inputFunction in pluginFunctions)
        {
            IRowsInput rowsInput;
            try
            {
                rowsInput = inputFunction.Delegate.Invoke(FunctionCallInfo.Empty).GetAsObject<IRowsInput>();
                rowsInput.Open();
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Warning("Cannot init rows input '{InputFunction}': {Message}",
                    inputFunction, ex.Message);
                continue;
            }

            sb
                .AppendLine($"\n## **{inputFunction.Name}**\n")
                .AppendLine(inputFunction.Description)
                .AppendLine("\n```")
                .AppendLine($"{inputFunction}")
                .AppendLine("```\n")
                .AppendLine("| Name | Type | Description |")
                .AppendLine("| --- | --- | --- |");
            foreach (var column in rowsInput.Columns)
            {
                sb.AppendLine($"| `{column.Name}` | `{column.DataType}` | {column.Description} |");
            }
        }

        var file = Path.Combine(context.OutputDirectory, "plugin.md");
        File.WriteAllText(file, sb.ToString());
        context.Log.Information($"Wrote to {file}.");
        return Task.CompletedTask;
    }
}

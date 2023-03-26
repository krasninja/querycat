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
using QueryCat.Backend.Relational;
using QueryCat.Backend.Storage;
using QueryCat.Backend.Types;

namespace QueryCat.Build.Tasks;

[TaskName("Get-Inputs-Markdown")]
public class GetInputsInMarkdownTask : AsyncFrostingTask<BuildContext>
{
    private static readonly string[] ExcludeList =
    {
        "read_file",
        "read_text",
    };

    private sealed class CollectQueryContext : QueryContext
    {
        /// <inheritdoc />
        public override QueryContextQueryInfo QueryInfo { get; } = new(Array.Empty<Column>());

        /// <inheritdoc />
        public CollectQueryContext() : base(QueryCat.Backend.Execution.ExecutionThread.Empty)
        {
        }
    }

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
                && f.ReturnObjectName == nameof(IRowsInput)
                && f.Name.Contains("_") // Usually plugin name should have name like "pluginName_method".
                && !ExcludeList.Contains(f.Name)
            )
            .OrderBy(f => f.Name)
            .ToList();
        var sb = new StringBuilder()
            .AppendLine("## Sources");

        foreach (var inputFunction in pluginFunctions)
        {
            IRowsInput rowsInput;
            var queryContext = new CollectQueryContext();
            try
            {
                var functionCallInfo = new FunctionCallInfo(ExecutionThread.Empty);
                for (var i = 0; i < inputFunction.Arguments.Length; i++)
                {
                    functionCallInfo.Push(VariantValue.Null);
                }
                rowsInput = inputFunction.Delegate.Invoke(functionCallInfo).GetAsObject<IRowsInput>();
                rowsInput.SetContext(queryContext);
                rowsInput.Open();
            }
            catch (Exception ex)
            {
                Serilog.Log.Logger.Warning("Cannot init rows input '{InputFunction}': {Message}",
                    inputFunction, ex.Message);
                continue;
            }

            sb
                .AppendLine($"\n### **{inputFunction.Name}**")
                .AppendLine("\n```")
                .AppendLine(inputFunction.ToString())
                .AppendLine("```\n")
                .AppendLine(inputFunction.Description)
                .AppendLine("\n| Name | Type | Required | Description |")
                .AppendLine("| --- | --- | --- | --- |");
            foreach (var column in rowsInput.Columns)
            {
                if (column.IsHidden)
                {
                    continue;
                }
                var inputColumn =
                    queryContext.InputInfo.KeyColumns.FirstOrDefault(c => c.ColumnName == column.Name);
                if (inputColumn == null)
                {
                    inputColumn = new QueryContextInputInfo.KeyColumn(column.Name, Array.Empty<VariantValue.Operation>());
                }
                var operations = string.Join(", ", inputColumn.Operations.Select(o => $"`{o}`"));
                sb.AppendLine($"| `{column.Name}` | `{column.DataType}` | {(inputColumn.IsRequired ? "yes" : string.Empty)} | {column.Description} |");
            }
        }

        var file = Path.Combine(context.OutputDirectory, "plugin.md");
        File.WriteAllText(file, sb.ToString());
        context.Log.Information($"Wrote to {file}.");
        return Task.CompletedTask;
    }
}

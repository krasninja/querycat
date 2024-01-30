using System.Text;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;
using QueryCat.Backend.ThriftPlugins;

namespace QueryCat.Build.Tasks;

[TaskName("Get-Inputs-Markdown")]
public class GetInputsInMarkdownTask : AsyncFrostingTask<BuildContext>
{
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(GetInputsInMarkdownTask));

    private static readonly string[] ExcludeList =
    {
        "read_file",
        "read_text",
    };

    private sealed class CollectQueryContext : QueryContext
    {
        /// <inheritdoc />
        public override QueryContextQueryInfo QueryInfo { get; } = new(Array.Empty<Column>());
    }

    /// <inheritdoc />
    public override Task RunAsync(BuildContext context)
    {
        var assemblyFile = context.Arguments.GetArgument("Assembly");
        using var thread = new ExecutionThreadBootstrapper()
            .WithPluginsLoader(thread => new ThriftPluginsLoader(thread, new[] { assemblyFile }))
            .Create();
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
            IRowsInputKeys rowsInput;
            var queryContext = new CollectQueryContext();
            try
            {
                var functionCallInfo = new FunctionCallInfo(Executor.Thread, inputFunction.Name);
                for (var i = 0; i < inputFunction.Arguments.Length; i++)
                {
                    functionCallInfo.Push(VariantValue.Null);
                }
                rowsInput = inputFunction.Delegate.Invoke(functionCallInfo).As<IRowsInputKeys>();
                rowsInput.QueryContext = queryContext;
                rowsInput.Open();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Cannot init rows input '{InputFunction}': {Message}",
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
                    rowsInput.GetKeyColumns().FirstOrDefault(c => Column.NameEquals(c.ColumnName, column.Name));
                if (inputColumn == null)
                {
                    inputColumn = new KeyColumn(column.Name);
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

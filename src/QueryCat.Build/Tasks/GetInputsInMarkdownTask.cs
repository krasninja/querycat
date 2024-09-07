using System.Text;
using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Frosting;
using Microsoft.Extensions.Logging;
using QueryCat.Backend;
using QueryCat.Backend.AssemblyPlugins;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Execution;
using QueryCat.Backend.ThriftPlugins;

namespace QueryCat.Build.Tasks;

[TaskName("Get-Inputs-Markdown")]
public sealed class GetInputsInMarkdownTask : AsyncFrostingTask<BuildContext>
{
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(GetInputsInMarkdownTask));

    private static readonly string[] _excludeList =
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
        var targetFile = context.Arguments.GetArgument("File");
        var loader = context.Arguments.GetArgument("Loader");

        // Create thread and load plugin.
        var bootstrapper = new ExecutionThreadBootstrapper(new ExecutionOptions
        {
            RunBootstrapScript = false,
            SafeMode = true,
        });
        if (!string.IsNullOrEmpty(loader) && loader.Contains("thrift", StringComparison.OrdinalIgnoreCase))
        {
            bootstrapper.WithPluginsLoader(thr => new ThriftPluginsLoader(thr, [targetFile],
                ExecutionThread.GetApplicationDirectory()));
        }
        else
        {
            bootstrapper.WithPluginsLoader(thr => new DotNetAssemblyPluginsLoader(thr.FunctionsManager, [targetFile]));
        }
        using var thread = bootstrapper.Create();

        // Prepare functions list.
        var pluginFunctions = thread.FunctionsManager.GetFunctions()
            .Where(f =>
                f.ReturnType == DataType.Object
                && !f.IsAggregate
                && f.ReturnObjectName == nameof(IRowsInput)
                && f.Name.Contains("_") // Usually plugin name should have name like "pluginName_method".
                && !_excludeList.Contains(f.Name)
            )
            .OrderBy(f => f.Name)
            .ToList();
        var sb = new StringBuilder()
            .AppendLine("# Schema")
            .AppendLine();

        // Prepare TOC.
        foreach (var inputFunction in pluginFunctions)
        {
            sb.AppendLine($"- [{inputFunction.Name}](#{inputFunction.Name})");
        }

        // Iterate and write whole schema.
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
                .AppendLine($"\n## **{inputFunction.Name}**")
                .AppendLine("\n```")
                .AppendLine(inputFunction.ToString())
                .AppendLine("```\n")
                .AppendLine(inputFunction.Description)
                .AppendLine("\n| Name | Type | Required | Description |")
                .AppendLine("| --- | --- | --- | --- |");
            for (var i = 0; i < rowsInput.Columns.Length; i++)
            {
                var column = rowsInput.Columns[i];
                if (column.IsHidden)
                {
                    continue;
                }
                var inputColumn =
                    rowsInput.GetKeyColumns().FirstOrDefault(c => rowsInput.Columns[c.ColumnIndex] == column);
                if (inputColumn == null)
                {
                    inputColumn = new KeyColumn(i);
                }
                sb.AppendLine($"| `{column.Name}` | `{column.DataType}` | {(inputColumn.IsRequired ? "yes" : string.Empty)} | {column.Description} |");
            }
        }

        var file = Path.Combine(context.OutputDirectory, "plugin.md");
        File.WriteAllText(file, sb.ToString());
        context.Log.Information($"Wrote to {file}.");

        return base.RunAsync(context);
    }
}

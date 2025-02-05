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

namespace QueryCat.Build.Tasks;

[TaskName("Get-Inputs-Markdown")]
public sealed class GetInputsInMarkdownTask : AsyncFrostingTask<BuildContext>
{
    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(GetInputsInMarkdownTask));

    private static readonly string[] _excludeList =
    [
        "read_file",
        "read_text"
    ];

    private sealed class CollectQueryContext : QueryContext
    {
        /// <inheritdoc />
        public override QueryContextQueryInfo QueryInfo { get; } = new(Array.Empty<Column>());
    }

    /// <inheritdoc />
    public override async Task RunAsync(BuildContext context)
    {
        var targetFile = context.Arguments.GetArgument("File");
        var outDirectory = context.Arguments.HasArgument("Out")
            ? context.Arguments.GetArgument("Out")
            : context.OutputDirectory;
        var loader = context.Arguments.GetArgument("Loader");

        // Create thread and load plugin.
        var bootstrapper = new ExecutionThreadBootstrapper(new ExecutionOptions
        {
            RunBootstrapScript = false,
            SafeMode = true,
        });
        if (!string.IsNullOrEmpty(loader) && loader.Contains("thrift", StringComparison.OrdinalIgnoreCase))
        {
#if PLUGIN_THRIFT
            bootstrapper.WithPluginsLoader(thread => new Backend.ThriftPlugins.ThriftPluginsLoader(
                thread,
                [targetFile],
                QueryCat.Backend.Execution.ExecutionThread.GetApplicationDirectory()));
#else
            throw new NotSupportedException("ThriftPlugin is not supported.");
#endif
        }
        else
        {
            bootstrapper.WithPluginsLoader(thr => new DotNetAssemblyPluginsLoader(thr.FunctionsManager, [targetFile]));
        }

        await using var thread = await bootstrapper.CreateAsync();

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
            .AppendLine("# Schema");
        if (pluginFunctions.Any())
        {
            sb.AppendLine();
        }

        // Prepare TOC.
        foreach (var inputFunction in pluginFunctions)
        {
            var name = inputFunction.Name.ToLowerInvariant();
            sb.AppendLine($"- [{name}](#{name})");
        }

        // Iterate and write whole schema.
        foreach (var inputFunction in pluginFunctions)
        {
            IRowsInputKeys rowsInput;
            var queryContext = new CollectQueryContext();
            try
            {
                using var frame = thread.Stack.CreateFrame();
                for (var i = 0; i < inputFunction.Arguments.Length; i++)
                {
                    frame.Push(VariantValue.Null);
                }
                rowsInput = (await FunctionCaller.CallAsync(inputFunction.Delegate, thread))
                    .As<IRowsInputKeys?>()!;
                rowsInput.QueryContext = queryContext;
                await rowsInput.OpenAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Cannot init rows input '{InputFunction}': {Message}",
                    inputFunction, ex.Message);
                continue;
            }

            sb
                .AppendLine($"\n## **{inputFunction.Name.ToLowerInvariant()}**")
                .AppendLine("\n```")
                .AppendLine(FunctionFormatter.GetSignature(inputFunction, forceLowerCase: true))
                .AppendLine("```\n")
                .AppendLine(inputFunction.Description)
                .AppendLine("\n| Name | Type | Key | Required | Description |")
                .AppendLine("| --- | --- | --- | --- | --- |");
            for (var i = 0; i < rowsInput.Columns.Length; i++)
            {
                var column = rowsInput.Columns[i];
                if (column.IsHidden)
                {
                    continue;
                }
                var inputColumn =
                    rowsInput.GetKeyColumns().FirstOrDefault(c => rowsInput.Columns[c.ColumnIndex] == column);
                var isKey = inputColumn != null;
                if (inputColumn == null)
                {
                    inputColumn = new KeyColumn(i);
                }
                sb.Append($"| `{column.Name}`" )
                    .Append($"| `{column.DataType}` ")
                    .Append($"| {(isKey ? "yes" : string.Empty)} ")
                    .Append($"| {(inputColumn.IsRequired ? "yes" : string.Empty)} ")
                    .Append($"| {column.Description}");
                sb.AppendLine(" |");
            }
        }

        var file = Path.Combine(outDirectory, "Schema.md");
        await File.WriteAllTextAsync(file, sb.ToString());
        context.Log.Information($"Wrote to {file}.");

        await base.RunAsync(context);
    }
}

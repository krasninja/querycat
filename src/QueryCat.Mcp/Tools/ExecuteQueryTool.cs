using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Mcp.Models;
using QueryCat.Mcp.Utils;

namespace QueryCat.Mcp.Tools;

internal sealed class ExecuteQueryTool : IMcpTool
{
    private const string QueryKey = "query";
    private const string MaxRowsKey = "max_rows";

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(ExecuteQueryTool));

    /// <inheritdoc />
    public McpToolModel Metadata { get; } = new(
        "execute_query",
        "Execute SQL query and returns data set.")
    {
        Description = "Executes SQL query and returns data set. Use search_functions to find the function "
            + "and its signature first. The return function type must be Object<IRowsInput> or Object<IRowsIterator>.",
        InputSchema = new McpToolInputSchemaModel
        {
            Properties = new Dictionary<string, McpToolSchemaPropertyModel>
            {
                [QueryKey] = new(McpToolSchemaPropertyModel.TypeString, "SQL query"),
                [MaxRowsKey] = new(McpToolSchemaPropertyModel.TypeInteger,
                    $"Max rows to return, cannot be greater than {VariantValueJsonSerializer.MaxRowsCount}. "
                    + "The result has 'truncated' flag set if there are more rows."),
            },
            Required = [QueryKey],
        },
        Annotations = new McpToolAnnotationsModel
        {
            ReadOnlyHint = false,
            OpenWorldHint = true,
        },
    };

    /// <inheritdoc />
    public async Task<McpCallToolResultModel> CallAsync(
        IExecutionThread thread,
        IReadOnlyDictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken)
    {
        var query = ToolArgumentsUtils.GetString(arguments, QueryKey);
        if (string.IsNullOrEmpty(query))
        {
            return McpCallToolResultModel.CreateError(Resources.Errors.McpQueryRequired);
        }
        var maxRows = ToolArgumentsUtils.GetInt32(arguments, MaxRowsKey, VariantValueJsonSerializer.MaxRowsCount);

        _logger.LogTrace("Query '{Query}', MaxRows {MaxRows}.", query, maxRows);
        JsonElement structuredContent;
        try
        {
            var result = await thread.RunAsync(query, null, cancellationToken);
            structuredContent = await VariantValueJsonSerializer.CreateStructuredContentAsync(result, maxRows, cancellationToken);
        }
        catch (QueryMissedConditionException e)
        {
            return McpCallToolResultModel.CreateError(QueryMissedConditionText.GetClientInstructionText(e));
        }
        catch (QueryCatException e)
        {
            return McpCallToolResultModel.CreateError(e.Message);
        }

        return McpCallToolResultModel.CreateStructured(structuredContent);
    }
}

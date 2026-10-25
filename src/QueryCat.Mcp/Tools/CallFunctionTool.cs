using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Commands;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Functions;
using QueryCat.Mcp.Models;
using QueryCat.Mcp.Utils;

namespace QueryCat.Mcp.Tools;

internal sealed class CallFunctionTool : IMcpTool
{
    private const string NameKey = "name";
    private const string ArgsKey = "args";

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(CallFunctionTool));

    /// <inheritdoc />
    public McpToolModel Metadata { get; } = new(
        "call_function",
        "Calls the function from the registry.")
    {
        Description = "Calls the function by name with the specified arguments and returns the result. "
            + "Use search_functions to find the function and its signature first.",
        InputSchema = new McpToolInputSchemaModel
        {
            Properties = new Dictionary<string, McpToolSchemaPropertyModel>
            {
                [NameKey] = new(McpToolSchemaPropertyModel.TypeString,
                    "Function name"),
                [ArgsKey] = new(McpToolSchemaPropertyModel.TypeObject,
                    "Function arguments, object where key is the argument name and value is the argument value"),
            },
            Required = [NameKey],
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
        var name = ToolArgumentsUtils.GetString(arguments, NameKey);
        if (string.IsNullOrEmpty(name))
        {
            return McpCallToolResultModel.CreateError(Resources.Errors.McpFunctionNameRequired);
        }

        if (!FunctionCallArgumentsReader.TryRead(arguments, ArgsKey, out var callArguments))
        {
            return McpCallToolResultModel.CreateError(Resources.Errors.McpInvalidFunctionArguments);
        }

        JsonElement structuredContent;
        IFunction? function = null;
        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Calling function '{FunctionName}' with arguments '{FunctionArguments}'.", name,
                callArguments.ToString());
        }
        try
        {
            function = thread.FunctionsManager.FindByNameFirst(name, callArguments.GetTypes());
            if (function.IsAggregate)
            {
                return McpCallToolResultModel.CreateError(
                    string.Format(Resources.Errors.McpCannotCallAggregateFunction, function.Name));
            }
            var result = await thread.FunctionsManager.CallFunctionAsync(function, thread, callArguments, cancellationToken);
            structuredContent = await VariantValueJsonSerializer.CreateStructuredContentAsync(result, cancellationToken: cancellationToken);
        }
        catch (SafeModeException e)
        {
            if (function != null)
            {
                return McpCallToolResultModel.CreateError(
                    string.Format(Resources.Errors.McpCannotCallUnsafeFunction, function.Name));
            }
            else
            {
                return McpCallToolResultModel.CreateError(e.Message);
            }
        }
        catch (QueryMissedConditionException e)
        {
            if (string.IsNullOrEmpty(e.InputSourceName) && function != null)
            {
                e = new QueryMissedConditionException(
                    function.Name,
                    QueryMissedConditionException.MissedConditionInputSourceType.Function,
                    e.ColumnName,
                    e.Operations);
            }
            return McpCallToolResultModel.CreateError(QueryMissedConditionText.GetClientInstructionText(e));
        }
        catch (QueryCatException e)
        {
            return McpCallToolResultModel.CreateError(e.Message);
        }

        return McpCallToolResultModel.CreateStructured(structuredContent);
    }
}

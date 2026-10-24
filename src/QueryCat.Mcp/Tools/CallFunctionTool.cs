using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Commands;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Fetch;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
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
    };

    /// <inheritdoc />
    public async Task<McpCallToolResultModel> CallAsync(
        IExecutionThread thread,
        IReadOnlyDictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken)
    {
        var name = arguments.TryGetValue(NameKey, out var nameElement) && nameElement.ValueKind == JsonValueKind.String
            ? (nameElement.GetString() ?? string.Empty).Trim()
            : string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            return McpCallToolResultModel.CreateError(Resources.Errors.McpFunctionNameRequired);
        }

        if (!TryGetCallArguments(arguments, out var callArguments))
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

        return new McpCallToolResultModel
        {
            Content =
            [
                new McpToolTextContentModel
                {
                    Text = structuredContent.ToString(),
                },
            ],
            StructuredContent = structuredContent,
        };
    }

    private static bool TryGetCallArguments(
        IReadOnlyDictionary<string, JsonElement> arguments,
        out FunctionCallArguments callArguments)
    {
        callArguments = new FunctionCallArguments();
        if (!arguments.TryGetValue(ArgsKey, out var argsElement)
            || argsElement.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return true;
        }

        // Named arguments.
        if (argsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in argsElement.EnumerateObject())
            {
                if (!VariantValue.TryGetValueFromJsonElement(property.Value, out var value))
                {
                    return false;
                }
                callArguments.Add(property.Name, value);
            }
            return true;
        }

        // Positional arguments.
        if (argsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in argsElement.EnumerateArray())
            {
                if (!VariantValue.TryGetValueFromJsonElement(item, out var value))
                {
                    return false;
                }
                callArguments.Add(value);
            }
            return true;
        }

        return false;
    }
}

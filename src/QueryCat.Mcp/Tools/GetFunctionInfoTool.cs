using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Ast;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Mcp.Models;
using QueryCat.Mcp.Utils;

namespace QueryCat.Mcp.Tools;

internal sealed class GetFunctionInfoTool : IMcpTool
{
    private const string NameKey = "name";
    private const string ArgsKey = "args";

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(GetFunctionInfoTool));

    /// <inheritdoc />
    public McpToolModel Metadata { get; } = new(
        "get_function_info",
        "Returns detailed function information.")
    {
        Description = "Returns detailed information about all function overloads: signature, description, "
            + "arguments and return type. For functions that return Object<IRowsInput> or Object<IRowsIterator> "
            + "it also returns the output columns schema (names, types and supported key filter operations). "
            + "To get the columns schema provide the arguments required to open the input.",
        InputSchema = new McpToolInputSchemaModel
        {
            Properties = new Dictionary<string, McpToolSchemaPropertyModel>
            {
                [NameKey] = new(McpToolSchemaPropertyModel.TypeString, "Function name"),
                [ArgsKey] = new(McpToolSchemaPropertyModel.TypeObject,
                    "Function arguments, object where key is the argument name and value is the argument value. "
                    + "Used only to open rows input and get its columns schema."),
            },
            Required = [NameKey],
        },
    };

    internal sealed class GetFunctionInfoToolArgumentResponse
    {
        public string Name { get; }

        public string Type { get; }

        public bool IsOptional { get; }

        public string? DefaultValue { get; }

        public bool IsArray { get; }

        public bool IsVariadic { get; }

        public GetFunctionInfoToolArgumentResponse(FunctionSignatureArgument argument)
        {
            Name = argument.Name.ToLowerInvariant();
            Type = argument.Type.ToString().ToLowerInvariant();
            IsOptional = argument.IsOptional;
            DefaultValue = argument.HasDefaultValue && !argument.DefaultValue.IsNull
                ? FunctionFormatter.ValueToString(argument.DefaultValue)
                : null;
            IsArray = argument.IsArray;
            IsVariadic = argument.IsVariadic;
        }
    }

    internal sealed class GetFunctionInfoToolColumnResponse
    {
        public required string Name { get; init; }

        public required string Type { get; init; }

        public string? Description { get; init; }

        /// <summary>
        /// Filter operations that the input can handle on its side (WHERE column [operation] value).
        /// </summary>
        public string[]? KeyOperations { get; init; }

        /// <summary>
        /// The key condition must be specified in the query.
        /// </summary>
        public bool? IsKeyRequired { get; init; }
    }

    internal sealed class GetFunctionInfoToolFunctionResponse
    {
        public string Name { get; }

        public string Description { get; }

        public string Signature { get; }

        public string ReturnType { get; }

        public string? ReturnObjectName { get; }

        public bool IsAggregate { get; }

        public bool IsSafe { get; }

        public string[]? Formatters { get; }

        public List<GetFunctionInfoToolArgumentResponse> Arguments { get; }

        public List<GetFunctionInfoToolColumnResponse>? Columns { get; set; }

        public string? ColumnsError { get; set; }

        public GetFunctionInfoToolFunctionResponse(IFunction function)
        {
            Name = function.Name;
            Description = function.Description;
            Signature = FunctionFormatter.GetSignature(function, forceLowerCase: true);
            ReturnType = function.ReturnType.ToString().ToLowerInvariant();
            ReturnObjectName = !string.IsNullOrEmpty(function.ReturnObjectName) ? function.ReturnObjectName : null;
            IsAggregate = function.IsAggregate;
            IsSafe = function.IsSafe;
            Formatters = function.Formatters.Length > 0 ? function.Formatters : null;
            Arguments = function.Arguments.Select(a => new GetFunctionInfoToolArgumentResponse(a)).ToList();
        }
    }

    internal sealed class GetFunctionInfoToolResponse
    {
        public List<GetFunctionInfoToolFunctionResponse> Functions { get; set; } = new();
    }

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

        if (!FunctionCallArgumentsReader.TryRead(arguments, ArgsKey, out var callArguments))
        {
            return McpCallToolResultModel.CreateError(Resources.Errors.McpInvalidFunctionArguments);
        }

        _logger.LogTrace("Get function info '{FunctionName}'.", name);

        var functions = thread.FunctionsManager.FindByName(name);
        if (functions.Length < 1)
        {
            return McpCallToolResultModel.CreateError(new CannotFindFunctionException(name).Message);
        }

        // The overload to call to get the rows input schema.
        var callFunction = thread.FunctionsManager.FindByName(name, callArguments.GetTypes()).FirstOrDefault();

        var response = new GetFunctionInfoToolResponse();
        foreach (var function in functions)
        {
            var functionResponse = new GetFunctionInfoToolFunctionResponse(function);
            if (IsRowsSourceFunction(function))
            {
                if (callFunction == null)
                {
                    functionResponse.ColumnsError = Resources.Errors.McpFunctionInfoArgumentsRequired;
                }
                else if (callFunction == function)
                {
                    await FillColumnsAsync(thread, function, callArguments, functionResponse, cancellationToken);
                }
            }
            response.Functions.Add(functionResponse);
        }

        var json = JsonSerializer.SerializeToElement(response,
            SourceGenerationContext.Default.GetFunctionInfoToolResponse);
        return new McpCallToolResultModel
        {
            Content =
            [
                new McpToolTextContentModel
                {
                    Text = json.ToString(),
                },
            ],
            StructuredContent = json,
        };
    }

    private static bool IsRowsSourceFunction(IFunction function)
        => !function.IsAggregate
           && function.ReturnType == DataType.Object
           && (function.ReturnObjectName.Equals(nameof(IRowsInput), StringComparison.OrdinalIgnoreCase)
               || function.ReturnObjectName.Equals(nameof(IRowsIterator), StringComparison.OrdinalIgnoreCase));

    private async Task FillColumnsAsync(
        IExecutionThread thread,
        IFunction function,
        FunctionCallArguments callArguments,
        GetFunctionInfoToolFunctionResponse functionResponse,
        CancellationToken cancellationToken)
    {
        IRowsInput? rowsInput = null;
        try
        {
            var result = await thread.FunctionsManager.CallFunctionAsync(function, thread, callArguments, cancellationToken);
            var obj = result.AsObject;
            if (obj is IRowsInput input)
            {
                rowsInput = input;
                if (rowsInput.Columns.Length > 0)
                {
                    functionResponse.Columns = GetColumns(rowsInput.Columns, rowsInput.GetKeyColumns().ToArray());
                    rowsInput = null;
                }
                else
                {
                    await rowsInput.OpenAsync(cancellationToken);
                    functionResponse.Columns = GetColumns(rowsInput.Columns, rowsInput.GetKeyColumns().ToArray());
                }
            }
            else if (obj is IRowsIterator iterator)
            {
                functionResponse.Columns = GetColumns(iterator.Columns, []);
            }
            else
            {
                functionResponse.ColumnsError = Resources.Errors.McpFunctionInfoInvalidRowsInput;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogDebug(e, "Cannot get columns schema of function '{FunctionName}'.", function.Name);
            functionResponse.ColumnsError = e.Message;
        }
        finally
        {
            if (rowsInput != null)
            {
                try
                {
                    await rowsInput.CloseAsync(cancellationToken);
                }
                catch (Exception e) when (e is not OperationCanceledException)
                {
                    _logger.LogWarning(e, "Cannot close rows input of function '{FunctionName}'.", function.Name);
                }
            }
        }
    }

    private static List<GetFunctionInfoToolColumnResponse> GetColumns(Column[] columns, KeyColumn[] keyColumns)
    {
        var result = new List<GetFunctionInfoToolColumnResponse>(columns.Length);
        for (var i = 0; i < columns.Length; i++)
        {
            var column = columns[i];
            if (column.IsHidden)
            {
                continue;
            }

            var columnKeys = Array.FindAll(keyColumns, kc => kc.ColumnIndex == i);
            var keyOperations = columnKeys
                .SelectMany(kc => kc.GetOperations())
                .Distinct()
                .Select(QueryAstVisitor.GetOperationString)
                .ToArray();
            result.Add(new GetFunctionInfoToolColumnResponse
            {
                Name = column.Name,
                Type = column.DataType.ToString().ToLowerInvariant(),
                Description = !string.IsNullOrEmpty(column.Description) ? column.Description : null,
                KeyOperations = keyOperations.Length > 0 ? keyOperations : null,
                IsKeyRequired = columnKeys.Length > 0 ? columnKeys.Any(kc => kc.IsRequired) : null,
            });
        }
        return result;
    }
}

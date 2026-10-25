using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Mcp.Models;
using QueryCat.Mcp.Utils;

namespace QueryCat.Mcp.Tools;

internal sealed class SearchFunctionsTool : IMcpTool
{
    private const string TermKey = "term";

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(SearchFunctionsTool));

    /// <inheritdoc />
    public McpToolModel Metadata { get; } = new(
        "search_functions",
        "Searches the existing function registry by term.")
    {
        Description = "Searches the existing function in part of function name or description.",
        InputSchema = new McpToolInputSchemaModel
        {
            Properties = new Dictionary<string, McpToolSchemaPropertyModel>
            {
                [TermKey] = new(McpToolSchemaPropertyModel.TypeString, "Search term")
            },
            Required = [TermKey],
        },
        Annotations = new McpToolAnnotationsModel
        {
            ReadOnlyHint = true,
            DestructiveHint = false,
            IdempotentHint = true,
            OpenWorldHint = false,
        },
    };

    internal sealed class SearchFunctionsToolFunctionResponse
    {
        public string Name { get; }

        public string Description { get; }

        public string Signature { get; }

        public float Relevance { get; }

        public SearchFunctionsToolFunctionResponse(IFunction function, float relevance)
        {
            Name = function.Name;
            Description = function.Description;
            Signature = FunctionFormatter.GetSignature(function, forceLowerCase: true);
            Relevance = relevance;
        }
    }

    internal sealed class SearchFunctionsToolResponse
    {
        public List<SearchFunctionsToolFunctionResponse> Functions { get; set; } = new();
    }

    /// <inheritdoc />
    public Task<McpCallToolResultModel> CallAsync(
        IExecutionThread thread,
        IReadOnlyDictionary<string, JsonElement> arguments,
        CancellationToken cancellationToken)
    {
        var term = ToolArgumentsUtils.GetString(arguments, TermKey);
        if (string.IsNullOrEmpty(term))
        {
            return Task.FromResult(McpCallToolResultModel.CreateError(Resources.Errors.McpTermRequired));
        }

        var response = new SearchFunctionsToolResponse();
        var termParts = term.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var function in thread.FunctionsManager.GetFunctions())
        {
            var relevance = 0.0f;
            foreach (var termPart in termParts)
            {
                if (function.Name.Contains(termPart, StringComparison.OrdinalIgnoreCase))
                {
                    relevance += 0.3f;
                }
                if (function.Description.Contains(termPart, StringComparison.OrdinalIgnoreCase))
                {
                    relevance += 0.15f;
                }
            }

            if (relevance > 0.0f)
            {
                response.Functions.Add(new SearchFunctionsToolFunctionResponse(function, relevance));
            }
        }
        response.Functions = response.Functions.OrderByDescending(x => x.Relevance).ToList();

        _logger.LogTrace("Search functions by term '{Term}'. Found candidates {CandidatesCount}.", term, response.Functions.Count);

        var json = JsonSerializer.SerializeToElement(response,
            SourceGenerationContext.Default.SearchFunctionsToolResponse);
        return Task.FromResult(McpCallToolResultModel.CreateStructured(json));
    }
}

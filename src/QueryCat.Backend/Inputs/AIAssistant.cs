using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Inputs;

/// <summary>
/// The class helps to execute natural language question query.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AIAssistant
{
    // ReSharper disable once InconsistentNaming
    public const string DefaultAIAgentVariableName = "_ANSWER_AGENT";

    /// <summary>
    /// Default instance of AI assistant.
    /// </summary>
    public static AIAssistant Default { get; set; } = new();

    public const string PromptPreamble =
        """
        You translate questions into QueryCat SQL. QueryCat is a SQL engine with a
        PostgreSQL-like dialect (it is NOT DuckDB, MySQL or SQLite).
        """;

    public const string PromptGuidelines =
        """
        == Rules
        1. Generate exactly one read-only SELECT statement (CTEs allowed). Never INSERT, UPDATE, DELETE, SET or DECLARE.
        2. Use only the tables and columns listed below. Reference tables by their identifier.
        3. Quote identifiers with double quotes ("my col"); use single quotes only for string literals.
        4. Available types for CAST / ::: integer, string, float, timestamp, boolean, numeric, interval, blob.
        5. Supported: WITH [RECURSIVE], window functions, DISTINCT ON, LIMIT/OFFSET, FETCH FIRST n ROWS,
           LIKE, SIMILAR TO (uses .NET regex). Use only those functions listed in Functions section.
        6. If the question cannot be answered from the given tables, do not guess; explain why in "Refusal".

        Respond with a single JSON object and nothing else (no markdown, no comments):
        {"Query": "<sql or empty>", "Refusal": "<reason or empty>"}
        Example: {"Query": "SELECT \"name\" FROM \"actors\" WHERE \"age\" >= 18", "Refusal": ""}
        """;

    private const int MaxQueryFixAttempts = 3;

    private readonly ILogger _logger = Application.LoggerFactory.CreateLogger(nameof(AIAssistant));

    /// <summary>
    /// Ask question model.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class AskAIRequest
    {
        /// <summary>
        /// Question text.
        /// </summary>
        public string Question { get; }

        /// <summary>
        /// Input to select data from.
        /// </summary>
        public IReadOnlyDictionary<string, IRowsInput> Inputs { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="question">Question text.</param>
        /// <param name="inputs">Inputs.</param>
        public AskAIRequest(string question, IReadOnlyDictionary<string, IRowsInput> inputs)
        {
            Question = question;
            Inputs = inputs;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="question">Question text.</param>
        /// <param name="inputs">Inputs.</param>
        public AskAIRequest(string question, params KeyValuePair<string, IRowsInput>[] inputs)
        {
            Question = question;
            Inputs = inputs.ToDictionary(k => k.Key, v => v.Value);
        }
    }

    /// <summary>
    /// Model for AI agent serialization/deserialization.
    /// </summary>
    public sealed class PromptResponseModel
    {
        public string Query { get; set; } = string.Empty;

        public string Refusal { get; set; } = string.Empty;

        public bool IsSuccess => !string.IsNullOrEmpty(Query);

        internal static ChatResponse CreateSuccessMessage(string sql)
            => new(
                JsonSerializer.Serialize(new PromptResponseModel
                {
                    Query = sql,
                }, SourceGenerationContext.Default.PromptResponseModel));

        /// <inheritdoc />
        public override string ToString() => $"Q: {Query}, R: {Refusal}";
    }

    /// <summary>
    /// Run question query for AI agent, generate SQL and execute it.
    /// </summary>
    /// <param name="request">AI request.</param>
    /// <param name="answerAgent">Answer agent.</param>
    /// <param name="thread">Execution token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Instance of <see cref="IRowsIterator" />.</returns>
    public async Task<IRowsIterator> RunQueryAsync(
        AskAIRequest request,
        IAnswerAgent answerAgent,
        IExecutionThread thread,
        CancellationToken cancellationToken = default)
    {
        var inputs = request.Inputs;

        // Ask AI.
        try
        {
            var iterator = await AiPromptingLoopAsync(request.Question, inputs, answerAgent, thread, cancellationToken);
            if (iterator == null)
            {
                throw new QueryCatException(Resources.Errors.CannotCreateIterator);
            }
            return iterator;
        }
        catch (Exception)
        {
            await CloseInputsAsync(inputs, cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Get the default answer agent from variables.
    /// </summary>
    /// <param name="thread">Execution thread.</param>
    /// <returns>Instance of <see cref="IAnswerAgent" />.</returns>
    public static IAnswerAgent GetDefaultAnswerAgent(IExecutionThread thread)
    {
        var answerAgentValue = thread.GetVariable(DefaultAIAgentVariableName);
        if (!answerAgentValue.IsNull
            && (answerAgentValue.Type == DataType.Object || answerAgentValue.Type == DataType.Dynamic)
            && answerAgentValue.AsObjectUnsafe is IAnswerAgent agent)
        {
            return agent;
        }

        throw new QueryCatException(
            string.Format(Resources.Errors.AnswerAgentNotFound, DefaultAIAgentVariableName, nameof(IAnswerAgent)));
    }

    /// <summary>
    /// Get all input variables.
    /// </summary>
    /// <param name="thread">Execution thread.</param>
    /// <returns>List of inputs.</returns>
    public static IList<KeyValuePair<string, IRowsInput>> GetInputs(IExecutionThread thread)
    {
        var inputs = new List<KeyValuePair<string, IRowsInput>>();
        foreach (var variable in thread.TopScope.Variables)
        {
            var rowsInputNamePair = RowsInputConverter.Convert(variable.Value);
            if (rowsInputNamePair.Value == null)
            {
                continue;
            }
            inputs.Add(new KeyValuePair<string, IRowsInput>(variable.Key, rowsInputNamePair.Value));
        }
        return inputs;
    }

    private async Task CloseInputsAsync(
        IReadOnlyDictionary<string, IRowsInput> inputs,
        CancellationToken cancellationToken)
    {
        foreach (var input in inputs)
        {
            await input.Value.CloseAsync(cancellationToken);
        }
    }

    private async Task<IRowsIterator?> AiPromptingLoopAsync(
        string question,
        IReadOnlyDictionary<string, IRowsInput> inputs,
        IAnswerAgent answerAgent,
        IExecutionThread thread,
        CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>();
        var canProceedQuery = false;
        var currentAiRequest = GetInitialQuestion(question, thread.FunctionsManager.GetFunctions(), inputs);
        messages.AddRange(currentAiRequest.Messages);
        var queryAttempts = 0;
        _logger.LogDebug("Prompt: {Prompt}.", currentAiRequest);
        while (!canProceedQuery)
        {
            var response = await answerAgent.AskAsync(new ChatRequest(messages.ToArray(), ChatRequest.TypeSql), cancellationToken);
            messages.AddRange(response.Messages);
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Resolver response: {Response}", response.ToString());
            }
            var model = ConvertAnswerToSql(response.Answer);
            canProceedQuery = model.IsSuccess;
            if (!canProceedQuery)
            {
                var userResponse = await ClarifyAsync(model.Refusal, cancellationToken);
                if (string.IsNullOrEmpty(userResponse))
                {
                    throw new QueryCatException(string.Format(Resources.Errors.AnswerAgentIssue, model.Refusal));
                }
                currentAiRequest = new ChatRequest(userResponse);
                messages.AddRange(currentAiRequest.Messages);
                continue;
            }

            VariantValue result;
            try
            {
                _logger.LogDebug("SQL to execute: {SQL}.", model.Query);
                result = await thread.RunAsync(
                    model.Query,
                    inputs.ToDictionary(k => k.Key, v => VariantValue.CreateFromObject(v.Value)),
                    cancellationToken);
            }
            catch (QueryCatException e)
            {
                queryAttempts++;
                canProceedQuery = false;
                _logger.LogTrace("Query attempt {AttemptCount}, Exception: {Error}", queryAttempts, e.Message);
                if (queryAttempts >= MaxQueryFixAttempts)
                {
                    throw new QueryCatException(
                        string.Format(Resources.Errors.CannotProcessMaxAttempts, queryAttempts));
                }
                currentAiRequest = new ChatRequest(GetPromptIssue(e.Message));
                messages.AddRange(currentAiRequest.Messages);
                continue;
            }

            return result.AsRequired<IRowsIterator>();
        }

        return null;
    }

    private static ChatRequest GetInitialQuestion(
        string question,
        IEnumerable<IFunction> functions,
        IReadOnlyDictionary<string, IRowsInput> inputs)
    {
        var messages = new ChatMessage[]
        {
            new(PromptPreamble, ChatMessage.RoleSystem),
            new(PromptGuidelines, ChatMessage.RoleSystem),
            new(GetFunctionsPreamble(functions), ChatMessage.RoleSystem),
            new(GetPromptTablesInformation(inputs)),
            new(GetPromptQuestion(question))
        };
        return new ChatRequest(messages, ChatRequest.TypeSql);
    }

    private static string GetFunctionsPreamble(IEnumerable<IFunction> functions)
    {
        var functionSection = new StringBuilder()
            .AppendLine("== Functions");
        foreach (var function in functions)
        {
            if (!function.IsSafe || function.Name.StartsWith('_') || function.Name == "ai_input")
            {
                continue;
            }
            functionSection.AppendLine("- " + FunctionFormatter.GetSignature(function, forceLowerCase: true));
        }
        return functionSection.ToString();
    }

    private PromptResponseModel ConvertAnswerToSql(string answer)
    {
        var startOfJson = answer.IndexOf('{');
        var endOfJson = answer.LastIndexOf('}');
        if (startOfJson == endOfJson
            || startOfJson < 0
            || endOfJson < 0)
        {
            throw new InvalidOperationException(string.Format(Resources.Errors.InvalidAgentResponse, answer));
        }
        var json = answer.Substring(startOfJson, endOfJson - startOfJson + 1);
        var model = JsonSerializer.Deserialize(
            json,
            SourceGenerationContext.Default.PromptResponseModel);
        if (model == null)
        {
            throw new InvalidOperationException(string.Format(Resources.Errors.InvalidAgentResponse, json));
        }
        return model;
    }

    /// <summary>
    /// Asks user to clarify input to help AI assistant to generate SQL.
    /// </summary>
    /// <param name="issue">Issue text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Answer for assistant.</returns>
    protected virtual Task<string> ClarifyAsync(string issue, CancellationToken cancellationToken)
    {
        // By default, provide no feedback that means query is failed.
        return Task.FromResult(string.Empty);
    }

    private static string Quote(string target, char quoteChar = '"')
        => StringUtils.Quote(target, quoteChar: quoteChar, force: true);

    public static string GetPromptTablesInformation(IReadOnlyDictionary<string, IRowsInput> inputs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("== Tables and Columns");
        foreach (var input in inputs)
        {
            sb.AppendFormat("=== Table identifier: {0}.", Quote(input.Key));
            sb.AppendLine();
            if (input.Value is IModelDescription modelDescription)
            {
                if (!string.IsNullOrEmpty(modelDescription.Name))
                {
                    sb.AppendFormat(" Table logical label: {0}.", Quote(modelDescription.Name, '\''));
                }
                if (!string.IsNullOrEmpty(modelDescription.Description))
                {
                    sb.AppendFormat(" Table description: {0}.", Quote(modelDescription.Description, '\''));
                }
            }
            sb.Append(" Columns:");
            sb.AppendLine();
            foreach (var column in input.Value.Columns)
            {
                if (column.IsHidden)
                {
                    continue;
                }
                sb.AppendFormat("- Column {0} of type '{1}';", Quote(column.Name), column.DataType);
                if (!string.IsNullOrEmpty(column.Description))
                {
                    sb.AppendFormat(" Description: {0};", Quote(column.Description, '\''));
                }
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Get string for prompt with user question.
    /// </summary>
    /// <param name="question">User question.</param>
    /// <returns>Prompt lines.</returns>
    public static string GetPromptQuestion(string question)
    {
        var sb = new StringBuilder();
        sb.AppendLine("== User Question");
        sb.AppendLine(question);
        return sb.ToString();
    }

    /// <summary>
    /// Get string for prompt with error.
    /// </summary>
    /// <param name="issue">Issue text.</param>
    /// <returns>Prompt lines.</returns>
    public static string GetPromptIssue(string issue)
    {
        var sb = new StringBuilder()
            .AppendLine(PromptGuidelines)
            .AppendLine("== Error")
            .AppendLine("The query failed with this error:")
            .AppendLine(issue)
            .AppendLine("Fix the query and respond with the same JSON object.");
        return sb.ToString();
    }
}

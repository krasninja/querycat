using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Inputs;

/// <summary>
/// The class helps to execute natural language question query.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AIAssistant
{
    /// <summary>
    /// Default instance of AI assistant.
    /// </summary>
    public static AIAssistant Default { get; set; } = new();

    public const string PromptPreamble =
        """
        You are the DuckDB expert.

        Please help to generate an SQL query to answer the question.
        Your response should ONLY be based on the given context and follow the response guidelines and format instructions.

        """;

    public const string PromptGuidelines =
        """
        == Response Guidelines
        1. If the provided context is sufficient, please generate a valid query without any explanations for the question.
        2. If the provided context is insufficient, please explain why it cannot be generated.
        3. Please use the most relevant table(s). Use table identifiers for the FROM clause.
        4. Please format the query before responding.
        5. The following types only are available: integer, string, float, timestamp, boolean, numeric, interval, BLOB.
        6. Please output in JSON format with the following structure: `{ "Query": "", "Refusal": "" }`.
           Put generated SQL into the `Query` field. If you cannot generate SQL, fill the `Refusal` field.
        7. If possible, use the following documentation to format the SQL query correctly: 'https://querycat.readthedocs.io/en/latest/'.

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

        /// <inheritdoc />
        public override string ToString() => $"Q: {Query}, R: {Refusal}";
    }

    public AIAssistant()
    {
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
        finally
        {
            await CloseInputsAsync(inputs, cancellationToken);
        }
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
        var canProceedQuery = false;
        var currentAiRequest = GetInitialQuestion(question, inputs);
        var queryAttempts = 0;
        while (!canProceedQuery)
        {
            _logger.LogTrace("Prompt: {Prompt}.", currentAiRequest.Message);
            var response = await answerAgent.AskAsync(currentAiRequest, cancellationToken);
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
                currentAiRequest = new QuestionRequest(userResponse);
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
                currentAiRequest = new QuestionRequest(GetPromptIssue(e.Message));
                continue;
            }

            return result.AsRequired<IRowsIterator>();
        }

        return null;
    }

    private static QuestionRequest GetInitialQuestion(
        string question,
        IReadOnlyDictionary<string, IRowsInput> inputs)
    {
        var messages = new QuestionMessage[]
        {
            new(PromptPreamble, QuestionMessage.RoleSystem),
            new(PromptGuidelines, QuestionMessage.RoleSystem),
            new(GetPromptTablesInformation(inputs)),
            new(GetPromptQuestion(question))
        };
        return new QuestionRequest(messages, QuestionRequest.TypeSql);
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

    private static string Quote(string target)
        => StringUtils.Quote(target, quote: "'", force: true);

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
                    sb.AppendFormat(" Table logical name: {0}.", Quote(modelDescription.Name));
                }
                if (!string.IsNullOrEmpty(modelDescription.Description))
                {
                    sb.AppendFormat(" Table description: {0}.", Quote(modelDescription.Name));
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
                    sb.AppendFormat(" Description: {0};", Quote(column.Description));
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
        var sb = new StringBuilder();
        sb.AppendLine(PromptGuidelines);
        sb.AppendLine("== Error");
        sb.AppendLine("I was not able to run the generated SQL. Try to re-format the SQL. The error is below:");
        sb.AppendLine(issue);
        return sb.ToString();
    }
}

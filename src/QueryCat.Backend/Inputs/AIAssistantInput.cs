using System.ComponentModel;
using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Storage;

namespace QueryCat.Backend.Inputs;

// ReSharper disable once InconsistentNaming
internal sealed class AIAssistantInput : IRowsInput, IRowsIteratorParent
{
    [SafeFunction]
    [Description("Uses AI to convert question into SQL and run the query.")]
    [FunctionSignature("ai_input(question: string, ...source?: any[]): object<IRowsInput>")]
    // ReSharper disable once InconsistentNaming
    public static async ValueTask<VariantValue> AIInput(IExecutionThread thread, CancellationToken cancellationToken)
    {
        var question = thread.Stack[0].AsString;
        IAnswerAgent? answerAgent = null;

        var inputs = new List<KeyValuePair<string, IRowsInput>>();
        for (var i = 1; i < thread.Stack.FrameLength; i++)
        {
            var source = thread.Stack[i];

            // Special case. Here we can override answer agent, or use default.
            if ((source.Type == DataType.Object || source.Type == DataType.Dynamic)
                && source.AsObjectUnsafe is IAnswerAgent agent)
            {
                answerAgent = agent;
                continue;
            }

            KeyValuePair<string, IRowsInput?> rowsInputNamePair = new KeyValuePair<string, IRowsInput?>(string.Empty, null);

            // We can try to resolve strings to inputs.
            if (source.Type == DataType.String)
            {
                rowsInputNamePair = await RowsInputConverter.ResolveInputAsync(thread, source.AsString, cancellationToken);
            }
            if (rowsInputNamePair.Value != null)
            {
                rowsInputNamePair = RowsInputConverter.Convert(source);
            }

            if (rowsInputNamePair.Value != null)
            {
                inputs.Add(new KeyValuePair<string, IRowsInput>(rowsInputNamePair.Key, rowsInputNamePair.Value));
            }
        }

        if (answerAgent == null)
        {
            answerAgent = AIAssistant.GetDefaultAnswerAgent(thread);
        }
        // No inputs provided? Let's search within current variables.
        if (inputs.Count == 0)
        {
            inputs.AddRange(AIAssistant.GetInputs(thread));
        }

        var aiInput = new AIAssistantInput(AIAssistant.Default, answerAgent, question, thread, inputs.ToArray());
        return VariantValue.CreateFromObject(aiInput);
    }

    private readonly AIAssistant _assistant;
    private readonly IAnswerAgent _answerAgent;
    private readonly string _question;
    private readonly IExecutionThread _thread;
    private readonly KeyValuePair<string, IRowsInput>[] _inputs;
    private IRowsIterator _rowsIterator = EmptyIterator.Instance;

    /// <inheritdoc />
    public QueryContext QueryContext { get; set; } = NullQueryContext.Instance;

    /// <inheritdoc />
    public Column[] Columns => _rowsIterator.Columns;

    /// <inheritdoc />
    public string[] UniqueKey => [_question];

    public AIAssistantInput(
        AIAssistant assistant,
        IAnswerAgent answerAgent,
        string question,
        IExecutionThread thread,
        params KeyValuePair<string, IRowsInput>[] inputs)
    {
        if (inputs.Length == 0)
        {
            throw new ArgumentException(Resources.Errors.NoInputs, nameof(inputs));
        }

        _assistant = assistant;
        _answerAgent = answerAgent;
        _question = question;
        _thread = thread;
        _inputs = inputs;
    }

    /// <inheritdoc />
    public async Task OpenAsync(CancellationToken cancellationToken = default)
    {
        _rowsIterator = await _assistant.RunQueryAsync(
            new AIAssistant.AskAIRequest(_question, _inputs),
            _answerAgent,
            _thread,
            cancellationToken);
    }

    /// <inheritdoc />
    public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task ResetAsync(CancellationToken cancellationToken = default) => OpenAsync(cancellationToken);

    /// <inheritdoc />
    public IReadOnlyList<KeyColumn> GetKeyColumns() => [];

    /// <inheritdoc />
    public void SetKeyColumnValue(int columnIndex, VariantValue value, VariantValue.Operation operation)
    {
    }

    /// <inheritdoc />
    public void UnsetKeyColumnValue(int columnIndex, VariantValue.Operation operation)
    {
    }

    /// <inheritdoc />
    public ErrorCode ReadValue(int columnIndex, out VariantValue value)
    {
        value = _rowsIterator.Current[columnIndex];
        return ErrorCode.OK;
    }

    /// <inheritdoc />
    public ValueTask<bool> ReadNextAsync(CancellationToken cancellationToken = default)
        => _rowsIterator.MoveNextAsync(cancellationToken);

    /// <inheritdoc />
    public IEnumerable<IRowsSchema> GetChildren()
    {
        foreach (var input in _inputs)
        {
            yield return input.Value;
        }
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendRowsInputsWithIndent($"AI ({_question})",
            _inputs.Select(i => i.Value).ToArray());
    }
}

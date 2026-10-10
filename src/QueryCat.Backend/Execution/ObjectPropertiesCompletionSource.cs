using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The completion source that uses reflection to get properties from object types.
/// </summary>
public class ObjectPropertiesCompletionSource : BaseObjectPropertiesCompletionSource
{
    /// <inheritdoc />
    protected override async ValueTask<object?> GetSourceObjectAsync(CompletionContext context, CancellationToken cancellationToken)
    {
        // The base pattern is "id.". It means, at least we should have 2 tokens.
        var separatorTokenIndex = context.TriggerTokens.LastSeparatorTokenIndex;
        if (context.TriggerTokens.Count < 2
            || separatorTokenIndex == context.TriggerTokens.Count - 1)
        {
            return null;
        }

        var termTokens = context.TriggerTokens.GetRange(separatorTokenIndex + 1);
        var (objectSelectExpression, _) = GetObjectExpressionAndTerm(termTokens);

        var value = await RunAsync(context.ExecutionThread, objectSelectExpression, cancellationToken: cancellationToken);
        if (!value.IsNull && value.Type == DataType.Object)
        {
            return value.AsObjectUnsafe;
        }

        return null;
    }

    protected static async ValueTask<VariantValue> RunAsync(
        IExecutionThread executionThread,
        string expression,
        CancellationToken cancellationToken)
    {
        VariantValue value = VariantValue.Null;
        try
        {
            if (executionThread is DefaultExecutionThread defaultExecutionThread)
            {
                value = await defaultExecutionThread.EvaluateForCompletionAsync(expression,
                    cancellationToken: cancellationToken);
            }
            else
            {
                value = await executionThread.RunAsync(expression, cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
        }

        return value;
    }
}

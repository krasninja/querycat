﻿using QueryCat.Backend.Core;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Execution;

/// <summary>
/// The completion source that uses reflection to get properties from object types.
/// </summary>
public class ObjectPropertiesCompletionSource : BaseObjectPropertiesCompletionSource
{
    /// <inheritdoc />
    protected override object? GetSourceObject(CompletionContext context)
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

        try
        {
            var value = context.ExecutionThread.Run(objectSelectExpression);
            if (!value.IsNull && value.GetInternalType() == DataType.Object)
            {
                return value.AsObjectUnsafe;
            }
        }
        catch (QueryCatException)
        {
        }

        return null;
    }
}

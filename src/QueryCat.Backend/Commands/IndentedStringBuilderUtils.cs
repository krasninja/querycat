using QueryCat.Backend.Core.Data;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Utils for <see cref="IndentedStringBuilder" />.
/// </summary>
internal static class IndentedStringBuilderUtils
{
    public static IndentedStringBuilder AppendSubQueriesWithIndent(
        IndentedStringBuilder stringBuilder,
        IFuncUnit funcUnit)
    {
        if (funcUnit is IRowsIteratorParent funcUnitDelegate)
        {
            stringBuilder.IncreaseIndent();
            foreach (var rowsIterator in funcUnitDelegate.GetChildren())
            {
                if (rowsIterator is IRowsIterator rowsIteratorDelegate)
                {
                    rowsIteratorDelegate.Explain(stringBuilder);
                }
            }
            stringBuilder.DecreaseIndent();
        }

        return stringBuilder;
    }

    public static IndentedStringBuilder AppendSubQueriesWithIndent(
        this IndentedStringBuilder stringBuilder,
        IEnumerable<IFuncUnit> funcUnits)
    {
        foreach (var funcUnit in funcUnits)
        {
            AppendSubQueriesWithIndent(stringBuilder, funcUnit);
        }
        return stringBuilder;
    }
}

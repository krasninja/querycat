using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Functions;

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
        if (funcUnit is FuncUnitDelegate funcUnitDelegate && funcUnitDelegate.SubQueryIterators != null)
        {
            stringBuilder.IncreaseIndent();
            foreach (var rowsIterator in funcUnitDelegate.SubQueryIterators)
            {
                rowsIterator.Explain(stringBuilder);
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

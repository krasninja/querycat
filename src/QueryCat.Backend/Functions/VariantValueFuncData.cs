using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;

namespace QueryCat.Backend.Functions;

/// <summary>
/// Context for delegates execution. When we generate execution delegate for a node (expression node),
/// we need to pass custom environment. For example, sometimes we need to execute delegate with another rows iterator (cached)
/// instead of the original one.
/// </summary>
public class VariantValueFuncData
{
    /// <summary>
    /// Current rows iterator.
    /// </summary>
    public IRowsIterator RowsIterator { get; }

    public static VariantValueFuncData Empty { get; } = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    public VariantValueFuncData() : this(EmptyIterator.Instance)
    {
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="rowsIterator">Rows iterator to use.</param>
    public VariantValueFuncData(in IRowsIterator rowsIterator)
    {
        RowsIterator = rowsIterator;
    }
}

using System.Runtime.CompilerServices;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Relational.Iterators;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

/// <summary>
/// The class is the delegate implementation for QueryCat project. It contains all the data
/// needed to run the node delegate and get the result.
/// </summary>
public class FuncUnit
{
    private readonly VariantValueFunc _func;
    private VariantValueFuncData _data;

    /// <summary>
    /// Data to be used for func invocation.
    /// </summary>
    public VariantValueFuncData Data => _data;

    public IRowsIterator[] SubQueryIterators { get; internal set; } = Array.Empty<IRowsIterator>();

    public FuncUnit(VariantValue value)
    {
        _func = _ => value;
        _data = VariantValueFuncData.Empty;
    }

    public FuncUnit(VariantValueFunc func)
    {
        _func = func;
        _data = new VariantValueFuncData(EmptyIterator.Instance);
    }

    /// <summary>
    /// Set the iterator to get current row value on SELECT iteration stage.
    /// </summary>
    /// <param name="rowsIterator">Rows iterator.</param>
    public void SetIterator(IRowsIterator rowsIterator)
    {
        _data = new VariantValueFuncData(rowsIterator);
    }

    /// <summary>
    /// Invoke the func delegate.
    /// </summary>
    /// <returns>Invocation result.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public VariantValue Invoke() => _func(_data);

    /// <summary>
    /// Set the iterator to get current row value on SELECT iteration stage.
    /// </summary>
    /// <param name="funcUnits">Delegates.</param>
    /// <param name="rowsIterator">Rows iterator.</param>
    public static void SetIterator(IEnumerable<FuncUnit> funcUnits, IRowsIterator rowsIterator)
    {
        foreach (var funcUnit in funcUnits)
        {
            funcUnit.SetIterator(rowsIterator);
        }
    }
}

using QueryCat.Backend.Types;
using QueryCat.Backend.Utils;

namespace QueryCat.Backend.Relational.Iterators;

/// <summary>
/// The iterator converts LINQ <see cref="IEnumerable{T}" /> into iterator.
/// </summary>
/// <typeparam name="TClass">Class type.</typeparam>
public sealed class EnumerableRowsIterator<TClass> : IRowsIterator
{
    private readonly IEnumerator<TClass> _enumerator;
    private readonly Func<TClass, VariantValue>[] _valuesGetters;
    private readonly Row _row;

    /// <inheritdoc />
    public Column[] Columns { get; }

    /// <inheritdoc />
    public Row Current => _row;

    public EnumerableRowsIterator(Column[] columns, Func<TClass, VariantValue>[] valuesGetters, IEnumerable<TClass> enumerable)
    {
        _valuesGetters = valuesGetters;
        Columns = columns;
        _row = new Row(this);
        _enumerator = enumerable.GetEnumerator();
    }

    /// <inheritdoc />
    public bool MoveNext()
    {
        var hasData = _enumerator.MoveNext();
        hasData = hasData && _enumerator.Current != null;
        if (hasData)
        {
            for (int i = 0; i < Columns.Length; i++)
            {
                _row[i] = _valuesGetters[i].Invoke(_enumerator.Current!);
            }
        }
        return hasData;
    }

    /// <inheritdoc />
    public void Reset()
    {
        _enumerator.Reset();
    }

    /// <inheritdoc />
    public void Explain(IndentedStringBuilder stringBuilder)
    {
        stringBuilder.AppendLine("Enumerable");
    }
}

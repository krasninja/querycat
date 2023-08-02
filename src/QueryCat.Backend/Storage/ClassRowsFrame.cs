using QueryCat.Backend.Abstractions;
using QueryCat.Backend.Relational;
using QueryCat.Backend.Types;

namespace QueryCat.Backend.Storage;

/// <summary>
/// Rows frame that contains objects of specific class.
/// </summary>
/// <typeparam name="TClass">Class type.</typeparam>
public class ClassRowsFrame<TClass> : RowsFrame where TClass : class
{
    private readonly Func<TClass, VariantValue>[] _valuesGetters;
    private readonly Row _row;

    internal ClassRowsFrame(
        RowsFrameOptions options,
        Column[] columns,
        params Func<TClass, VariantValue>[] valuesGetters) : base(options, columns)
    {
        _valuesGetters = valuesGetters;
        _row = new Row(this);
    }

    /// <summary>
    /// Add object.
    /// </summary>
    /// <param name="obj">Object to add.</param>
    public void AddRow(TClass obj)
    {
        for (var i = 0; i < Columns.Length; i++)
        {
            _row[i] = _valuesGetters[i].Invoke(obj);
        }
        AddRow(_row);
    }

    /// <summary>
    /// Add several objects.
    /// </summary>
    /// <param name="objects">Objects to add.</param>
    public void AddRows(IEnumerable<TClass> objects)
    {
        foreach (var obj in objects)
        {
            for (var i = 0; i < Columns.Length; i++)
            {
                _row[i] = _valuesGetters[i].Invoke(obj);
            }
            AddRow(_row);
        }
    }
}

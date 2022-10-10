using QueryCat.Backend.Types;

namespace QueryCat.Backend.Relational;

/// <summary>
/// Rows frame that contains objects of specific class.
/// </summary>
/// <typeparam name="TClass">Class type.</typeparam>
public class ClassRowsFrame<TClass> : RowsFrame where TClass : class
{
    private readonly Func<TClass, VariantValue>[] _valuesGetters;
    private readonly Row _row;

    public static ClassRowsFrame<TClass> Empty { get; } = new(
        new RowsFrameOptions(),
        Array.Empty<Column>(),
        _ => VariantValue.Null
    );

    internal ClassRowsFrame(
        RowsFrameOptions options,
        Column[] columns,
        params Func<TClass, VariantValue>[] valuesGetters) : base(options, columns)
    {
        _valuesGetters = valuesGetters;
        _row = new Row(this);
    }

    public void AddRow(TClass obj)
    {
        for (int i = 0; i < Columns.Length; i++)
        {
            _row[i] = _valuesGetters[i].Invoke(obj);
        }
        AddRow(_row);
    }

    public void AddRows(IEnumerable<TClass> objects)
    {
        foreach (var obj in objects)
        {
            for (int i = 0; i < Columns.Length; i++)
            {
                _row[i] = _valuesGetters[i].Invoke(obj);
            }
            AddRow(_row);
        }
    }
}

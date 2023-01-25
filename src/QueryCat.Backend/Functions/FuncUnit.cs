using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

internal abstract class FuncUnit : IFuncUnit
{
    public const int SubqueriesRowsIterators = 10;

    private IDictionary<int, object>? _objects;

    /// <inheritdoc />
    public object? GetData(int index)
    {
        return _objects?[index];
    }

    /// <inheritdoc />
    public void SetData(int index, object obj)
    {
        _objects ??= new SortedList<int, object>();
        _objects[index] = obj;
    }

    /// <inheritdoc />
    public abstract VariantValue Invoke();
}

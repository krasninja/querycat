using QueryCat.Backend.Types;

namespace QueryCat.Backend.Functions;

internal abstract class FuncUnit : IFuncUnit
{
    public const int SubqueriesRowsIterators = 10;

    private IDictionary<int, object>? _objects;

    /// <inheritdoc />
    public object? GetData(int index)
    {
        if (_objects == null)
        {
            return null;
        }
        return _objects[index];
    }

    /// <inheritdoc />
    public void SetData(int index, object obj)
    {
        if (_objects == null)
        {
            _objects = new SortedDictionary<int, object>();
        }
        _objects[index] = obj;
    }

    /// <inheritdoc />
    public abstract VariantValue Invoke();
}

using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.FunctionsManager;

internal abstract class FuncUnit : IFuncUnit
{
    public const int SubqueriesRowsIterators = 10;

    private IDictionary<int, object>? _objects;

    /// <inheritdoc />
    public abstract DataType OutputType { get; }

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

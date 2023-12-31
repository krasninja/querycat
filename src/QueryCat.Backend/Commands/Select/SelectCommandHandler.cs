using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands.Select;

internal sealed class SelectCommandHandler : IFuncUnit, IDisposable
{
    public SelectCommandContext SelectCommandContext { get; }

    /// <inheritdoc />
    public DataType OutputType => DataType.Null;

    public SelectCommandHandler(SelectCommandContext selectCommandContext)
    {
        SelectCommandContext = selectCommandContext;
    }

    /// <inheritdoc />
    public VariantValue Invoke()
    {
        return VariantValue.CreateFromObject(SelectCommandContext.CurrentIterator);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SelectCommandContext.Dispose();
    }
}

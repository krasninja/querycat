using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

internal sealed class EmptyCommandHandler : IFuncUnit
{
    public static EmptyCommandHandler Empty { get; } = new();

    /// <inheritdoc />
    public DataType OutputType => DataType.Null;

    /// <inheritdoc />
    public VariantValue Invoke()
    {
        return VariantValue.Null;
    }
}

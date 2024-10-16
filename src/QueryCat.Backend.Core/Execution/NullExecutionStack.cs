using System.Collections;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Execution stack with no implementation.
/// </summary>
public sealed class NullExecutionStack : IExecutionStack
{
    /// <summary>
    /// Instance of <see cref="NullExecutionStack" />.
    /// </summary>
    public static IExecutionStack Instance { get; } = new NullExecutionStack();

    /// <inheritdoc />
    public VariantValue this[int index]
    {
        get => VariantValue.Null;
        set { }
    }

    /// <inheritdoc />
    public int FrameLength => 0;

    /// <inheritdoc />
    public void CreateFrame()
    {
    }

    /// <inheritdoc />
    public void CloseFrame()
    {
    }

    /// <inheritdoc />
    public void Push(VariantValue value)
    {
    }

    /// <inheritdoc />
    public VariantValue Pop() => default;

    /// <inheritdoc />
    public IEnumerator<VariantValue> GetEnumerator()
    {
        yield break;
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

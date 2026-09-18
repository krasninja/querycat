namespace QueryCat.Backend.Core.Types;

/// <summary>
/// Array of <see cref="VariantValue" /> with Equals, GetHashCode implementation.
/// </summary>
/// <remarks>
/// The struct is not read only for VariantValue array items. The internal state
/// should not be mutated because the hash is frozen at construction.
/// </remarks>
internal readonly struct VariantValueArray : IEquatable<VariantValueArray>, ICloneable
{
    /// <summary>
    /// Empty array instance.
    /// </summary>
    public static VariantValueArray Empty { get; } = new([]);

    private readonly VariantValue[] _values;
    private readonly int _hashCode;

    public ref readonly VariantValue this[int index] => ref Values[index];

    private VariantValue[] Values => _values ?? [];

    /// <summary>
    /// Array length.
    /// </summary>
    public int Length => Values.Length;

    public VariantValueArray(params VariantValue[] values)
    {
        _values = values ?? [];;

        var hashCode = default(HashCode);
        for (var i = 0; i < _values.Length; i++)
        {
            hashCode.Add(_values[i].GetHashCode());
        }
        _hashCode = hashCode.ToHashCode();
    }

    public VariantValueArray() : this([])
    {
    }

    public VariantValueArray(IEnumerable<VariantValue> values) : this(values.ToArray())
    {
    }

    public VariantValueArray(VariantValueArray variantValueArray)
        : this((VariantValue[])(variantValueArray._values ?? []).Clone())
    {
    }

    /// <summary>
    /// Creates an independent copy — safe to use as a hash key.
    /// </summary>
    /// <param name="values">Values.</param>
    /// <returns>Copy of values.</returns>
    public static VariantValueArray CopyOf(ReadOnlySpan<VariantValue> values) => new(values.ToArray());

    public ReadOnlySpan<VariantValue> AsSpan() => _values;

    /// <inheritdoc />
    public bool Equals(VariantValueArray other)
    {
        var left = _values;
        var right = other._values;
        return left.AsSpan().SequenceEqual(right);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is VariantValueArray other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _values is null ? Empty._hashCode : _hashCode;

    /// <inheritdoc />
    public override string ToString() => string.Join("; ", Values);

    /// <inheritdoc />
    public object Clone() => new VariantValueArray(this);

    public static bool operator ==(VariantValueArray left, VariantValueArray right)
        => left.Equals(right);

    public static bool operator !=(VariantValueArray left, VariantValueArray right)
        => !(left == right);

    public static implicit operator VariantValue[](VariantValueArray value) => value.Values;
}

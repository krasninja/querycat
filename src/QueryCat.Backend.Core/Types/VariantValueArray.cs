namespace QueryCat.Backend.Core.Types;

/// <summary>
/// Array of <see cref="VariantValue" /> with Equals, GetHashCode implementation.
/// </summary>
public readonly struct VariantValueArray(params VariantValue[] values) : IEquatable<VariantValueArray>
{
    public static VariantValueArray Empty { get; } = new(0);

    private readonly VariantValue[] _values = values;

    public VariantValue this[int index] => _values[index];

    public VariantValueArray(IEnumerable<VariantValue> values) : this(values.ToArray())
    {
    }

    internal VariantValueArray(int size) : this(new VariantValue[size])
    {
    }

    public VariantValueArray(VariantValueArray variantValueArray) : this(variantValueArray._values.Length)
    {
        Array.Copy(variantValueArray._values, _values, variantValueArray._values.Length);
    }

    /// <summary>
    /// Make every value NULL.
    /// </summary>
    public void Clear()
    {
        Array.Fill(_values, VariantValue.Null);
    }

    /// <inheritdoc />
    public bool Equals(VariantValueArray other)
    {
        if (other._values.Length != _values.Length)
        {
            return false;
        }

        for (var i = 0; i < _values.Length; i++)
        {
            if (!other._values[i].Equals(_values[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is VariantValueArray other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        foreach (var value in _values)
        {
            hashCode.Add(value);
        }
        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() => string.Join("; ", _values);

    public static bool operator ==(VariantValueArray left, VariantValueArray right)
        => left.Equals(right);

    public static bool operator !=(VariantValueArray left, VariantValueArray right)
        => !(left == right);

    public static implicit operator VariantValue[](VariantValueArray value) => value._values;
}

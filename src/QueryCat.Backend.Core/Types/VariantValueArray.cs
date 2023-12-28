using System.Text;

namespace QueryCat.Backend.Core.Types;

/// <summary>
/// Array of <see cref="VariantValue" /> with Equals implementation.
/// </summary>
public sealed class VariantValueArray
{
    private VariantValue[] _values;

    public static VariantValueArray Empty { get; } = new(Array.Empty<VariantValue>());

    /// <summary>
    /// Values array.
    /// </summary>
    public VariantValue[] Values => _values;

    public VariantValueArray(params VariantValue[] values)
    {
        _values = values;
    }

    public VariantValueArray(IEnumerable<VariantValue> values) : this(values.ToArray())
    {
    }

    public VariantValueArray(int size)
    {
        _values = new VariantValue[size];
    }

    public VariantValueArray(VariantValueArray variantValueArray) : this(variantValueArray._values.Length)
    {
        Array.Copy(variantValueArray._values, _values, variantValueArray._values.Length);
    }

    /// <summary>
    /// Ensure that array has enough size. Resize if needed.
    /// </summary>
    /// <param name="newSize">New size.</param>
    public void EnsureResize(int newSize)
    {
        if (newSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize));
        }
        if (newSize <= _values.Length)
        {
            return;
        }
        Array.Resize(ref _values, newSize);
    }

    /// <summary>
    /// Make every value NULL.
    /// </summary>
    public void Clear()
    {
        Array.Fill(_values, VariantValue.Null);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not VariantValueArray variantValues)
        {
            return false;
        }
        if (variantValues._values.Length != _values.Length)
        {
            return false;
        }

        for (var i = 0; i < _values.Length; i++)
        {
            if (!variantValues._values[i].Equals(_values[i]))
            {
                return false;
            }
        }
        return true;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hashCode = default(HashCode);
        for (var i = 0; i < _values.Length; i++)
        {
            hashCode.Add(_values[i]);
        }
        return hashCode.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < _values.Length; i++)
        {
            sb.Append(_values[i].ToString());
            if (i < _values.Length - 1)
            {
                sb.Append("; ");
            }
        }
        return sb.ToString();
    }

    public static bool operator ==(VariantValueArray left, VariantValueArray right)
        => left.Equals(right);

    public static bool operator !=(VariantValueArray left, VariantValueArray right)
        => !(left == right);
}

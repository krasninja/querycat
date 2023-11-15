using System.Text;
using QueryCat.Backend.Core.Functions;

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

    public VariantValueArray(params IFuncUnit[] functions) : this(size: functions.Length)
    {
        for (var i = 0; i < functions.Length; i++)
        {
            _values[i] = functions[i].Invoke();
        }
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

    public void Resize(int newSize)
    {
        if (newSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(newSize));
        }
        if (newSize == _values.Length)
        {
            return;
        }
        var oldValues = _values;
        _values = new VariantValue[newSize];
        Array.Copy(oldValues, _values, oldValues.Length);
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

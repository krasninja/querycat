using QueryCat.Backend.Core;

namespace QueryCat.Backend.Commands.Declare;

/// <summary>
/// Occurs when value cannot be set by expression.
/// </summary>
public sealed class CannotSetValueException : QueryCatException
{
    public CannotSetValueException(string name) : base(string.Format(Resources.Errors.CannotSetValue, name))
    {
    }
}

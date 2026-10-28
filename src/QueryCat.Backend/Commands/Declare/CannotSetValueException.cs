using QueryCat.Backend.Core;

namespace QueryCat.Backend.Commands.Declare;

/// <summary>
/// Occurs when value cannot be set by expression.
/// </summary>
public sealed class CannotSetValueException : QueryCatException
{
    /// <summary>
    /// Identifier or object expression name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Identifier or object expression name.</param>
    /// <param name="innerException">Inner exception.</param>
    public CannotSetValueException(string name, Exception? innerException)
        : base(string.Format(Resources.Errors.CannotSetValue, name), innerException)
    {
        Name = name;
    }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Identifier or object expression name.</param>
    public CannotSetValueException(string name) : this(string.Format(Resources.Errors.CannotSetValue, name), null)
    {
    }
}

using QueryCat.Backend.Core.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Base class for commands.
/// </summary>
public abstract class CommandHandler : IDisposable
{
    /// <summary>
    /// Invoke the command and return value.
    /// </summary>
    /// <returns>Command result or null.</returns>
    public abstract VariantValue Invoke();

    public Func<VariantValue> AsFunc() => Invoke;

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

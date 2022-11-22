using QueryCat.Backend.Types;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Base class for commands.
/// </summary>
public abstract class CommandContext : IDisposable
{
    /// <summary>
    /// Invoke the command and return value.
    /// </summary>
    /// <returns>Command result or null.</returns>
    public abstract VariantValue Invoke();

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

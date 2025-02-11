namespace QueryCat.Backend.Core.Indexes;

/// <summary>
/// General index interface.
/// </summary>
public interface IIndex
{
    /// <summary>
    /// Recreate the index values.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Awaitable task.</returns>
    Task RebuildAsync(CancellationToken cancellationToken = default);
}

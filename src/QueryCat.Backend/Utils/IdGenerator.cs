namespace QueryCat.Backend.Utils;

/// <summary>
/// Global next identifier generator class.
/// </summary>
internal static class IdGenerator
{
    private static int _nextId = 1;

    /// <summary>
    /// Get the next identifier.
    /// </summary>
    /// <param name="offset">Optional offset.</param>
    /// <returns>Next id.</returns>
    public static int GetNext(int offset = 0) => offset + Interlocked.Increment(ref _nextId);
}

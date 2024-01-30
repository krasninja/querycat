namespace QueryCat.Backend.Commands;

internal class CommandContext
{
    private static int _nextId;

    /// <summary>
    /// The identifier used to distinguish command contexts between each other.
    /// </summary>
    internal int Id { get; } = Interlocked.Increment(ref _nextId);
}

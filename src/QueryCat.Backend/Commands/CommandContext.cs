using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Commands;

/// <summary>
/// Base command context that is used to store current command state.
/// </summary>
internal abstract class CommandContext
{
    /// <summary>
    /// The identifier used to distinguish command contexts between each other.
    /// </summary>
    internal int Id { get; } = IdGenerator.GetNext();
}

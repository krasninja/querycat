namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Represents a tool call requested by an AI agent.
/// </summary>
public class ChatToolCall
{
    /// <summary>
    /// Unique tool call identifier.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Tool name to call.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Tool call arguments as JSON.
    /// </summary>
    public string Arguments { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="id">Unique tool call identifier.</param>
    /// <param name="name">Tool name.</param>
    /// <param name="arguments">Tool call arguments as JSON.</param>
    public ChatToolCall(string id, string name, string arguments)
    {
        Id = id;
        Name = name;
        Arguments = arguments;
    }
}

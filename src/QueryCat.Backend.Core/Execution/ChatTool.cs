using System.Text.Json;

namespace QueryCat.Backend.Core.Execution;

/// <summary>
/// Describes a tool that an AI agent can call.
/// </summary>
public class ChatTool
{
    /// <summary>
    /// Tool name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Tool description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// JSON schema describing the tool parameters.
    /// </summary>
    public string ParametersSchema { get; }

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="name">Tool name.</param>
    /// <param name="description">Tool description.</param>
    /// <param name="parametersSchema">JSON schema for tool parameters.</param>
    public ChatTool(string name, string description, string parametersSchema)
    {
        Name = name;
        Description = description;
        ParametersSchema = parametersSchema;
    }
}

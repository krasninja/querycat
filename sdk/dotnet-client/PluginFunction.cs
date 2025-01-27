using System;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Simplified function representation for plugin manager.
/// </summary>
internal sealed class PluginFunction : IFunction
{
    /// <inheritdoc />
    public Delegate Delegate { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;

    /// <inheritdoc />
    public DataType ReturnType { get; set; } = DataType.Void;

    /// <inheritdoc />
    public string ReturnObjectName { get; set; } = string.Empty;

    /// <inheritdoc />
    public bool IsAggregate { get; set; }

    /// <inheritdoc />
    public FunctionSignatureArgument[] Arguments { get; set; } = [];

    /// <inheritdoc />
    public bool IsSafe { get; set; }

    /// <inheritdoc />
    public string[] Formatters { get; set; } = [];

    /// <summary>
    /// Function full signature.
    /// </summary>
    public string Signature { get; set; }

    public PluginFunction(Delegate @delegate, string signature, FunctionMetadata? metadata = null)
    {
        Delegate = @delegate;
        Signature = signature;
        if (metadata != null)
        {
            Description = metadata.Description;
            IsSafe = metadata.IsSafe;
            IsAggregate = metadata.IsAggregate;
            Formatters = metadata.Formatters;
        }
        var firstBracketIndex = signature.IndexOf('(');
        Name = firstBracketIndex > -1 ? signature.Substring(0, firstBracketIndex).ToUpperInvariant() : "Unknown";
    }
}

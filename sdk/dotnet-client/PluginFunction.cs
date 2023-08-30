using System;
using QueryCat.Backend.Core.Functions;
using QueryCat.Backend.Core.Types;

namespace QueryCat.Plugins.Client;

/// <summary>
/// Simplified function representation for plugin manager.
/// </summary>
public sealed class PluginFunction : IFunction
{
    /// <inheritdoc />
    public FunctionDelegate Delegate { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <summary>
    /// Function signature.
    /// </summary>
    public string Signature { get; }

    /// <inheritdoc />
    public string Description { get; set; } = string.Empty;

    /// <inheritdoc />
    public DataType ReturnType { get; set; } = DataType.Void;

    /// <inheritdoc />
    public string ReturnObjectName { get; } = string.Empty;

    /// <inheritdoc />
    public bool IsAggregate { get; } = false;

    /// <inheritdoc />
    public FunctionSignatureArgument[] Arguments { get; set; } = Array.Empty<FunctionSignatureArgument>();

    public PluginFunction(string name, string signature, FunctionDelegate @delegate)
    {
        Name = name;
        Signature = signature;
        Delegate = @delegate;
    }
}

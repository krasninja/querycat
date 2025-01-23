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
    public Delegate Delegate { get; }

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
    public string ReturnObjectName => string.Empty;

    /// <inheritdoc />
    public bool IsAggregate => false;

    /// <inheritdoc />
    public FunctionSignatureArgument[] Arguments { get; set; } = [];

    /// <inheritdoc />
    public bool IsSafe { get; internal set; }

    /// <inheritdoc />
    public string[] Formatters { get; }

    public PluginFunction(string name, string signature, Delegate @delegate, string[]? formatterIdentifiers = null)
    {
        Name = name;
        Signature = signature;
        Delegate = @delegate;
        Formatters = formatterIdentifiers ?? [];
    }
}

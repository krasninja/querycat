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
        Arguments = ParseArgumentsSimple(signature);
        Name = GetFunctionName(signature);
    }

    private static FunctionSignatureArgument[] ParseArgumentsSimple(string signature)
    {
        // "Naive" implementation of arguments parsing.
        var firstBracketIndex = signature.IndexOf('(');
        var lastBracketIndex = signature.LastIndexOf(')');
        if (firstBracketIndex == -1 || lastBracketIndex == -1 || lastBracketIndex <= firstBracketIndex)
        {
            return [];
        }

        var argsString = signature.Substring(firstBracketIndex + 1, lastBracketIndex - firstBracketIndex - 1);
        if (string.IsNullOrWhiteSpace(argsString))
        {
            return [];
        }

        var args = argsString.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var result = new FunctionSignatureArgument[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            var parts = args[i].Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                var type = DataType.String;
                if (Enum.TryParse(parts[1], ignoreCase: true, out DataType parsedType))
                {
                    type = parsedType;
                }
                result[i] = new FunctionSignatureArgument(parts[0], type);
            }
        }
        return result;
    }

    public static string GetFunctionName(string signature)
    {
        var firstBracketIndex = signature.IndexOf('(');
        return firstBracketIndex > -1 ? signature.Substring(0, firstBracketIndex).ToUpperInvariant() : "Unknown";
    }
}

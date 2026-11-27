using System.ComponentModel;
using System.Reflection;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Function additional information.
/// </summary>
public sealed class FunctionMetadata
{
    /// <summary>
    /// Function description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// <c>True</c> if function has no side effects.
    /// </summary>
    public bool IsSafe { get; set; }

    /// <summary>
    /// <c>True</c> if it is the function used for aggregates.
    /// </summary>
    public bool IsAggregate { get; set; }

    public string[] Formatters { get; set; } = [];

    /// <summary>
    /// Map of argument name (as in signature) to its description.
    /// </summary>
    public IReadOnlyDictionary<string, string> ArgumentsDescriptions { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Examples of the function call or its usage in SQL query.
    /// </summary>
    public string[] Examples { get; set; } = [];

    public static FunctionMetadata CreateFromAttributes(MemberInfo memberInfo)
    {
        var formatterAttribute = memberInfo.GetCustomAttribute<FunctionFormattersAttribute>();
        var metadata = new FunctionMetadata
        {
            Description = memberInfo.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
            IsSafe = memberInfo.IsDefined(typeof(SafeFunctionAttribute)),
            IsAggregate = memberInfo.IsDefined(typeof(AggregateFunctionSignatureAttribute)),
            Formatters = formatterAttribute != null ? formatterAttribute.FormatterIds : [],
            ArgumentsDescriptions = GetArgumentsDescriptions(memberInfo),
            Examples = GetExamples(memberInfo),
        };
        return metadata;
    }

    /// <summary>
    /// Get arguments descriptions from <see cref="FunctionArgumentDescriptionAttribute" /> attributes.
    /// </summary>
    /// <param name="memberInfo">Method or type.</param>
    /// <returns>Map of argument name to its description.</returns>
    public static IReadOnlyDictionary<string, string> GetArgumentsDescriptions(MemberInfo memberInfo)
    {
        var descriptions = new Dictionary<string, string>();
        foreach (var attribute in memberInfo.GetCustomAttributes<FunctionArgumentDescriptionAttribute>())
        {
            descriptions[attribute.Name] = attribute.Description;
        }
        return descriptions;
    }

    /// <summary>
    /// Get examples from <see cref="FunctionExampleAttribute" /> attributes.
    /// </summary>
    /// <param name="memberInfo">Method or type.</param>
    /// <returns>Examples.</returns>
    public static string[] GetExamples(MemberInfo memberInfo)
        => memberInfo.GetCustomAttributes<FunctionExampleAttribute>().Select(a => a.Example).ToArray();

    /// <summary>
    /// Create a copy of signature arguments with the descriptions applied.
    /// </summary>
    /// <param name="arguments">Signature arguments.</param>
    /// <param name="descriptions">Map of argument name to its description.</param>
    /// <returns>Arguments with descriptions.</returns>
    public static FunctionSignatureArgument[] ApplyArgumentsDescriptions(
        FunctionSignatureArgument[] arguments,
        IReadOnlyDictionary<string, string> descriptions)
    {
        if (descriptions.Count == 0)
        {
            return arguments;
        }

        var normalizedDescriptions = new Dictionary<string, string>(descriptions.Count);
        foreach (var description in descriptions)
        {
            normalizedDescriptions[FunctionFormatter.NormalizeName(description.Key)] = description.Value;
        }

        var result = new FunctionSignatureArgument[arguments.Length];
        for (var i = 0; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            result[i] = normalizedDescriptions.TryGetValue(FunctionFormatter.NormalizeName(argument.Name), out var description)
                ? new FunctionSignatureArgument(argument) { Description = description }
                : argument;
        }
        return result;
    }
}

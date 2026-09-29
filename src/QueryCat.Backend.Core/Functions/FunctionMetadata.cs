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
    public bool IsSafe { get; set; } = true;

    /// <summary>
    /// <c>True</c> if it is the function used for aggregates.
    /// </summary>
    public bool IsAggregate { get; set; }

    public string[] Formatters { get; set; } = [];

    public static FunctionMetadata CreateFromAttributes(MemberInfo memberInfo)
    {
        var formatterAttribute = memberInfo.GetCustomAttribute<FunctionFormattersAttribute>();
        var metadata = new FunctionMetadata
        {
            Description = memberInfo.GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
            IsSafe = memberInfo.GetCustomAttribute<SafeFunctionAttribute>() != null,
            IsAggregate = memberInfo.GetCustomAttribute<AggregateFunctionSignatureAttribute>() != null,
            Formatters = formatterAttribute != null ? formatterAttribute.FormatterIds : [],
        };
        return metadata;
    }
}

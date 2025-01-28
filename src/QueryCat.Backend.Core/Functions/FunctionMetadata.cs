using System.ComponentModel;
using System.Reflection;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Function additional information.
/// </summary>
public sealed class FunctionMetadata
{
    public string Description { get; set; } = string.Empty;

    public bool IsSafe { get; set; } = true;

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

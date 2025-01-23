using System.Globalization;
using System.Text;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Functions utils.
/// </summary>
internal static class FunctionUtils
{
    /// <summary>
    /// Format signature from functions instance.
    /// </summary>
    /// <param name="function">Instance of <see cref="IFunction" />.</param>
    /// <returns>Function signature.</returns>
    internal static string GetSignature(IFunction function)
    {
        var sb = new StringBuilder();
        sb.Append(function.Name)
            .Append('(');
        foreach (var argument in function.Arguments)
        {
            if (argument.IsVariadic)
            {
                sb.Append("...");
            }
            sb.Append(argument.Name);
            if (argument.IsOptional)
            {
                sb.Append('?');
            }
            sb.Append(": ");
            sb.Append(argument.Type);
            if (argument.HasDefaultValue && !argument.DefaultValue.IsNull)
            {
                sb.Append($" := {ValueToString(argument.DefaultValue)}");
            }
        }
        sb.Append(')');
        return sb.ToString();
    }

    private static string Quote(string target) => StringUtils.Quote(target, quote: "\'").ToString();

    internal static string ValueToString(VariantValue value) => value.Type switch
    {
        DataType.String => Quote(value.AsStringUnsafe),
        DataType.Timestamp => Quote(value.AsTimestampUnsafe.ToString(Application.Culture)) + "::timestamp",
        DataType.Interval => Quote(value.AsIntervalUnsafe.ToString("c", Application.Culture)) + "::interval",
        DataType.Object => Quote($"[object:{value.AsObjectUnsafe?.GetType().Name}]"),
        DataType.Blob => "E" + Quote(value.ToString(CultureInfo.InvariantCulture)),
        _ => value.ToString(CultureInfo.InvariantCulture),
    };
}

using System.Globalization;
using System.Reflection;
using System.Text;
using QueryCat.Backend.Core.Data;
using QueryCat.Backend.Core.Execution;
using QueryCat.Backend.Core.Types;
using QueryCat.Backend.Core.Utils;

namespace QueryCat.Backend.Core.Functions;

/// <summary>
/// Various format methods for functions maintenance (common naming, arguments formatting, etc).
/// </summary>
internal static class FunctionFormatter
{
    internal static string GetSignatureFromParameters(string name, ParameterInfo[] parameterInfos, Type outputType)
    {
        var sb = new StringBuilder();
        sb.Append(name);
        sb.Append('(');

        var parametersList = new List<string>(capacity: parameterInfos.Length);
        foreach (var parameterInfo in parameterInfos)
        {
            if (typeof(IExecutionThread).IsAssignableFrom(parameterInfo.ParameterType)
                || parameterInfo.ParameterType == typeof(CancellationToken))
            {
                continue;
            }

            var paramName = $"{ToSnakeCase(parameterInfo.Name)}: {GetTypeName(parameterInfo.ParameterType)}";
            parametersList.Add(paramName);
        }
        sb.Append(string.Join(", ", parametersList));
        sb.Append("): ");
        sb.Append(GetTypeName(outputType));

        return sb.ToString();
    }

    private static string GetTypeName(Type type)
    {
        string GetObjectTypeFromName(Type objType)
        {
            if (objType.IsAssignableTo(typeof(IRowsInput)))
            {
                return nameof(IRowsInput);
            }
            if (objType.IsAssignableTo(typeof(IRowsOutput)))
            {
                return nameof(IRowsOutput);
            }
            if (objType.IsAssignableTo(typeof(IRowsIterator)))
            {
                return nameof(IRowsIterator);
            }
            if (objType.IsAssignableTo(typeof(IRowsFormatter)))
            {
                return nameof(IRowsFormatter);
            }
            return string.Empty;
        }

        // Unwrap generic types.
        if (type.IsGenericType)
        {
            if (type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type)!;
            }
            else if (type.GetGenericTypeDefinition() == typeof(Task<>))
            {
                type = type.GetGenericArguments()[0];
            }
        }
        else if (type == typeof(Task))
        {
            type = typeof(void);
        }

        var dataType = Converter.ConvertFromSystem(type);
        if (dataType == DataType.Object)
        {
            var objectTypeName = GetObjectTypeFromName(type);
            return !string.IsNullOrEmpty(objectTypeName) ? $"{dataType}<{objectTypeName}>" : dataType.ToString();
        }
        return dataType.ToString();
    }

    internal static string ToSnakeCase(string? target = null)
    {
        if (string.IsNullOrEmpty(target))
        {
            return string.Empty;
        }

        // Based on https://stackoverflow.com/questions/63055621/how-to-convert-camel-case-to-snake-case-with-two-capitals-next-to-each-other.
        var sb = new StringBuilder(capacity: target.Length)
            .Append(char.ToLower(target[0]));
        for (var i = 1; i < target.Length; ++i)
        {
            var ch = target[i];
            if (char.IsUpper(ch))
            {
                sb.Append('_');
                sb.Append(char.ToLower(ch));
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Format signature from functions instance.
    /// </summary>
    /// <param name="function">Instance of <see cref="IFunction" />.</param>
    /// <returns>Function signature.</returns>
    internal static string GetSignature(IFunction function) => GetSignature(function, forceLowerCase: false);

    /// <summary>
    /// Format signature from functions instance.
    /// </summary>
    /// <param name="function">Instance of <see cref="IFunction" />.</param>
    /// <param name="forceLowerCase">Force lower case mode for function name and arguments.</param>
    /// <returns>Function signature.</returns>
    internal static string GetSignature(IFunction function, bool forceLowerCase)
    {
        var sb = new StringBuilder();
        sb.Append(forceLowerCase ? function.Name.ToLowerInvariant() : function.Name)
            .Append('(');
        var i = 0;
        foreach (var argument in function.Arguments)
        {
            if (argument.IsVariadic)
            {
                sb.Append("...");
            }
            sb.Append(forceLowerCase ? argument.Name.ToLowerInvariant() : argument.Name);
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

            i++;
            if (i < function.Arguments.Length)
            {
                sb.Append(", ");
            }
        }
        sb.Append(')');

        sb.Append(": ");
        sb.Append(function.ReturnType);
        if (!string.IsNullOrEmpty(function.ReturnObjectName))
        {
            sb.Append('<');
            sb.Append(function.ReturnObjectName);
            sb.Append('>');
        }
        return sb.ToString();
    }

    internal static string ValueToString(VariantValue value) => value.Type switch
    {
        DataType.String => StringUtils.Quote(value.AsStringUnsafe),
        DataType.Timestamp => StringUtils.Quote(value.AsTimestampUnsafe.ToString(Application.Culture)) + "::timestamp",
        DataType.Interval => StringUtils.Quote(value.AsIntervalUnsafe.ToString("c", Application.Culture)) + "::interval",
        DataType.Object => StringUtils.Quote($"[object:{value.AsObjectUnsafe?.GetType().Name}]"),
        DataType.Blob => "E" + StringUtils.Quote(value.ToString(CultureInfo.InvariantCulture)),
        _ => value.ToString(CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// Normalize function name. Make it uppercase.
    /// </summary>
    /// <param name="target">Target function name.</param>
    /// <returns>Normalized name.</returns>
    internal static string NormalizeName(string target)
    {
        target = StringUtils.Unquote(target);
        return target.ToUpperInvariant();
    }
}

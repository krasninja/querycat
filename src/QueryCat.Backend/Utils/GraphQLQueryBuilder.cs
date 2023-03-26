using System.Globalization;

namespace QueryCat.Backend.Utils;

/// <summary>
/// Simple Graph QL query builder.
/// </summary>
// ReSharper disable once InconsistentNaming
public sealed class GraphQLQueryBuilder
{
    private record struct Field(string Name, Func<bool> IfFunc, Action<GraphQLQueryBuilder>? SubBuilder);

    private record struct Parameter(string Name, object Value);

    private readonly string _name;
    private readonly int _level;

    private readonly List<Field> _fields = new();
    private readonly List<Parameter> _params = new();

    private bool IsRoot => _level == 0;

    public GraphQLQueryBuilder(string name)
    {
        _name = name;
    }

    private GraphQLQueryBuilder(int level, Action<GraphQLQueryBuilder> builder)
    {
        _level = level;
        _name = string.Empty;
        builder.Invoke(this);
    }

    // Source: https://github.com/charlesdevandiere/graphql-query-builder-dotnet/blob/master/src/GraphQL.Query.Builder/QueryStringBuilder.cs#L53.

    /// <summary>
    /// Formats query param.
    /// Returns:
    /// - String: `"foo"`.
    /// - Number: `10`.
    /// - Boolean: `true` or `false`.
    /// - Enum: `EnumValue`.
    /// - DateTime: `"2022-06-15T13:45:30.0000000Z"`.
    /// - Key value pair: `foo:"bar"` or `foo:10` ...
    /// - List: `["foo","bar"]` or `[1,2]` ...
    /// - Dictionary: `{foo:"bar",b:10}`.
    /// - Object: `{foo:"bar",b:10}`.
    /// </summary>
    /// <param name="stringBuilder">String builder.</param>
    /// <param name="value">Value to format.</param>
    /// <exception cref="InvalidDataException">Invalid object type in param list.</exception>
    private static void WriteQueryParam(IndentedStringBuilder stringBuilder, object value)
    {
        switch (value)
        {
            case string strValue:
                string encoded = strValue.Replace("\"", "\\\"");
                stringBuilder.Append($"\"{encoded}\"");
                break;
            case char charValue:
                stringBuilder.Append($"\"{charValue}\"");
                break;
            case byte byteValue:
                stringBuilder.Append(byteValue);
                break;
            case sbyte sbyteValue:
                stringBuilder.Append(sbyteValue);
                break;
            case short shortValue:
                stringBuilder.Append(shortValue);
                break;
            case ushort ushortValue:
                stringBuilder.Append(ushortValue);
                break;
            case int intValue:
                stringBuilder.Append(intValue);
                break;
            case uint uintValue:
                stringBuilder.Append(uintValue);
                break;
            case long longValue:
                stringBuilder.Append(longValue);
                break;
            case ulong ulongValue:
                stringBuilder.Append(ulongValue);
                break;
            case float floatValue:
                stringBuilder.Append(floatValue.ToString(CultureInfo.InvariantCulture));
                break;
            case double doubleValue:
                stringBuilder.Append(doubleValue.ToString(CultureInfo.InvariantCulture));
                break;
            case decimal decimalValue:
                stringBuilder.Append(decimalValue.ToString(CultureInfo.InvariantCulture));
                break;
            case bool booleanValue:
                stringBuilder.Append(booleanValue ? "true" : "false");
                break;
            case Enum enumValue:
                stringBuilder.Append(enumValue.ToString());
                break;
            case DateTime dateTimeValue:
                stringBuilder.Append(dateTimeValue.ToString("o"));
                break;
            case KeyValuePair<string, object> kvValue:
                stringBuilder.Append($"{kvValue.Key}");
                stringBuilder.Append(':');
                WriteQueryParam(stringBuilder, kvValue.Value);
                break;
            default:
                throw new InvalidDataException($"Invalid object type in params list: {value.GetType()}.");
        }
    }

    public GraphQLQueryBuilder AddField(string fieldName)
    {
        _fields.Add(new Field(fieldName, () => true, null));
        return this;
    }

    public GraphQLQueryBuilder AddFieldIf(Func<bool> func, string fieldName)
    {
        _fields.Add(new Field(fieldName, func, null));
        return this;
    }

    public GraphQLQueryBuilder AddField(string fieldName, Action<GraphQLQueryBuilder> subField)
    {
        _fields.Add(new Field(fieldName, () => true, subField));
        return this;
    }

    public GraphQLQueryBuilder AddFieldIf(string fieldName, Func<bool> func, Action<GraphQLQueryBuilder> subField)
    {
        _fields.Add(new Field(fieldName, func, subField));
        return this;
    }

    public GraphQLQueryBuilder AddParam(string paramName, object value)
    {
        _params.Add(new Parameter(paramName, value));
        return this;
    }

    public IndentedStringBuilder Build()
    {
        var stringBuilder = new IndentedStringBuilder(skipFirstLineIndent: !IsRoot);
        stringBuilder.IncreaseIndent(_level);
        Build(stringBuilder);
        return stringBuilder;
    }

    private void Build(IndentedStringBuilder stringBuilder)
    {
        if (IsRoot)
        {
            stringBuilder.AppendLine("query {").IncreaseIndent();
        }

        stringBuilder.Append(_name).IncreaseIndent(); // Field names.

        // Params.
        if (_params.Any())
        {
            stringBuilder.Append("(");
            for (var i = 0; i < _params.Count; i++)
            {
                if (i != 0)
                {
                    stringBuilder.Append(", ");
                }
                stringBuilder.Append(_params[i].Name);
                stringBuilder.Append(':');
                WriteQueryParam(stringBuilder, _params[i].Value);
            }
            stringBuilder.Append(")");
        }
        stringBuilder.AppendLine(" {");

        // Fields.
        foreach (var field in _fields)
        {
            if (!field.IfFunc.Invoke())
            {
                continue;
            }
            stringBuilder.Append(field.Name);
            if (field.SubBuilder != null)
            {
                var subBuilder = new GraphQLQueryBuilder(_level + 1, field.SubBuilder);
                stringBuilder.Append(subBuilder.Build());
            }
            else
            {
                stringBuilder.AppendLine();
            }
        }

        stringBuilder.DecreaseIndent().AppendLine("}"); // Field names.

        if (IsRoot)
        {
            stringBuilder.DecreaseIndent().AppendLine("}"); // query.
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        var stringBuilder = new IndentedStringBuilder();
        Build(stringBuilder);
        return stringBuilder.ToString();
    }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace QueryCat.Backend.Core.Types;

public sealed class VariantValueJsonConverter : JsonConverter<VariantValue>
{
    /// <inheritdoc />
    public override VariantValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.None || reader.TokenType == JsonTokenType.Null)
        {
            return VariantValue.Null;
        }
        if (reader.TokenType == JsonTokenType.String)
        {
            return DataTypeUtils.DeserializeVariantValue(reader.GetString(), strongDeserialization: false);
        }
        if (reader.TokenType == JsonTokenType.Number)
        {
            return new VariantValue(reader.GetDecimal());
        }
        if (reader.TokenType == JsonTokenType.True)
        {
            return VariantValue.TrueValue;
        }
        if (reader.TokenType == JsonTokenType.False)
        {
            return VariantValue.FalseValue;
        }
        throw new JsonException();
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, VariantValue value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(DataTypeUtils.SerializeVariantValue(value));
    }
}

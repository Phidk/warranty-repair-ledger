using System.Text.Json;
using System.Text.Json.Serialization;

namespace WarrantyRepairLedger.Serialization;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected date string.");
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Date string was empty.");
        }

        if (DateOnly.TryParse(value, out var result))
        {
            return result;
        }

        throw new JsonException($"Unable to parse '{value}' to DateOnly.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString(Format));
}

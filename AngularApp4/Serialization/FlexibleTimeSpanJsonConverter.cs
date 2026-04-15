using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AngularApp4.Serialization;

public sealed class FlexibleTimeSpanJsonConverter : JsonConverter<TimeSpan>
{
    private static readonly string[] Formats =
    {
        @"hh\:mm",
        @"h\:mm",
        @"hh\:mm\:ss",
        @"h\:mm\:ss",
        @"c"
    };

    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            if (TryParse(raw, out var value))
            {
                return value;
            }
        }

        throw new JsonException("Time value must be in HH:mm or HH:mm:ss format.");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture));
    }

    internal static bool TryParse(string? raw, out TimeSpan value)
    {
        if (!string.IsNullOrWhiteSpace(raw))
        {
            var normalized = raw.Trim();
            if (TimeSpan.TryParseExact(normalized, Formats, CultureInfo.InvariantCulture, out value) ||
                TimeSpan.TryParse(normalized, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }
}

public sealed class NullableFlexibleTimeSpanJsonConverter : JsonConverter<TimeSpan?>
{
    public override TimeSpan? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var raw = reader.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            if (FlexibleTimeSpanJsonConverter.TryParse(raw, out var value))
            {
                return value;
            }
        }

        throw new JsonException("Time value must be in HH:mm or HH:mm:ss format.");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture));
            return;
        }

        writer.WriteNullValue();
    }
}

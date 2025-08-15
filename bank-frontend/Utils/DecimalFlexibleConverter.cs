using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Staj_Proje_1.Utils // veya başka uygun namespace
{
    public sealed class DecimalFlexibleConverter : JsonConverter<decimal>
    {
        public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetDecimal(out var n)) return n;
                throw new JsonException("Invalid decimal number.");
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString() ?? "";
                s = s.Trim();
                var filtered = new string(s.Where(ch => char.IsDigit(ch) || ch is '-' or '.' or ',').ToArray());

                if (string.IsNullOrWhiteSpace(filtered))
                    return 0m;

                // TR formatlarını normalize et: "1.234,56" -> "1234.56"
                if (filtered.Contains(',') && filtered.LastIndexOf(',') > filtered.LastIndexOf('.'))
                {
                    filtered = filtered.Replace(".", "").Replace(',', '.');
                }

                if (decimal.TryParse(filtered, NumberStyles.Number | NumberStyles.AllowLeadingSign,
                                     CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
                throw new JsonException($"Invalid decimal string: '{s}'");
            }

            throw new JsonException($"Token type {reader.TokenType} not supported for decimal.");
        }

        public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value);
    }
}

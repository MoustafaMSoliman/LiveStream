using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LiveStream.APPLICATION.DTOs
{
    public class SourceConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString() ?? string.Empty;
            }
            else if (reader.TokenType == JsonTokenType.StartObject)
            {
               
                using var document = JsonDocument.ParseValue(ref reader);
                if (document.RootElement.TryGetProperty("source", out var sourceElement) &&
                    sourceElement.ValueKind == JsonValueKind.String)
                {
                    return sourceElement.GetString() ?? string.Empty;
                }
                else if (document.RootElement.TryGetProperty("url", out var urlElement) &&
                         urlElement.ValueKind == JsonValueKind.String)
                {
                    return urlElement.GetString() ?? string.Empty;
                }
                else
                {
                    
                    return document.RootElement.GetRawText();
                }
            }
            else
            {
                
                return reader.GetString() ?? string.Empty;
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}

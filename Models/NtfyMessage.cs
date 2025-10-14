using System.Text.Json;
using System.Text.Json.Serialization;

namespace FixWidget.Models;

public class NtfyMessage
{
    [JsonPropertyName("id")]
    public object? IdRaw { get; set; }

    [JsonIgnore]
    public int Id
    {
        get
        {
            if (IdRaw == null) return 0;
            
            if (IdRaw is int intValue)
                return intValue;
            
            if (IdRaw is long longValue)
                return (int)longValue;
            
            if (IdRaw is string strValue && int.TryParse(strValue, out int parsedValue))
                return parsedValue;
            
            // Проверяем JsonElement
            if (IdRaw is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number && jsonElement.TryGetInt32(out int numValue))
                    return numValue;
                
                if (jsonElement.ValueKind == JsonValueKind.String && int.TryParse(jsonElement.GetString(), out int strParsed))
                    return strParsed;
            }
            
            return 0;
        }
    }

    [JsonPropertyName("steamid64")]
    public string? SteamId64 { get; set; }

    [JsonPropertyName("login")]
    public string? Login { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("createdAt")]
    public long? CreatedAt { get; set; }

    [JsonPropertyName("extra")]
    public WidgetExtra? Extra { get; set; }

    // Метод для парсинга вложенного JSON из поля message
    public NtfyMessage? ParseNestedMessage()
    {
        if (string.IsNullOrWhiteSpace(Message))
            return null;

        try
        {
            return JsonSerializer.Deserialize<NtfyMessage>(Message);
        }
        catch
        {
            // Игнорируем ошибки парсинга вложенного JSON
            return null;
        }
    }
}

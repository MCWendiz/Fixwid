using System.Text.Json.Serialization;

namespace FixWidget.Models;

public class WidgetExtra
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    // Reference resolution for position scaling
    [JsonPropertyName("setupResolutionX")]
    public object? SetupResolutionXRaw { get; set; }

    [JsonIgnore]
    public int SetupResolutionX => ParseInt(SetupResolutionXRaw, 2560);

    [JsonPropertyName("setupResolutionY")]
    public object? SetupResolutionYRaw { get; set; }

    [JsonIgnore]
    public int SetupResolutionY => ParseInt(SetupResolutionYRaw, 1440);

    [JsonPropertyName("setupX")]
    public object? SetupXRaw { get; set; }

    [JsonIgnore]
    public int SetupX => ParseInt(SetupXRaw, 0);

    [JsonPropertyName("setupY")]
    public object? SetupYRaw { get; set; }

    [JsonIgnore]
    public int SetupY => ParseInt(SetupYRaw, 0);

    [JsonPropertyName("x")]
    public object? XRaw { get; set; }

    [JsonIgnore]
    public int X => ParseInt(XRaw, 0);

    [JsonPropertyName("y")]
    public object? YRaw { get; set; }

    [JsonIgnore]
    public int Y => ParseInt(YRaw, 0);

    [JsonPropertyName("width")]
    public object? WidthRaw { get; set; }

    [JsonIgnore]
    public int Width => ParseInt(WidthRaw, 800);

    [JsonPropertyName("height")]
    public object? HeightRaw { get; set; }

    [JsonIgnore]
    public int Height => ParseInt(HeightRaw, 600);

    [JsonPropertyName("opacity")]
    public object? OpacityRaw { get; set; }

    [JsonIgnore]
    public double Opacity
    {
        get
        {
            var value = ParseDouble(OpacityRaw, 100.0);
            // Если значение от 0 до 1, преобразуем в проценты
            if (value <= 1.0)
                return value * 100.0;
            return value;
        }
    }

    [JsonPropertyName("scale")]
    public object? ScaleRaw { get; set; }

    [JsonIgnore]
    public double Scale
    {
        get
        {
            var result = ParseDouble(ScaleRaw, 1.0);
            return result;
        }
    }

    // Масштаб КОНТЕНТА внутри окна (CSS zoom/transform)
    // Если не задан - используется значение из scale
    [JsonPropertyName("contentZoom")]
    public object? ContentZoomRaw { get; set; }

    [JsonIgnore]
    public double ContentZoom
    {
        get
        {
            // Если contentZoom не задан (равен 1.0 по умолчанию и не был передан),
            // используем значение из scale
            var contentZoomValue = ParseDouble(ContentZoomRaw, 0.0);
            
            // Если contentZoom явно не указан (0.0), используем scale
            if (contentZoomValue == 0.0 && ContentZoomRaw == null)
            {
                return Scale;
            }
            
            // Если contentZoom = 0, всё равно используем scale
            if (contentZoomValue == 0.0)
            {
                return Scale;
            }
            
            return contentZoomValue;
        }
    }

    [JsonPropertyName("durationMs")]
    public object? DurationMsRaw { get; set; }

    [JsonIgnore]
    public int DurationMs => ParseInt(DurationMsRaw, 0);

    [JsonPropertyName("clickThrough")]
    public object? ClickThroughRaw { get; set; }

    [JsonIgnore]
    public bool ClickThrough => ParseBool(ClickThroughRaw, false);

    [JsonPropertyName("volume")]
    public object? VolumeRaw { get; set; }

    [JsonIgnore]
    public double Volume => ParseDouble(VolumeRaw, 0.5);

    // Вспомогательные методы парсинга
    private static int ParseInt(object? value, int defaultValue)
    {
        if (value == null) return defaultValue;

        // Обрабатываем JsonElement (приходит из System.Text.Json)
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            try
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    // Пробуем как int, если не получается - как double и округляем
                    if (jsonElement.TryGetInt32(out int intValue))
                        return intValue;
                    else if (jsonElement.TryGetDouble(out double doubleValue))
                        return (int)doubleValue;
                }
                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var str = jsonElement.GetString();
                    if (int.TryParse(str, out int result))
                        return result;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ ParseInt error: {ex.Message}");
            }
            return defaultValue;
        }

        return value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            double doubleValue => (int)doubleValue,
            string strValue when int.TryParse(strValue, out int result) => result,
            _ => defaultValue
        };
    }

    private static double ParseDouble(object? value, double defaultValue)
    {
        if (value == null) return defaultValue;

        // Обрабатываем JsonElement (приходит из System.Text.Json)
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            try
            {
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    return jsonElement.GetDouble();
                }
                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    var str = jsonElement.GetString();
                    if (double.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result))
                        return result;
                }
            }
            catch
            {
                // Игнорируем ошибки парсинга
            }
            return defaultValue;
        }

        return value switch
        {
            double doubleValue => doubleValue,
            float floatValue => floatValue,
            int intValue => intValue,
            long longValue => longValue,
            string strValue when double.TryParse(strValue, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double result) => result,
            _ => defaultValue
        };
    }

    private static bool ParseBool(object? value, bool defaultValue)
    {
        if (value == null) return defaultValue;

        // Обрабатываем JsonElement (приходит из System.Text.Json)
        if (value is System.Text.Json.JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.True)
                return true;
            else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.False)
                return false;
            else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var str = jsonElement.GetString();
                if (bool.TryParse(str, out bool result))
                    return result;
                if (str == "1") return true;
                if (str == "0") return false;
            }
            else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                var num = jsonElement.GetInt32();
                if (num == 1) return true;
                if (num == 0) return false;
            }
            return defaultValue;
        }

        return value switch
        {
            bool boolValue => boolValue,
            string strValue when bool.TryParse(strValue, out bool result) => result,
            string strValue when strValue == "1" => true,
            string strValue when strValue == "0" => false,
            int intValue when intValue == 1 => true,
            int intValue when intValue == 0 => false,
            _ => defaultValue
        };
    }
}

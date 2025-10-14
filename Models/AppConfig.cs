using System.Text.Json.Serialization;

namespace FixWidget.Models;

public class AppConfig
{
    [JsonPropertyName("NtfyUrl")]
    public string NtfyUrl { get; set; } = "https://ntfy.sh/asfdasd7f9987890231hkj";
}

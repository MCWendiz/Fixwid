using System.Text.Json.Serialization;

namespace FixWidget.Models;

/// <summary>
/// ntfy server message envelope wrapper.
/// Actual widget data is in the Message field as a JSON string.
/// </summary>
public class NtfyEnvelope
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("expires")]
    public long? Expires { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    /// <summary>
    /// Message field contains nested JSON as a string
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

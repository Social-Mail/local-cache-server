using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace LocalCache;

public class CacheMessage
{
    [JsonPropertyName("id")]
    public string? ID { get;set; }

    [JsonPropertyName("cmd")]
    public string? Command { get; set; }

    [JsonPropertyName("key")]
    public string? Key { get; set; }

    [JsonPropertyName("value")]
    public JsonValue? Value { get; set; }

    [JsonPropertyName("maxAge")]
    public long? MaxAge { get; set; }

    [JsonPropertyName("ttl")]
    public long? TTL { get; set; }

}

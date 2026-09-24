using System.Text.Json.Serialization;

namespace SpeedTest.Core;

/// <summary>Shape of GET /meta. Unknown fields are ignored; every field is optional.</summary>
internal sealed class CloudflareMeta
{
    [JsonPropertyName("clientIp")]
    public string? ClientIp { get; set; }

    [JsonPropertyName("asOrganization")]
    public string? AsOrganization { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("colo")]
    public string? Colo { get; set; }

    public ConnectionInfo ToConnectionInfo() => new(AsOrganization, ClientIp, City, Country, Colo);
}

/// <summary>Source-generated serializer context so the extension stays trim- and AOT-safe.</summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CloudflareMeta))]
internal sealed partial class CloudflareJsonContext : JsonSerializerContext
{
}

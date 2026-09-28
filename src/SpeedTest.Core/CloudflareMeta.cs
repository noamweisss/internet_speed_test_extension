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

    /// <summary>The serving data centre. An object, not a string (observed 2026-09-28; a string here broke every parse).</summary>
    [JsonPropertyName("colo")]
    public CloudflareColo? Colo { get; set; }

    public ConnectionInfo ToConnectionInfo() => new(AsOrganization, ClientIp, City, Country, Colo?.Iata);
}

/// <summary>The <c>colo</c> object inside /meta. Only the airport code is shown; the coordinates are ignored.</summary>
internal sealed class CloudflareColo
{
    [JsonPropertyName("iata")]
    public string? Iata { get; set; }
}

/// <summary>Source-generated serializer context so the extension stays trim- and AOT-safe.</summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(CloudflareMeta))]
[JsonSerializable(typeof(CloudflareColo))]
internal sealed partial class CloudflareJsonContext : JsonSerializerContext
{
}

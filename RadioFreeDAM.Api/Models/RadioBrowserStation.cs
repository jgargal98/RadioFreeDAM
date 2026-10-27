using System.Text.Json.Serialization;

namespace RadioFreeDAM.Api.Models;

/// <summary>
/// Emisora tal y como la devuelve Radio Browser (solo los campos que usamos).
/// Es un formato externo: nunca sale de la API, se convierte antes a StationDto.
/// </summary>
public class RadioBrowserStation
{
    [JsonPropertyName("stationuuid")]
    public string StationUuid { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>URL final del stream, ya resuelta (sin listas .pls/.m3u intermedias).</summary>
    [JsonPropertyName("url_resolved")]
    public string UrlResolved { get; set; } = string.Empty;

    [JsonPropertyName("favicon")]
    public string Favicon { get; set; } = string.Empty;

    [JsonPropertyName("country")]
    public string Country { get; set; } = string.Empty;

    [JsonPropertyName("tags")]
    public string Tags { get; set; } = string.Empty;

    [JsonPropertyName("codec")]
    public string Codec { get; set; } = string.Empty;

    [JsonPropertyName("bitrate")]
    public int Bitrate { get; set; }
}

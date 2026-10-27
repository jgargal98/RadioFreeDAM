namespace RadioFreeDAM.Shared.Dtos;

/// <summary>
/// Emisora tal y como la API se la entrega a la app.
/// Es nuestro propio formato: la API adapta los datos de Radio Browser a esta clase.
/// </summary>
public class StationDto
{
    /// <summary>Identificador único de la emisora (el "stationuuid" de Radio Browser).</summary>
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Dirección del streaming de audio que reproducirá la app.</summary>
    public string StreamUrl { get; set; } = string.Empty;

    /// <summary>URL del logo de la emisora. Puede venir vacía.</summary>
    public string? FaviconUrl { get; set; }

    public string Country { get; set; } = string.Empty;

    /// <summary>Géneros o etiquetas separados por comas (por ejemplo "pop,rock").</summary>
    public string Tags { get; set; } = string.Empty;

    /// <summary>Formato del audio (MP3, AAC...). Puede venir vacío.</summary>
    public string Codec { get; set; } = string.Empty;

    /// <summary>Calidad en kbps. 0 si no se conoce.</summary>
    public int Bitrate { get; set; }
}

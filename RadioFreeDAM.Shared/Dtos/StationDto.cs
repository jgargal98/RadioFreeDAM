namespace RadioFreeDAM.Shared.Dtos;

/// <summary>
/// Emisora tal y como la API se la entrega a la app.
/// Es nuestro propio formato: en la Fase 2 la API adaptará los datos de Radio Browser a esta clase.
/// </summary>
public class StationDto
{
    /// <summary>Identificador único de la emisora (en Radio Browser será el "stationuuid").</summary>
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Dirección del streaming de audio que reproducirá la app.</summary>
    public string StreamUrl { get; set; } = string.Empty;

    /// <summary>URL del logo de la emisora. Puede venir vacía.</summary>
    public string? FaviconUrl { get; set; }

    public string Country { get; set; } = string.Empty;

    /// <summary>Géneros o etiquetas separados por comas (por ejemplo "pop,rock").</summary>
    public string Tags { get; set; } = string.Empty;
}

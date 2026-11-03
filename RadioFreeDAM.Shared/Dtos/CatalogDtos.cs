namespace RadioFreeDAM.Shared.Dtos;

/// <summary>Una cadena de radio con varias emisoras (por ejemplo COPE con sus desconexiones locales).</summary>
public class NetworkDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Logo de la emisora más popular de la cadena. Puede venir vacío.</summary>
    public string? FaviconUrl { get; set; }
}

/// <summary>
/// Una fila del catálogo agrupado: o una emisora suelta, o una cadena que agrupa varias.
/// Siempre viene relleno uno de los dos.
/// </summary>
public class CatalogEntryDto
{
    public StationDto? Station { get; set; }

    public NetworkDto? Network { get; set; }
}

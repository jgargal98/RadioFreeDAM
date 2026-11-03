namespace RadioFreeDAM.Services;

/// <summary>
/// Pide el catálogo de emisoras a nuestra API.
/// La app no sabe de dónde saca la API las emisoras (Radio Browser o datos de prueba).
/// </summary>
public interface IStationApiService
{
    /// <summary>Emisoras populares (las que muestra Inicio), con las cadenas agrupadas en una fila.</summary>
    /// <exception cref="HttpRequestException">
    /// Si la API no responde (StatusCode null) o devuelve un error (por ejemplo 502 si Radio Browser falla).
    /// </exception>
    Task<IReadOnlyList<CatalogEntryDto>> GetTopStationsAsync();

    /// <summary>Emisoras cuyo nombre contiene <paramref name="name"/>, con las cadenas agrupadas.</summary>
    /// <exception cref="HttpRequestException">Igual que <see cref="GetTopStationsAsync"/>.</exception>
    Task<IReadOnlyList<CatalogEntryDto>> SearchAsync(string name);

    /// <summary>Todas las emisoras de una cadena (por ejemplo, las COPE de cada ciudad).</summary>
    /// <exception cref="HttpRequestException">Igual que <see cref="GetTopStationsAsync"/>.</exception>
    Task<IReadOnlyList<StationDto>> GetNetworkStationsAsync(string networkId);
}

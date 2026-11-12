namespace RadioFreeDAM.Services;

/// <summary>
/// Pide el catálogo de emisoras a nuestra API.
/// La app no sabe de dónde saca la API las emisoras (Radio Browser o datos de prueba).
/// </summary>
public interface IStationApiService
{
    /// <summary>
    /// Catalog of the home list for the given filters, with the networks (COPE, SER...) grouped in one row.
    /// </summary>
    /// <exception cref="HttpRequestException">
    /// Si la API no responde (StatusCode null) o devuelve un error (por ejemplo 502 si Radio Browser falla).
    /// </exception>
    Task<IReadOnlyList<CatalogEntryDto>> GetCatalogAsync(CatalogFilter filter);

    /// <summary>Todas las emisoras de una cadena (por ejemplo, las COPE de cada ciudad).</summary>
    /// <exception cref="HttpRequestException">Igual que <see cref="GetCatalogAsync"/>.</exception>
    Task<IReadOnlyList<StationDto>> GetNetworkStationsAsync(string networkId);
}

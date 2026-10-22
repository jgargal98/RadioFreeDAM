namespace RadioFreeDAM.Services;

/// <summary>
/// Pide el catálogo de emisoras a nuestra API.
/// La app no sabe si la API usa datos de prueba (Fase 1) o Radio Browser (Fase 2).
/// </summary>
public interface IStationApiService
{
    /// <exception cref="HttpRequestException">Si la API no responde o devuelve un error.</exception>
    Task<IReadOnlyList<StationDto>> GetStationsAsync();
}

namespace RadioFreeDAM.Services;

/// <summary>
/// Pide el catálogo de emisoras a nuestra API.
/// La app no sabe de dónde saca la API las emisoras (Radio Browser o datos de prueba).
/// </summary>
public interface IStationApiService
{
    /// <summary>Emisoras populares (las que muestra Inicio al abrir).</summary>
    /// <exception cref="HttpRequestException">
    /// Si la API no responde (StatusCode null) o devuelve un error (por ejemplo 502 si Radio Browser falla).
    /// </exception>
    Task<IReadOnlyList<StationDto>> GetTopStationsAsync();

    /// <summary>Emisoras cuyo nombre contiene <paramref name="name"/>.</summary>
    /// <exception cref="HttpRequestException">Igual que <see cref="GetTopStationsAsync"/>.</exception>
    Task<IReadOnlyList<StationDto>> SearchAsync(string name);
}

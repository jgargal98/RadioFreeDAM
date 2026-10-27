using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Obtiene el catálogo de emisoras. El controlador solo conoce esta interfaz,
/// así que da igual si los datos son de prueba o vienen de Radio Browser.
/// </summary>
public interface IStationService
{
    /// <summary>Emisoras más populares del país configurado.</summary>
    /// <exception cref="StationCatalogUnavailableException">Si el catálogo externo no responde.</exception>
    Task<IReadOnlyList<StationDto>> GetTopStationsAsync(CancellationToken cancellationToken = default);

    /// <summary>Busca emisoras de cualquier país cuyo nombre contenga <paramref name="name"/>.</summary>
    /// <exception cref="StationCatalogUnavailableException">Si el catálogo externo no responde.</exception>
    Task<IReadOnlyList<StationDto>> SearchByNameAsync(string name, CancellationToken cancellationToken = default);
}

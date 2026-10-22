using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Obtiene el catálogo de emisoras. El controlador solo conoce esta interfaz,
/// así que cambiar de datos de prueba a Radio Browser (Fase 2) no le afecta.
/// </summary>
public interface IStationService
{
    Task<IReadOnlyList<StationDto>> GetStationsAsync();
}

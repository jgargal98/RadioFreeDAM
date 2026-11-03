using Microsoft.AspNetCore.Mvc;
using RadioFreeDAM.Api.Services;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StationsController(IStationService stationService) : ControllerBase
{
    private const int MinSearchLength = 2;
    private const int MaxSearchLength = 100;

    // GET api/stations            → emisoras populares
    // GET api/stations?name=jazz  → búsqueda por nombre
    // Si Radio Browser falla, ApiExceptionHandler responde 502.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StationDto>>> GetStations(
        [FromQuery] string? name,
        CancellationToken cancellationToken)
    {
        var search = name?.Trim();

        if (string.IsNullOrEmpty(search))
            return Ok(await stationService.GetTopStationsAsync(cancellationToken));

        if (search.Length < MinSearchLength || search.Length > MaxSearchLength)
            throw new ValidationFailedException($"La búsqueda debe tener entre {MinSearchLength} y {MaxSearchLength} caracteres.");

        return Ok(await stationService.SearchByNameAsync(search, cancellationToken));
    }
}

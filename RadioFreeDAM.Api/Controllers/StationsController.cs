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
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StationDto>>> GetStations(
        [FromQuery] string? name,
        CancellationToken cancellationToken)
    {
        var search = name?.Trim();

        if (!string.IsNullOrEmpty(search) && (search.Length < MinSearchLength || search.Length > MaxSearchLength))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: $"La búsqueda debe tener entre {MinSearchLength} y {MaxSearchLength} caracteres.");
        }

        try
        {
            var stations = string.IsNullOrEmpty(search)
                ? await stationService.GetTopStationsAsync(cancellationToken)
                : await stationService.SearchByNameAsync(search, cancellationToken);

            return Ok(stations);
        }
        catch (StationCatalogUnavailableException ex)
        {
            // 502: nuestra API funciona, pero el servicio externo del que depende no.
            return Problem(statusCode: StatusCodes.Status502BadGateway, title: ex.Message);
        }
    }
}

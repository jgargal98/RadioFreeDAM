using Microsoft.AspNetCore.Mvc;
using RadioFreeDAM.Api.Services;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StationsController(IStationService stationService) : ControllerBase
{
    // GET api/stations
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<StationDto>>> GetStations()
    {
        var stations = await stationService.GetStationsAsync();
        return Ok(stations);
    }
}

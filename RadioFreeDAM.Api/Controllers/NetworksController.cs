using Microsoft.AspNetCore.Mvc;
using RadioFreeDAM.Api.Services;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Controllers;

/// <summary>Emisoras de una cadena (por ejemplo, todas las COPE), para elegir la ciudad.</summary>
[ApiController]
[Route("api/networks")]
public class NetworksController(CatalogService catalogService) : ControllerBase
{
    // GET api/networks/cope/stations  → todas las COPE de España; 404 si la cadena no existe
    [HttpGet("{id}/stations")]
    public async Task<ActionResult<IReadOnlyList<StationDto>>> GetStations(string id, CancellationToken cancellationToken) =>
        Ok(await catalogService.GetNetworkStationsAsync(id, cancellationToken));
}

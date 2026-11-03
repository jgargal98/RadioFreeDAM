using Microsoft.AspNetCore.Mvc;
using RadioFreeDAM.Api.Services;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Controllers;

/// <summary>
/// Catálogo agrupado por cadenas, el que usa la app. (api/stations sigue dando la lista sin agrupar.)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CatalogController(CatalogService catalogService) : ControllerBase
{
    private const int MinSearchLength = 2;
    private const int MaxSearchLength = 100;

    // GET api/catalog            → populares, con las cadenas agrupadas
    // GET api/catalog?name=cope  → búsqueda, con las cadenas agrupadas
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CatalogEntryDto>>> Get(
        [FromQuery] string? name,
        CancellationToken cancellationToken)
    {
        var search = name?.Trim();

        if (string.IsNullOrEmpty(search))
            return Ok(await catalogService.GetTopAsync(cancellationToken));

        if (search.Length < MinSearchLength || search.Length > MaxSearchLength)
            throw new ValidationFailedException($"La búsqueda debe tener entre {MinSearchLength} y {MaxSearchLength} caracteres.");

        return Ok(await catalogService.SearchAsync(search, cancellationToken));
    }
}

using Microsoft.AspNetCore.Mvc;
using RadioFreeDAM.Api.Services;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Controllers;

/// <summary>
/// Grouped and filtered catalog, the one the app uses. (api/stations still gives the plain, ungrouped list.)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CatalogController(CatalogService catalogService) : ControllerBase
{
    private const int MinSearchLength = 2;
    private const int MaxSearchLength = 100;

    // GET api/catalog                                  -> Spain top, most listened
    // GET api/catalog?scope=world&category=music       -> world top of music
    // GET api/catalog?sort=name&name=cope              -> search sorted A-Z
    // Unknown filter ids answer 400 (see CatalogService).
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CatalogEntryDto>>> Get(
        [FromQuery] string? name,
        [FromQuery] string scope = CatalogFilters.ScopeSpain,
        [FromQuery] string category = CatalogFilters.AllCategories,
        [FromQuery] string sort = CatalogFilters.SortClicks,
        CancellationToken cancellationToken = default)
    {
        var search = name?.Trim();

        if (!string.IsNullOrEmpty(search) && (search.Length < MinSearchLength || search.Length > MaxSearchLength))
            throw new ValidationFailedException($"La búsqueda debe tener entre {MinSearchLength} y {MaxSearchLength} caracteres.");

        var request = new CatalogRequest(string.IsNullOrEmpty(search) ? null : search, scope, category, sort);
        return Ok(await catalogService.GetAsync(request, cancellationToken));
    }
}

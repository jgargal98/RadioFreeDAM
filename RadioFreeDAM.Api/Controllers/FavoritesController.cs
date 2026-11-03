using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadioFreeDAM.Api.Services;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Controllers;

/// <summary>
/// Favoritas del usuario que ha iniciado sesión. Sin token válido, todas devuelven 401.
/// El usuario sale del token, nunca de la URL: nadie puede tocar las favoritas de otro.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public class FavoritesController(FavoriteService favoriteService) : ControllerBase
{
    // GET api/favorites
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FavoriteDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await favoriteService.GetAllAsync(CurrentUserId, cancellationToken));

    // POST api/favorites  { "stationId": "..." }  → 201, 404 si la emisora no existe, 409 si ya es favorita
    [HttpPost]
    public async Task<ActionResult<FavoriteDto>> Add(AddFavoriteRequest request, CancellationToken cancellationToken)
    {
        var favorite = await favoriteService.AddAsync(CurrentUserId, request.StationId.Trim(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, favorite);
    }

    // DELETE api/favorites/{stationId}  → 204, 404 si no estaba
    [HttpDelete("{stationId}")]
    public async Task<IActionResult> Remove(string stationId, CancellationToken cancellationToken)
    {
        await favoriteService.RemoveAsync(CurrentUserId, stationId, cancellationToken);
        return NoContent();
    }

    /// <summary>Id del usuario, leído del token (claim "sub").</summary>
    private int CurrentUserId => int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
}

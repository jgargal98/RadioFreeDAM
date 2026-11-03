using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RadioFreeDAM.Api.Services;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{
    // POST api/auth/register  → 201 con el token, 400 si los datos no son válidos, 409 si el correo ya existe
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    // POST api/auth/login  → 200 con el token, 401 si el correo o la contraseña no son correctos
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken) =>
        Ok(await authService.LoginAsync(request, cancellationToken));

    // GET api/auth/me  → 200 con el usuario del token, 401 si el token no vale o el usuario ya no existe
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirst(JwtRegisteredClaimNames.Sub)!.Value);
        return Ok(await authService.GetCurrentUserAsync(userId, cancellationToken));
    }
}

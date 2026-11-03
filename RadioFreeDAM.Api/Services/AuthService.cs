using Microsoft.AspNetCore.Identity;
using RadioFreeDAM.Api.Models;
using RadioFreeDAM.Api.Repositories;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Registro e inicio de sesión. Aplica las reglas (correo único, contraseña con hash)
/// y delega el acceso a datos en el repositorio.
/// </summary>
public class AuthService(
    IUserRepository users,
    IPasswordHasher<User> passwordHasher,
    TokenService tokenService,
    TimeProvider timeProvider)
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);

        // Regla del PDF: el correo es único. Además hay un índice único en la tabla por si dos peticiones llegan a la vez.
        if (await users.EmailExistsAsync(email, cancellationToken))
            throw new ConflictAppException("Ya existe una cuenta con ese correo.");

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = email,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        // PBKDF2 con sal aleatoria: dos usuarios con la misma contraseña tienen hashes distintos.
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        try
        {
            await users.AddAsync(user, cancellationToken);
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException)
        {
            // Dos registros con el mismo correo a la vez: el índice único ha rechazado el segundo.
            throw new ConflictAppException("Ya existe una cuenta con ese correo.");
        }

        return CreateResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await users.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);

        // Mismo mensaje si el correo no existe o la contraseña falla: así no se revela qué correos están registrados.
        if (user is null ||
            passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAppException("Correo o contraseña incorrectos.");
        }

        return CreateResponse(user);
    }

    private AuthResponse CreateResponse(User user)
    {
        var (token, expiresAt) = tokenService.CreateToken(user);
        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            User = new UserDto { Id = user.Id, Name = user.Name, Email = user.Email }
        };
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}

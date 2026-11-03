using System.IdentityModel.Tokens.Jwt;
using Microsoft.EntityFrameworkCore;
using RadioFreeDAM.Api.Services;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task Register_GuardaElUsuarioConHash_NoLaContraseña()
    {
        using var db = new TestDatabase();

        var response = await db.CreateAuthService().RegisterAsync(TestData.Register("ana@test.com", "Contraseña123"));

        var saved = await db.Context.Users.SingleAsync();
        Assert.Equal("ana@test.com", saved.Email);
        Assert.NotEqual("Contraseña123", saved.PasswordHash);
        Assert.DoesNotContain("Contraseña123", saved.PasswordHash);
        Assert.Equal(saved.Id, response.User.Id);
        Assert.False(string.IsNullOrEmpty(response.Token));
    }

    [Fact]
    public async Task Register_CorreoRepetido_AunqueCambienMayusculas_Da409()
    {
        using var db = new TestDatabase();
        var auth = db.CreateAuthService();
        await auth.RegisterAsync(TestData.Register("ana@test.com"));

        var error = await Assert.ThrowsAsync<ConflictAppException>(
            () => auth.RegisterAsync(TestData.Register("  ANA@Test.com ")));

        Assert.Equal(409, error.StatusCode);
        Assert.Equal(1, await db.Context.Users.CountAsync());
    }

    [Fact]
    public async Task Login_ContraseñaIncorrecta_Da401()
    {
        using var db = new TestDatabase();
        var auth = db.CreateAuthService();
        await auth.RegisterAsync(TestData.Register("ana@test.com", "Contraseña123"));

        var error = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => auth.LoginAsync(new LoginRequest { Email = "ana@test.com", Password = "otra-contraseña" }));

        Assert.Equal(401, error.StatusCode);
    }

    [Fact]
    public async Task Login_CorreoInexistente_DaElMismoMensajeQueContraseñaIncorrecta()
    {
        using var db = new TestDatabase();
        var auth = db.CreateAuthService();
        await auth.RegisterAsync(TestData.Register("ana@test.com", "Contraseña123"));

        var wrongPassword = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => auth.LoginAsync(new LoginRequest { Email = "ana@test.com", Password = "mal" }));
        var unknownEmail = await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => auth.LoginAsync(new LoginRequest { Email = "nadie@test.com", Password = "mal" }));

        // Así no se puede averiguar qué correos están registrados.
        Assert.Equal(wrongPassword.Message, unknownEmail.Message);
    }

    [Fact]
    public async Task Login_Correcto_DevuelveUnTokenConElIdDelUsuario()
    {
        using var db = new TestDatabase();
        var auth = db.CreateAuthService();
        var registered = await auth.RegisterAsync(TestData.Register("ana@test.com", "Contraseña123"));

        var response = await auth.LoginAsync(new LoginRequest { Email = "ANA@test.com", Password = "Contraseña123" });

        var token = new JwtSecurityTokenHandler().ReadJwtToken(response.Token);
        Assert.Equal(registered.User.Id.ToString(), token.Subject);
        Assert.True(response.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task GetCurrentUser_UsuarioBorrado_Da401()
    {
        using var db = new TestDatabase();
        var auth = db.CreateAuthService();
        var registered = await auth.RegisterAsync(TestData.Register("ana@test.com"));
        db.Context.Users.Remove(await db.Context.Users.SingleAsync());
        await db.Context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAppException>(() => auth.GetCurrentUserAsync(registered.User.Id));
    }
}

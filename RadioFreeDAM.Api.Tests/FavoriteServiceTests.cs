using Microsoft.EntityFrameworkCore;
using RadioFreeDAM.Api.Models;
using RadioFreeDAM.Api.Services;

namespace RadioFreeDAM.Api.Tests;

public class FavoriteServiceTests
{
    private static readonly FakeStationService Catalog = new(
        TestData.Station("ser", "Cadena SER"),
        TestData.Station("jazz", "Smooth Jazz"));

    private static async Task<int> CreateUserAsync(TestDatabase db, string email = "ana@test.com") =>
        (await db.CreateAuthService().RegisterAsync(TestData.Register(email))).User.Id;

    [Fact]
    public async Task Añadir_Y_Listar_DevuelveLosDatosDeLaEmisora()
    {
        using var db = new TestDatabase();
        var userId = await CreateUserAsync(db);
        var favorites = db.CreateFavoriteService(Catalog);

        await favorites.AddAsync(userId, "ser");
        var list = await favorites.GetAllAsync(userId);

        var favorite = Assert.Single(list);
        Assert.Equal("ser", favorite.StationId);
        Assert.Equal("Cadena SER", favorite.Station?.Name);
    }

    [Fact]
    public async Task EnLaBaseDeDatos_SoloSeGuardaElIdDeLaEmisora()
    {
        using var db = new TestDatabase();
        var userId = await CreateUserAsync(db);

        await db.CreateFavoriteService(Catalog).AddAsync(userId, "ser");

        var saved = await db.Context.Favorites.SingleAsync();
        Assert.Equal("ser", saved.StationId);
        Assert.Equal(userId, saved.UserId);
    }

    [Fact]
    public async Task Añadir_Repetida_Da409()
    {
        using var db = new TestDatabase();
        var userId = await CreateUserAsync(db);
        var favorites = db.CreateFavoriteService(Catalog);
        await favorites.AddAsync(userId, "ser");

        var error = await Assert.ThrowsAsync<ConflictAppException>(() => favorites.AddAsync(userId, "ser"));

        Assert.Equal(409, error.StatusCode);
        Assert.Equal(1, await db.Context.Favorites.CountAsync());
    }

    [Fact]
    public async Task Añadir_EmisoraQueNoExiste_Da404()
    {
        using var db = new TestDatabase();
        var userId = await CreateUserAsync(db);

        await Assert.ThrowsAsync<NotFoundAppException>(
            () => db.CreateFavoriteService(Catalog).AddAsync(userId, "no-existe"));
    }

    [Fact]
    public async Task Añadir_ParaUnUsuarioQueNoExiste_Da401()
    {
        using var db = new TestDatabase();

        await Assert.ThrowsAsync<UnauthorizedAppException>(
            () => db.CreateFavoriteService(Catalog).AddAsync(userId: 999, "ser"));
    }

    [Fact]
    public async Task Quitar_UnaQueNoEsFavorita_Da404()
    {
        using var db = new TestDatabase();
        var userId = await CreateUserAsync(db);

        await Assert.ThrowsAsync<NotFoundAppException>(
            () => db.CreateFavoriteService(Catalog).RemoveAsync(userId, "ser"));
    }

    [Fact]
    public async Task CadaUsuario_SoloVeSusFavoritas()
    {
        using var db = new TestDatabase();
        var ana = await CreateUserAsync(db, "ana@test.com");
        var luis = await CreateUserAsync(db, "luis@test.com");
        var favorites = db.CreateFavoriteService(Catalog);

        await favorites.AddAsync(ana, "ser");
        await favorites.AddAsync(luis, "jazz");
        // La misma emisora puede ser favorita de dos usuarios distintos.
        await favorites.AddAsync(luis, "ser");

        Assert.Equal(["ser"], (await favorites.GetAllAsync(ana)).Select(f => f.StationId));
        Assert.Equal(2, (await favorites.GetAllAsync(luis)).Count);
    }

    [Fact]
    public async Task Listar_EmisoraQueYaNoEstaEnElCatalogo_SeDevuelveSinDatos()
    {
        using var db = new TestDatabase();
        var userId = await CreateUserAsync(db);
        var catalog = new FakeStationService(TestData.Station("ser", "Cadena SER"));
        var favorites = db.CreateFavoriteService(catalog);
        await favorites.AddAsync(userId, "ser");

        catalog.Remove("ser");
        var favorite = Assert.Single(await favorites.GetAllAsync(userId));

        // Sigue en la lista para que el usuario pueda quitarla.
        Assert.Equal("ser", favorite.StationId);
        Assert.Null(favorite.Station);
    }

    [Fact]
    public async Task LaBaseDeDatos_RechazaDuplicados_AunqueSeSalteElServicio()
    {
        using var db = new TestDatabase();
        var userId = await CreateUserAsync(db);
        db.Context.Favorites.Add(new Favorite { UserId = userId, StationId = "ser", AddedAt = DateTime.UtcNow });
        db.Context.Favorites.Add(new Favorite { UserId = userId, StationId = "ser", AddedAt = DateTime.UtcNow });

        // Índice único (usuario, emisora).
        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task LaBaseDeDatos_RechazaFavoritasDeUsuariosInexistentes()
    {
        using var db = new TestDatabase();
        db.Context.Favorites.Add(new Favorite { UserId = 999, StationId = "ser", AddedAt = DateTime.UtcNow });

        // Clave foránea a rfd_users.
        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }
}

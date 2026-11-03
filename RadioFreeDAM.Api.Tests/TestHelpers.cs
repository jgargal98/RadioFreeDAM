using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RadioFreeDAM.Api.Data;
using RadioFreeDAM.Api.Models;
using RadioFreeDAM.Api.Repositories;
using RadioFreeDAM.Api.Services;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Tests;

/// <summary>
/// Base de datos SQLite en memoria con las mismas tablas, índices únicos y claves foráneas que la real.
/// Cada prueba tiene la suya, vacía; desaparece al terminar.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        Context = new RadioFreeDamDbContext(
            new DbContextOptionsBuilder<RadioFreeDamDbContext>().UseSqlite(_connection).Options);
        Context.Database.EnsureCreated();
    }

    public RadioFreeDamDbContext Context { get; }

    public AuthService CreateAuthService() => new(
        new UserRepository(Context),
        new PasswordHasher<User>(),
        new TokenService(Options.Create(TestJwt.Options), TimeProvider.System),
        TimeProvider.System);

    public FavoriteService CreateFavoriteService(IStationService stations) => new(
        new FavoriteRepository(Context),
        new UserRepository(Context),
        stations,
        TimeProvider.System);

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}

public static class TestJwt
{
    public static readonly JwtOptions Options = new() { Key = "clave-de-pruebas-de-al-menos-32-caracteres!!" };
}

/// <summary>Catálogo falso: las emisoras que se le den, sin llamar a Radio Browser.</summary>
public sealed class FakeStationService(params StationDto[] stations) : IStationService
{
    private readonly List<StationDto> _stations = [.. stations];

    public void Remove(string id) => _stations.RemoveAll(s => s.Id == id);

    public Task<IReadOnlyList<StationDto>> GetTopStationsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StationDto>>(_stations);

    public Task<IReadOnlyList<StationDto>> SearchByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StationDto>>(_stations.Where(s => s.Name.Contains(name)).ToList());

    public Task<IReadOnlyList<StationDto>> GetByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<StationDto>>(_stations.Where(s => ids.Contains(s.Id)).ToList());
}

public static class TestData
{
    public static StationDto Station(string id, string name) =>
        new() { Id = id, Name = name, StreamUrl = $"https://stream.example.com/{id}", Country = "Spain" };

    public static RegisterRequest Register(string email, string password = "Contraseña123") =>
        new() { Name = "Usuario de prueba", Email = email, Password = password };
}

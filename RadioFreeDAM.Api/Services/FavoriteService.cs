using Microsoft.EntityFrameworkCore;
using RadioFreeDAM.Api.Models;
using RadioFreeDAM.Api.Repositories;
using RadioFreeDAM.Shared.Dtos;

namespace RadioFreeDAM.Api.Services;

/// <summary>
/// Favoritas del usuario. En la base de datos solo se guarda el Id de la emisora;
/// al listar, los datos se completan con el catálogo (Radio Browser, a través de la caché).
/// </summary>
public class FavoriteService(
    IFavoriteRepository favorites,
    IUserRepository users,
    IStationService stations,
    TimeProvider timeProvider)
{
    public async Task<IReadOnlyList<FavoriteDto>> GetAllAsync(int userId, CancellationToken cancellationToken = default)
    {
        var saved = await favorites.GetByUserAsync(userId, cancellationToken);
        if (saved.Count == 0)
            return [];

        var found = (await stations.GetByIdsAsync(saved.Select(f => f.StationId).ToList(), cancellationToken))
            .ToDictionary(s => s.Id);

        return saved
            .Select(f => ToDto(f, found.GetValueOrDefault(f.StationId)))
            .ToList();
    }

    public async Task<FavoriteDto> AddAsync(int userId, string stationId, CancellationToken cancellationToken = default)
    {
        // Regla del PDF: la favorita pertenece a un usuario existente.
        if (await users.GetByIdAsync(userId, cancellationToken) is null)
            throw new UnauthorizedAppException("La sesión no es válida. Vuelve a iniciar sesión.");

        // Regla del PDF: no se puede duplicar para el mismo usuario.
        if (await favorites.GetAsync(userId, stationId, cancellationToken) is not null)
            throw new ConflictAppException("Esta emisora ya está en tus favoritas.");

        // Solo se aceptan emisoras que existen de verdad en el catálogo.
        var station = (await stations.GetByIdsAsync([stationId], cancellationToken)).FirstOrDefault()
            ?? throw new NotFoundAppException("Esa emisora no existe en el catálogo.");

        var favorite = new Favorite
        {
            UserId = userId,
            StationId = stationId,
            AddedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        try
        {
            await favorites.AddAsync(favorite, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Dos peticiones a la vez: el índice único (usuario, emisora) ha rechazado la segunda.
            throw new ConflictAppException("Esta emisora ya está en tus favoritas.");
        }

        return ToDto(favorite, station);
    }

    public async Task RemoveAsync(int userId, string stationId, CancellationToken cancellationToken = default)
    {
        var favorite = await favorites.GetAsync(userId, stationId, cancellationToken)
            ?? throw new NotFoundAppException("Esta emisora no está en tus favoritas.");

        await favorites.RemoveAsync(favorite, cancellationToken);
    }

    private static FavoriteDto ToDto(Favorite favorite, StationDto? station) => new()
    {
        StationId = favorite.StationId,
        AddedAt = favorite.AddedAt,
        Station = station
    };
}

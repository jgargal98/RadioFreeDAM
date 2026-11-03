using RadioFreeDAM.Api.Models;

namespace RadioFreeDAM.Api.Repositories;

/// <summary>Acceso a la tabla de favoritas.</summary>
public interface IFavoriteRepository
{
    /// <summary>Favoritas del usuario, de la más reciente a la más antigua.</summary>
    Task<List<Favorite>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<Favorite?> GetAsync(int userId, string stationId, CancellationToken cancellationToken = default);

    Task AddAsync(Favorite favorite, CancellationToken cancellationToken = default);

    Task RemoveAsync(Favorite favorite, CancellationToken cancellationToken = default);
}

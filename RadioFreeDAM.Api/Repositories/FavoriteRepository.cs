using Microsoft.EntityFrameworkCore;
using RadioFreeDAM.Api.Data;
using RadioFreeDAM.Api.Models;

namespace RadioFreeDAM.Api.Repositories;

public class FavoriteRepository(RadioFreeDamDbContext db) : IFavoriteRepository
{
    public Task<List<Favorite>> GetByUserAsync(int userId, CancellationToken cancellationToken = default) =>
        db.Favorites
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.AddedAt)
            .ToListAsync(cancellationToken);

    public Task<Favorite?> GetAsync(int userId, string stationId, CancellationToken cancellationToken = default) =>
        db.Favorites.FirstOrDefaultAsync(f => f.UserId == userId && f.StationId == stationId, cancellationToken);

    public async Task AddAsync(Favorite favorite, CancellationToken cancellationToken = default)
    {
        db.Favorites.Add(favorite);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveAsync(Favorite favorite, CancellationToken cancellationToken = default)
    {
        db.Favorites.Remove(favorite);
        await db.SaveChangesAsync(cancellationToken);
    }
}

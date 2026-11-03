namespace RadioFreeDAM.Services;

/// <summary>Favoritas del usuario en nuestra API. Necesita sesión iniciada (token en el HttpClient).</summary>
public interface IFavoritesApiService
{
    Task<IReadOnlyList<FavoriteDto>> GetAllAsync();

    /// <exception cref="ApiException">409 si ya es favorita, 404 si la emisora no existe.</exception>
    Task<FavoriteDto> AddAsync(string stationId);

    Task RemoveAsync(string stationId);
}

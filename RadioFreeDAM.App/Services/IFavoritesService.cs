namespace RadioFreeDAM.Services;

/// <summary>
/// Favoritas del usuario dentro de la app. Igual que IPlayerService con la reproducción, es el único sitio
/// donde vive este estado: el corazón del Reproductor y la pantalla Favoritos leen de aquí y siempre coinciden.
/// </summary>
public interface IFavoritesService
{
    IReadOnlyList<FavoriteDto> Favorites { get; }

    bool IsLoaded { get; }

    /// <summary>Se lanza cuando cambia la lista (carga, alta, baja o cierre de sesión).</summary>
    event EventHandler? FavoritesChanged;

    bool IsFavorite(string stationId);

    /// <summary>Vuelve a pedir la lista a la API.</summary>
    /// <exception cref="ApiException">Si la API devuelve un error.</exception>
    Task LoadAsync();

    /// <exception cref="ApiException">Si la API rechaza la favorita.</exception>
    Task AddAsync(StationDto station);

    /// <exception cref="ApiException">Si la API devuelve un error.</exception>
    Task RemoveAsync(string stationId);
}

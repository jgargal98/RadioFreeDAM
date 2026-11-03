using CommunityToolkit.Mvvm.ComponentModel;

namespace RadioFreeDAM.ViewModels;

/// <summary>Una fila de la pantalla Favoritos.</summary>
public partial class FavoriteItemViewModel(FavoriteDto favorite) : ObservableObject
{
    public FavoriteDto Favorite { get; } = favorite;

    public string StationId => Favorite.StationId;

    /// <summary>False si Radio Browser ya no tiene esta emisora: solo se puede quitar.</summary>
    public bool IsAvailable => Favorite.Station is not null;

    public string Name => Favorite.Station?.Name ?? "Emisora no disponible";

    public string Subtitle => Favorite.Station is null
        ? "Ya no está en el catálogo de Radio Browser."
        : StationText.Describe(Favorite.Station.Country, Favorite.Station.Tags, maxTags: 3);

    public string? FaviconUrl => Favorite.Station?.FaviconUrl;

    public bool HasFavicon => !string.IsNullOrWhiteSpace(FaviconUrl);

    public string Initial => string.IsNullOrWhiteSpace(Favorite.Station?.Name) ? "?" : Favorite.Station.Name[..1].ToUpperInvariant();

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }
}

using CommunityToolkit.Mvvm.ComponentModel;

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// A card of the list: a single station or a network that groups several (for example "COPE").
/// Adds the visual state (selected / playing) and the colors of the card, which are not part of the API data.
/// </summary>
public partial class StationItemViewModel : ObservableObject
{
    /// <summary>Color taken from the logo, once known. Until then (or without logo) a stable color from the name is used.</summary>
    private Color? _logoColor;

    public StationItemViewModel(StationDto station)
    {
        Station = station;
        RefreshColors();
    }

    public StationItemViewModel(CatalogEntryDto entry)
    {
        Station = entry.Station;
        Network = entry.Network;
        RefreshColors();
    }

    /// <summary>The station, or null if the row is a network.</summary>
    public StationDto? Station { get; }

    /// <summary>The network, or null if the row is a single station.</summary>
    public NetworkDto? Network { get; }

    public bool IsNetwork => Network is not null;

    public string Name => Network?.Name ?? Station?.Name ?? string.Empty;

    public string? FaviconUrl => Network is not null ? Network.FaviconUrl : Station?.FaviconUrl;

    public string Initial => string.IsNullOrWhiteSpace(Name) ? "?" : Name[..1].ToUpperInvariant();

    public string Subtitle => Network is not null
        ? "Varias emisoras · elige tu ciudad"
        : StationText.Describe(Station!.Country, Station.Tags, maxTags: 3);

    /// <summary>Does this row correspond to the selected station? (A network, if the station belongs to it.)</summary>
    public bool Represents(StationDto? current) => current is not null && (Network is not null
        ? current.NetworkId == Network.Id
        : current.Id == Station!.Id);

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    // ---------- Colors ----------

    /// <summary>Soft background of the card: a little of the station color over the page color.</summary>
    [ObservableProperty]
    public partial Brush CardBackground { get; set; } = Brush.Transparent;

    /// <summary>Strong color of the station: used for the avatar when the logo is missing or fails.</summary>
    [ObservableProperty]
    public partial Color CardAccent { get; set; } = AccentPalette.DefaultAccent;

    /// <summary>Called when the color of the logo has been calculated.</summary>
    public void SetLogoColor(Color? color)
    {
        _logoColor = color;
        RefreshColors();
    }

    /// <summary>Recalculates the colors for the current theme. Called when the theme changes.</summary>
    public void RefreshColors()
    {
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var accent = AccentPalette.Fill(_logoColor ?? ColorFromName(Name));

        CardAccent = accent;
        CardBackground = new SolidColorBrush(AccentPalette.Blend(
            isDark ? AccentPalette.DarkCard : AccentPalette.LightCard, accent, isDark ? 0.26f : 0.16f));
    }

    /// <summary>The same name always gives the same color, so stations without a logo do not change every time.</summary>
    private static Color ColorFromName(string name)
    {
        // A stable hash (string.GetHashCode changes between runs).
        var hash = name.Aggregate(17, (current, c) => unchecked(current * 31 + c));
        var hue = (hash & 0x7FFFFFFF) % 360 / 360.0;
        return Color.FromHsla(hue, 0.6, 0.5);
    }
}

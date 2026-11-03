using CommunityToolkit.Mvvm.ComponentModel;

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Una fila del listado: una emisora suelta o una cadena que agrupa varias (por ejemplo "COPE").
/// Añade el estado visual (seleccionada / sonando), que no forma parte de los datos de la API.
/// </summary>
public partial class StationItemViewModel : ObservableObject
{
    public StationItemViewModel(StationDto station) => Station = station;

    public StationItemViewModel(CatalogEntryDto entry)
    {
        Station = entry.Station;
        Network = entry.Network;
    }

    /// <summary>La emisora, o null si la fila es una cadena.</summary>
    public StationDto? Station { get; }

    /// <summary>La cadena, o null si la fila es una emisora suelta.</summary>
    public NetworkDto? Network { get; }

    public bool IsNetwork => Network is not null;

    public string Name => Network?.Name ?? Station?.Name ?? string.Empty;

    public string? FaviconUrl => Network is not null ? Network.FaviconUrl : Station?.FaviconUrl;

    public string Initial => string.IsNullOrWhiteSpace(Name) ? "?" : Name[..1].ToUpperInvariant();

    public string Subtitle => Network is not null
        ? "Varias emisoras · elige tu ciudad"
        : StationText.Describe(Station!.Country, Station.Tags, maxTags: 3);

    /// <summary>¿Esta fila corresponde a la emisora seleccionada? (Una cadena, si la emisora es de esa cadena.)</summary>
    public bool Represents(StationDto? current) => current is not null && (Network is not null
        ? current.NetworkId == Network.Id
        : current.Id == Station!.Id);

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }
}

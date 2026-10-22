using CommunityToolkit.Mvvm.ComponentModel;

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Una fila del listado de emisoras. Envuelve el DTO y añade el estado visual
/// (seleccionada / sonando), que no forma parte de los datos de la API.
/// </summary>
public partial class StationItemViewModel(StationDto station) : ObservableObject
{
    public StationDto Station { get; } = station;

    public string Name => Station.Name;

    public string Initial => string.IsNullOrWhiteSpace(Station.Name) ? "?" : Station.Name[..1].ToUpperInvariant();

    public string Subtitle => string.IsNullOrWhiteSpace(Station.Tags)
        ? Station.Country
        : $"{Station.Country} · {Station.Tags.Replace(",", ", ")}";

    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }
}

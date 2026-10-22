using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Pantalla Favoritos. En la Fase 1 solo está preparada: la lista existe pero aún no se guarda nada.
/// En la Fase 3 se rellenará desde la API para el usuario que haya iniciado sesión.
/// </summary>
public partial class FavoritesViewModel : ObservableObject
{
    public ObservableCollection<StationDto> Favorites { get; } = [];
}

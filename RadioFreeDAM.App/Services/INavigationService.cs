namespace RadioFreeDAM.Services;

/// <summary>
/// Navegación entre pantallas. Los ViewModels la usan sin depender directamente de Shell.
/// </summary>
public interface INavigationService
{
    Task GoToHomeAsync();

    Task GoToPlayerAsync();
}

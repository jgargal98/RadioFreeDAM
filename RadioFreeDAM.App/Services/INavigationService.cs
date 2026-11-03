namespace RadioFreeDAM.Services;

/// <summary>
/// Navegación entre pantallas. Los ViewModels la usan sin depender directamente de Shell.
/// </summary>
public interface INavigationService
{
    Task GoToHomeAsync();

    Task GoToPlayerAsync();

    /// <summary>Abre Iniciar sesión encima de la pantalla actual.</summary>
    Task GoToLoginAsync();

    /// <summary>Abre Crear cuenta encima de Iniciar sesión.</summary>
    Task GoToRegisterAsync();

    /// <summary>Abre la lista de emisoras de una cadena (para elegir ciudad).</summary>
    Task GoToNetworkAsync(NetworkDto network);

    /// <summary>Abre Ajustes encima de la pantalla actual.</summary>
    Task GoToSettingsAsync();

    /// <summary>Cierra las pantallas de cuenta y vuelve a donde estaba el usuario.</summary>
    Task CloseAccountPagesAsync();
}

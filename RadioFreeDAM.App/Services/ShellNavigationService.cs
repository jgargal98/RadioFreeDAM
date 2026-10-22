namespace RadioFreeDAM.Services;

public class ShellNavigationService : INavigationService
{
    // "//" indica ruta absoluta: cambia de pestaña en lugar de apilar una página nueva.
    public Task GoToHomeAsync() => Shell.Current.GoToAsync("//home");

    public Task GoToPlayerAsync() => Shell.Current.GoToAsync("//player");
}

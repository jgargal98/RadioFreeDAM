namespace RadioFreeDAM.Services;

public class ShellNavigationService : INavigationService
{
    // "//" indica ruta absoluta: cambia de pestaña en lugar de apilar una página nueva.
    public Task GoToHomeAsync() => Shell.Current.GoToAsync("//home");

    public Task GoToPlayerAsync() => Shell.Current.GoToAsync("//player");

    // Sin "//": se apila encima de la pestaña actual (rutas registradas en MauiProgram).
    public Task GoToLoginAsync() => Shell.Current.GoToAsync(Routes.Login);

    public Task GoToRegisterAsync() => Shell.Current.GoToAsync(Routes.Register);

    public async Task CloseAccountPagesAsync()
    {
        // Quita Iniciar sesión / Crear cuenta de la pila y deja la pantalla de debajo.
        var stack = Shell.Current.Navigation.NavigationStack;
        var depth = stack.Count(page => page is LoginPage or RegisterPage);
        if (depth > 0)
            await Shell.Current.GoToAsync(string.Join("/", Enumerable.Repeat("..", depth)));
    }
}

public static class Routes
{
    public const string Login = "login";

    public const string Register = "register";
}

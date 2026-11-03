namespace RadioFreeDAM.Services;

/// <summary>
/// Cambia el tema con Application.UserAppTheme. Todos los colores de la app usan AppThemeBinding,
/// así que la interfaz se actualiza sola. La elección se guarda en Preferences (no es un dato sensible).
/// </summary>
public class ThemeService : IThemeService
{
    private const string PreferenceKey = "rfd_theme";

    public ThemeChoice Current { get; private set; } = ThemeChoice.System;

    public void ApplySaved()
    {
        var saved = Preferences.Default.Get(PreferenceKey, nameof(ThemeChoice.System));
        Apply(Enum.TryParse<ThemeChoice>(saved, out var choice) ? choice : ThemeChoice.System);
    }

    public void Apply(ThemeChoice choice)
    {
        Current = choice;
        Preferences.Default.Set(PreferenceKey, choice.ToString());

        if (Application.Current is { } app)
        {
            app.UserAppTheme = choice switch
            {
                ThemeChoice.Light => AppTheme.Light,
                ThemeChoice.Dark => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
        }
    }
}

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Fondo y ondas del Reproductor a partir del color de acento. El color se suaviza mezclándolo con el fondo
/// de la app, así el texto se lee igual de bien en claro y en oscuro sea cual sea el acento.
/// </summary>
public static class PlayerColors
{
    /// <summary>Degradado de arriba (color de acento) hacia abajo (fondo normal de la app).</summary>
    public static Brush Background(Color accent, bool isDark)
    {
        var page = isDark ? AccentPalette.DarkBackground : AccentPalette.LightBackground;
        var vivid = WithLuminosity(accent, isDark ? 0.42f : 0.5f);
        var top = AccentPalette.Blend(page, vivid, isDark ? 0.38f : 0.3f);

        return new LinearGradientBrush(
            [
                new GradientStop(top, 0f),
                new GradientStop(top, 0.25f),
                new GradientStop(page, 0.8f)
            ],
            new Point(0, 0),
            new Point(0, 1));
    }

    /// <summary>Color de las ondas: más oscuro en tema claro y más claro en tema oscuro.</summary>
    public static Color Waves(Color accent, bool isDark) =>
        WithLuminosity(accent, isDark ? 0.7f : 0.38f).WithAlpha(0.5f);

    private static Color WithLuminosity(Color color, float luminosity)
    {
        color.ToHsl(out var hue, out var saturation, out _);
        return Color.FromHsla(hue, Math.Max(saturation, 0.35f), luminosity);
    }
}

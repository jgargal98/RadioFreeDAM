namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Colores del Reproductor a partir del color de la emisora. El color se suaviza mezclándolo con el fondo
/// de la app, así el texto se lee igual de bien en claro y en oscuro sea cual sea la emisora.
/// </summary>
public static class PlayerColors
{
    /// <summary>Verde de la app: se usa cuando la emisora no tiene logo o es en blanco y negro.</summary>
    public static readonly Color DefaultAccent = Color.FromArgb("#1DB954");

    private static readonly Color LightBackground = Color.FromArgb("#F5F5F7");
    private static readonly Color DarkBackground = Color.FromArgb("#121212");

    /// <summary>Degradado de arriba (color de la emisora) hacia abajo (fondo normal de la app).</summary>
    public static Brush Background(Color accent, bool isDark)
    {
        var page = isDark ? DarkBackground : LightBackground;
        var vivid = WithLuminosity(accent, isDark ? 0.42f : 0.5f);
        var top = Blend(page, vivid, isDark ? 0.38f : 0.3f);

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

    private static Color Blend(Color from, Color to, float amount) => new(
        from.Red + (to.Red - from.Red) * amount,
        from.Green + (to.Green - from.Green) * amount,
        from.Blue + (to.Blue - from.Blue) * amount);
}

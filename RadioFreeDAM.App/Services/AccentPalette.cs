namespace RadioFreeDAM.Services;

/// <summary>
/// Convierte un color de acento cualquiera en los colores que usa la interfaz, ajustados a cada tema
/// para que siempre se lean bien (por ejemplo, un azul marino se aclara para los botones y el texto
/// encima pasa a blanco).
/// </summary>
public static class AccentPalette
{
    public static readonly Color DefaultAccent = Color.FromArgb("#1DB954");

    public static readonly Color LightBackground = Color.FromArgb("#F5F5F7");
    public static readonly Color DarkBackground = Color.FromArgb("#121212");

    /// <summary>Relleno de botones, avatares y pestaña activa.</summary>
    public static Color Fill(Color accent)
    {
        accent.ToHsl(out var hue, out var saturation, out var luminosity);
        return Color.FromHsla(hue, Math.Max(saturation, 0.5f), Math.Clamp(luminosity, 0.42f, 0.55f));
    }

    /// <summary>Texto encima del relleno: negro o blanco, el que tenga más contraste.</summary>
    public static Color OnFill(Color fill) =>
        RelativeLuminance(fill) > 0.179 ? Color.FromArgb("#0B0B0B") : Colors.White;

    /// <summary>Acento usado como texto sobre el fondo de la página.</summary>
    public static Color Text(Color accent, bool isDark)
    {
        accent.ToHsl(out var hue, out var saturation, out _);
        return Color.FromHsla(hue, Math.Max(saturation, 0.5f), isDark ? 0.68f : 0.32f);
    }

    /// <summary>Fondo suave (emisora seleccionada).</summary>
    public static Color Soft(Color accent, bool isDark) =>
        Blend(isDark ? DarkBackground : LightBackground, Fill(accent), isDark ? 0.25f : 0.16f);

    public static Color Blend(Color from, Color to, float amount) => new(
        from.Red + (to.Red - from.Red) * amount,
        from.Green + (to.Green - from.Green) * amount,
        from.Blue + (to.Blue - from.Blue) * amount);

    /// <summary>Luminancia relativa (WCAG), para decidir el contraste.</summary>
    private static double RelativeLuminance(Color color)
    {
        static double Channel(float c) => c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        return 0.2126 * Channel(color.Red) + 0.7152 * Channel(color.Green) + 0.0722 * Channel(color.Blue);
    }
}

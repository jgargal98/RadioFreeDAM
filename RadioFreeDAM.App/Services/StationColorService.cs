using System.Collections.Concurrent;
using SkiaSharp;

namespace RadioFreeDAM.Services;

/// <summary>
/// Descarga el logo de la emisora (igual que hace el control Image para mostrarlo), lo reduce a 32×32
/// y busca el tono que más se repite, ignorando fondos blancos, negros y transparentes.
/// Los resultados se guardan en memoria para no repetir el trabajo.
/// </summary>
public class StationColorService : IStationColorService
{
    private const int SampleSize = 32;
    private const int MaxLogoBytes = 2 * 1024 * 1024;

    // Los logos están en servidores de cada emisora, no en nuestra API: cliente propio y sin reintentos.
    private static readonly HttpClient LogoClient = new() { Timeout = TimeSpan.FromSeconds(5) };

    private readonly ConcurrentDictionary<string, Color?> _cache = new();

    public async Task<Color?> GetAccentColorAsync(StationDto station, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(station.FaviconUrl))
            return null;

        if (_cache.TryGetValue(station.Id, out var cached))
            return cached;

        Color? color = null;
        try
        {
            var bytes = await LogoClient.GetByteArrayAsync(station.FaviconUrl, cancellationToken);
            if (bytes.Length <= MaxLogoBytes)
                color = await Task.Run(() => DominantColor(bytes), cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Logo inexistente, formato no soportado (por ejemplo SVG) o servidor caído: sin color.
        }

        if (!cancellationToken.IsCancellationRequested)
            _cache[station.Id] = color;

        return color;
    }

    private static Color? DominantColor(byte[] imageBytes)
    {
        using var original = SKBitmap.Decode(imageBytes);
        if (original is null)
            return null;

        using var small = original.Resize(new SKImageInfo(SampleSize, SampleSize), new SKSamplingOptions(SKFilterMode.Linear));
        if (small is null)
            return null;

        // 12 grupos de tono (30° cada uno). Cada píxel con color suma más cuanto más saturado es.
        var weights = new double[12];
        var sums = new (double R, double G, double B, int Count)[12];
        var colorful = 0;

        for (var y = 0; y < SampleSize; y++)
        for (var x = 0; x < SampleSize; x++)
        {
            var pixel = small.GetPixel(x, y);
            if (pixel.Alpha < 128)
                continue;

            var color = Color.FromRgb(pixel.Red, pixel.Green, pixel.Blue);
            color.ToHsl(out var hue, out var saturation, out var luminosity);

            // Fondos blancos/negros y grises no cuentan.
            if (luminosity > 0.92f || luminosity < 0.08f || saturation < 0.25f)
                continue;

            var bucket = Math.Min((int)(hue * 12), 11);
            weights[bucket] += 1 + saturation;
            sums[bucket] = (sums[bucket].R + color.Red, sums[bucket].G + color.Green, sums[bucket].B + color.Blue, sums[bucket].Count + 1);
            colorful++;
        }

        // Si casi no hay color (logo en blanco y negro), mejor usar el verde de la app.
        if (colorful < SampleSize * SampleSize / 20)
            return null;

        var best = Array.IndexOf(weights, weights.Max());
        var (r, g, b, count) = sums[best];
        return new Color((float)(r / count), (float)(g / count), (float)(b / count));
    }
}

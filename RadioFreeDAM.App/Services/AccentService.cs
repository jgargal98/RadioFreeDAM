using System.Globalization;

namespace RadioFreeDAM.Services;

public class AccentService(IPlayerService player, IStationColorService stationColors) : IAccentService
{
    private const string PreferenceKey = "rfd_accent";

    /// <summary>Saturación y brillo fijos para la barra de tono: así cualquier tono se lee bien.</summary>
    private const float HueSaturation = 0.65f;
    private const float HueLuminosity = 0.48f;

    private string? _stationId;
    private Color? _stationColor;
    private CancellationTokenSource? _stationCts;

    public IReadOnlyList<(string Name, Color Color)> Presets { get; } =
    [
        ("Verde", Color.FromArgb("#1DB954")),
        ("Azul", Color.FromArgb("#2979FF")),
        ("Violeta", Color.FromArgb("#7C4DFF")),
        ("Rosa", Color.FromArgb("#E91E7A")),
        ("Naranja", Color.FromArgb("#FF6B35")),
        ("Rojo", Color.FromArgb("#E53935"))
    ];

    public AccentChoice Choice { get; private set; } = AccentChoice.Station;

    public Color Accent => Choice.Source switch
    {
        AccentSource.Preset => Choice.Color ?? AccentPalette.DefaultAccent,
        AccentSource.Hue => Color.FromHsla(Choice.Hue / 360, HueSaturation, HueLuminosity),
        _ => _stationColor ?? AccentPalette.DefaultAccent
    };

    public event EventHandler? AccentChanged;

    public void Initialize()
    {
        Choice = Load();

        player.StateChanged += (_, _) => _ = OnStationMaybeChangedAsync();
        if (Application.Current is { } app)
            app.RequestedThemeChanged += (_, _) => Apply();

        Apply();
    }

    public void SetChoice(AccentChoice choice)
    {
        Choice = choice;
        Save(choice);
        Apply();
    }

    /// <summary>Al cambiar de emisora, calcula el color de su logo (en segundo plano).</summary>
    private async Task OnStationMaybeChangedAsync()
    {
        var station = player.CurrentStation;
        if (station?.Id == _stationId)
            return;

        _stationId = station?.Id;
        _stationCts?.Cancel();
        var cts = _stationCts = new CancellationTokenSource();

        // Hasta tener el color nuevo se mantiene el anterior, para no parpadear a verde.
        var color = station is null ? null : await stationColors.GetAccentColorAsync(station, cts.Token);
        if (cts.IsCancellationRequested)
            return;

        _stationColor = color;
        if (Choice.Source == AccentSource.Station)
            Apply();
    }

    /// <summary>Publica los colores como recursos de la app para el tema actual.</summary>
    private void Apply()
    {
        if (Application.Current is not { } app)
            return;

        var isDark = app.RequestedTheme == AppTheme.Dark;
        var fill = AccentPalette.Fill(Accent);

        app.Resources["Primary"] = fill;
        app.Resources["PrimaryBrush"] = new SolidColorBrush(fill);
        app.Resources["OnPrimary"] = AccentPalette.OnFill(fill);
        app.Resources["PrimaryText"] = AccentPalette.Text(Accent, isDark);
        app.Resources["PrimarySoft"] = AccentPalette.Soft(Accent, isDark);

        AccentChanged?.Invoke(this, EventArgs.Empty);
    }

    // Formato guardado: "station", "preset:#RRGGBB" o "hue:210".
    private static AccentChoice Load()
    {
        var saved = Preferences.Default.Get(PreferenceKey, "station");

        if (saved.StartsWith("preset:") && Color.TryParse(saved["preset:".Length..], out var color))
            return new AccentChoice(AccentSource.Preset, color);

        if (saved.StartsWith("hue:") && double.TryParse(saved["hue:".Length..], NumberStyles.Float, CultureInfo.InvariantCulture, out var hue))
            return new AccentChoice(AccentSource.Hue, Hue: hue);

        return AccentChoice.Station;
    }

    private static void Save(AccentChoice choice) => Preferences.Default.Set(PreferenceKey, choice.Source switch
    {
        AccentSource.Preset => $"preset:{choice.Color?.ToArgbHex()}",
        AccentSource.Hue => $"hue:{choice.Hue.ToString(CultureInfo.InvariantCulture)}",
        _ => "station"
    });
}

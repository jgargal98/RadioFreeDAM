using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RadioFreeDAM.ViewModels;

/// <summary>
/// Pantalla Ajustes: cuenta (iniciar o cerrar sesión), tema claro/oscuro y color de acento.
/// </summary>
public partial class SettingsViewModel : ObservableObject
{
    private readonly ISessionService _session;
    private readonly IThemeService _theme;
    private readonly IAccentService _accent;
    private readonly INavigationService _navigation;

    /// <summary>Evita guardar el tono mientras se carga el valor inicial de la barra.</summary>
    private bool _loading;

    public SettingsViewModel(
        ISessionService session,
        IThemeService theme,
        IAccentService accent,
        INavigationService navigation)
    {
        _session = session;
        _theme = theme;
        _accent = accent;
        _navigation = navigation;

        ThemeOptions =
        [
            new ThemeOptionViewModel("Sistema", ThemeChoice.System),
            new ThemeOptionViewModel("Claro", ThemeChoice.Light),
            new ThemeOptionViewModel("Oscuro", ThemeChoice.Dark)
        ];

        Presets = new(_accent.Presets.Select(p => new AccentPresetViewModel(p.Name, p.Color)));

        _session.SessionChanged += (_, _) => UpdateAccount();
        UpdateAccount();
        UpdateTheme();
        UpdateAccent();
    }

    // ---------- Cuenta ----------

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoggedOut))]
    public partial bool IsLoggedIn { get; set; }

    public bool IsLoggedOut => !IsLoggedIn;

    [ObservableProperty]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserEmail { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserInitial { get; set; } = string.Empty;

    [RelayCommand]
    private Task LoginAsync() => _navigation.GoToLoginAsync();

    [RelayCommand]
    private void Logout() => _session.SignOut();

    private void UpdateAccount()
    {
        var user = _session.CurrentUser;
        IsLoggedIn = user is not null;
        UserName = user?.Name ?? string.Empty;
        UserEmail = user?.Email ?? string.Empty;
        UserInitial = string.IsNullOrWhiteSpace(user?.Name) ? "?" : user.Name[..1].ToUpperInvariant();
    }

    // ---------- Tema ----------

    public IReadOnlyList<ThemeOptionViewModel> ThemeOptions { get; }

    [RelayCommand]
    private void SelectTheme(ThemeOptionViewModel option)
    {
        _theme.Apply(option.Value);
        UpdateTheme();
    }

    private void UpdateTheme()
    {
        foreach (var option in ThemeOptions)
            option.IsSelected = option.Value == _theme.Current;
    }

    // ---------- Color de acento ----------

    public ObservableCollection<AccentPresetViewModel> Presets { get; }

    [ObservableProperty]
    public partial bool IsStationAccent { get; set; }

    [ObservableProperty]
    public partial bool IsHueAccent { get; set; }

    /// <summary>Valor de la barra de tono (0–360). Al moverla se aplica al momento.</summary>
    [ObservableProperty]
    public partial double Hue { get; set; } = 200;

    /// <summary>Muestra del color de la barra de tono.</summary>
    [ObservableProperty]
    public partial Color HuePreview { get; set; } = Colors.Gray;

    [RelayCommand]
    private void SelectStationAccent()
    {
        _accent.SetChoice(AccentChoice.Station);
        UpdateAccent();
    }

    [RelayCommand]
    private void SelectPreset(AccentPresetViewModel preset)
    {
        _accent.SetChoice(new AccentChoice(AccentSource.Preset, preset.Color));
        UpdateAccent();
    }

    partial void OnHueChanged(double value)
    {
        HuePreview = PreviewFor(value);
        if (_loading)
            return;

        _accent.SetChoice(new AccentChoice(AccentSource.Hue, Hue: Math.Round(value)));
        UpdateAccent();
    }

    private void UpdateAccent()
    {
        var choice = _accent.Choice;
        IsStationAccent = choice.Source == AccentSource.Station;
        IsHueAccent = choice.Source == AccentSource.Hue;

        foreach (var preset in Presets)
            preset.IsSelected = choice.Source == AccentSource.Preset && choice.Color?.ToArgbHex() == preset.Color.ToArgbHex();

        // Coloca la barra en el tono guardado sin volver a guardarlo.
        if (choice.Source == AccentSource.Hue && Math.Abs(Hue - choice.Hue) > 0.5)
        {
            _loading = true;
            Hue = choice.Hue;
            _loading = false;
        }

        HuePreview = PreviewFor(Hue);
    }

    private static Color PreviewFor(double hue) => AccentPalette.Fill(AccentPalette.FromHue(hue));
}

/// <summary>Una de las opciones de tema.</summary>
public partial class ThemeOptionViewModel(string label, ThemeChoice value) : ObservableObject
{
    public string Label { get; } = label;

    public ThemeChoice Value { get; } = value;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

/// <summary>Uno de los colores fijos de acento.</summary>
public partial class AccentPresetViewModel(string name, Color color) : ObservableObject
{
    public string Name { get; } = name;

    public Color Color { get; } = color;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

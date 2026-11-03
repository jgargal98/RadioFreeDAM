namespace RadioFreeDAM.Services;

/// <summary>
/// Color de acento de toda la app. Lo elige el usuario en Ajustes (por defecto, el color de la emisora).
/// Publica los colores como recursos dinámicos (Primary, OnPrimary, PrimaryText, PrimarySoft):
/// las pantallas los usan con DynamicResource y se repintan solas al cambiar.
/// </summary>
public interface IAccentService
{
    /// <summary>Colores fijos que se ofrecen en Ajustes.</summary>
    IReadOnlyList<(string Name, Color Color)> Presets { get; }

    AccentChoice Choice { get; }

    /// <summary>Acento efectivo, antes de adaptarlo al tema.</summary>
    Color Accent { get; }

    /// <summary>Se lanza cuando cambia el acento (elección, emisora o tema).</summary>
    event EventHandler? AccentChanged;

    /// <summary>Carga la elección guardada y aplica los colores. Se llama al arrancar.</summary>
    void Initialize();

    void SetChoice(AccentChoice choice);
}

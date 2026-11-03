namespace RadioFreeDAM.Models;

/// <summary>De dónde sale el color de acento de la app.</summary>
public enum AccentSource
{
    /// <summary>Color del logo de la emisora seleccionada (verde si no hay o no tiene logo).</summary>
    Station,

    /// <summary>Uno de los colores fijos.</summary>
    Preset,

    /// <summary>Tono elegido con la barra (la app pone saturación y brillo).</summary>
    Hue
}

/// <summary>Elección de color de acento del usuario.</summary>
/// <param name="Color">Solo para <see cref="AccentSource.Preset"/>.</param>
/// <param name="Hue">Solo para <see cref="AccentSource.Hue"/>: de 0 a 360.</param>
public record AccentChoice(AccentSource Source, Color? Color = null, double Hue = 0)
{
    public static readonly AccentChoice Station = new(AccentSource.Station);
}

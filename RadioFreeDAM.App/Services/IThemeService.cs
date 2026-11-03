namespace RadioFreeDAM.Services;

/// <summary>Tema claro/oscuro de la app. La elección se recuerda entre ejecuciones.</summary>
public interface IThemeService
{
    ThemeChoice Current { get; }

    /// <summary>Aplica el tema guardado. Se llama al arrancar la app.</summary>
    void ApplySaved();

    void Apply(ThemeChoice choice);
}

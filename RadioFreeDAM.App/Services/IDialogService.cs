namespace RadioFreeDAM.Services;

/// <summary>Diálogos del sistema, para que los ViewModels no dependan de Shell.</summary>
public interface IDialogService
{
    /// <summary>Muestra una lista de opciones. Devuelve la elegida, o null si se cancela.</summary>
    Task<string?> ChooseAsync(string title, params string[] options);
}

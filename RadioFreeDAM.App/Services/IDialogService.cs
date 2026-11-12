namespace RadioFreeDAM.Services;

/// <summary>System dialogs, so the view models do not depend on Shell.</summary>
public interface IDialogService
{
    /// <summary>Shows a list of options. Returns the chosen one, or null if cancelled.</summary>
    Task<string?> ChooseAsync(string title, params string[] options);
}

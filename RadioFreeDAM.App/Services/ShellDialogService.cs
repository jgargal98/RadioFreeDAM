namespace RadioFreeDAM.Services;

public class ShellDialogService : IDialogService
{
    private const string Cancel = "Cancelar";

    public async Task<string?> ChooseAsync(string title, params string[] options)
    {
        var choice = await Shell.Current.DisplayActionSheetAsync(title, Cancel, null, options);
        return choice is null or Cancel ? null : choice;
    }
}

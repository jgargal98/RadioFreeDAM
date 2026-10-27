namespace RadioFreeDAM.Views;

public partial class PlayerPage : ContentPage
{
    public PlayerPage(PlayerViewModel viewModel, MediaElementPlayerService playerService)
    {
        InitializeComponent();
        BindingContext = viewModel;

        // El control de audio está en esta vista; el servicio es quien lo maneja.
        playerService.Attach(AudioPlayer);
    }
}

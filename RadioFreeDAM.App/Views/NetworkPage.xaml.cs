namespace RadioFreeDAM.Views;

public partial class NetworkPage : ContentPage
{
    public NetworkPage(NetworkViewModel viewModel, PlayerViewModel playerViewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        MiniPlayer.BindingContext = playerViewModel;
    }
}

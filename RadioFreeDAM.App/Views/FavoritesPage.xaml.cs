namespace RadioFreeDAM.Views;

public partial class FavoritesPage : ContentPage
{
    private readonly FavoritesViewModel _viewModel;

    public FavoritesPage(FavoritesViewModel viewModel, PlayerViewModel playerViewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
        MiniPlayer.BindingContext = playerViewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}

namespace RadioFreeDAM.Views;

public partial class RegisterPage : ContentPage
{
    private readonly RegisterViewModel _viewModel;

    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    // Tocar el texto también marca o desmarca la casilla (detalle de interfaz, no lógica).
    private void OnRememberLabelTapped(object? sender, TappedEventArgs e) =>
        _viewModel.RememberMe = !_viewModel.RememberMe;
}

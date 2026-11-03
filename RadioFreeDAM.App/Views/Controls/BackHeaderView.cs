namespace RadioFreeDAM.Views.Controls;

/// <summary>
/// Cabecera con flecha de volver y título, para las pantallas que se abren encima de una pestaña
/// (Ajustes, Iniciar sesión, Crear cuenta). Sustituye a la barra de navegación nativa para que
/// la parte de arriba se vea igual que en las pestañas, también al cambiar de tema.
/// </summary>
public class BackHeaderView : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(BackHeaderView), string.Empty,
        propertyChanged: (view, _, value) => ((BackHeaderView)view)._title.Text = (string)value);

    private readonly Label _title;

    public BackHeaderView()
    {
        var back = new ImageButton
        {
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = 10,
            Background = Brush.Transparent,
            VerticalOptions = LayoutOptions.Center,
            Source = new FontImageSource
            {
                Glyph = Fonts.FluentUI.arrow_left_24_regular,
                FontFamily = Fonts.FluentUI.FontFamily,
                Size = 24
            }
        };
        // El color del icono sigue al tema (claro u oscuro).
        ((FontImageSource)back.Source).SetAppThemeColor(FontImageSource.ColorProperty,
            Color.FromArgb("#0D0D0D"), Color.FromArgb("#EDEDED"));
        SemanticProperties.SetDescription(back, "Volver");

        // Volver es solo navegación de la interfaz: no necesita pasar por el ViewModel.
        back.Clicked += async (_, _) => await Shell.Current.GoToAsync("..");

        _title = new Label { VerticalOptions = LayoutOptions.Center };
        _title.SetDynamicResource(StyleProperty, "Title1");

        Content = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)],
            ColumnSpacing = 4,
            Margin = new Thickness(-10, 0, 0, 0), // alinea la flecha con el margen del contenido
            Children = { back, _title }
        };
        Grid.SetColumn(_title, 1);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
}

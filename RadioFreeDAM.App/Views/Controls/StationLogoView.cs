namespace RadioFreeDAM.Views.Controls;

/// <summary>
/// Logo de una emisora o cadena. Si no hay logo, o la imagen no se puede cargar (pasa con muchas
/// direcciones de Radio Browser), muestra la inicial sobre el color de acento en lugar de un cuadro vacío.
/// </summary>
public class StationLogoView : ContentView
{
    public static readonly BindableProperty SourceProperty = BindableProperty.Create(
        nameof(Source), typeof(string), typeof(StationLogoView), null,
        propertyChanged: (view, _, _) => ((StationLogoView)view).UpdateSource());

    public static readonly BindableProperty InitialProperty = BindableProperty.Create(
        nameof(Initial), typeof(string), typeof(StationLogoView), string.Empty,
        propertyChanged: (view, _, value) => ((StationLogoView)view)._initial.Text = (string)value);

    public static readonly BindableProperty CornerRadiusProperty = BindableProperty.Create(
        nameof(CornerRadius), typeof(double), typeof(StationLogoView), 10d,
        propertyChanged: (view, _, _) => ((StationLogoView)view).UpdateShape());

    public static readonly BindableProperty InitialFontSizeProperty = BindableProperty.Create(
        nameof(InitialFontSize), typeof(double), typeof(StationLogoView), 22d,
        propertyChanged: (view, _, value) => ((StationLogoView)view)._initial.FontSize = (double)value);

    private readonly Border _avatar;
    private readonly Label _initial;
    private readonly Border _logo;
    private readonly Image _image;

    public StationLogoView()
    {
        _initial = new Label
        {
            FontSize = 22,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        _initial.SetDynamicResource(Label.TextColorProperty, "OnPrimary");

        _avatar = new Border { StrokeThickness = 0, Content = _initial };
        _avatar.SetDynamicResource(BackgroundProperty, "PrimaryBrush");

        _image = new Image { Aspect = Aspect.AspectFit };
        _image.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Image.IsLoading) && !_image.IsLoading)
                Dispatcher.Dispatch(CheckLoaded);
        };

        // Fondo blanco: la mayoría de logos están pensados para fondo claro.
        _logo = new Border
        {
            StrokeThickness = 0,
            Background = Colors.White,
            Padding = 4,
            IsVisible = false,
            Content = _image
        };

        Content = new Grid { Children = { _avatar, _logo } };
        UpdateShape();
    }

    public string? Source
    {
        get => (string?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public string Initial
    {
        get => (string)GetValue(InitialProperty);
        set => SetValue(InitialProperty, value);
    }

    public double CornerRadius
    {
        get => (double)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public double InitialFontSize
    {
        get => (double)GetValue(InitialFontSizeProperty);
        set => SetValue(InitialFontSizeProperty, value);
    }

    /// <summary>Margen interior del logo (en los tamaños grandes queda mejor con más aire).</summary>
    public Thickness LogoPadding
    {
        get => _logo.Padding;
        set => _logo.Padding = value;
    }

    private void UpdateSource()
    {
        // Radio Browser a veces da direcciones mal formadas: se tratan como si no hubiera logo.
        var hasUrl = Uri.TryCreate(Source, UriKind.Absolute, out var uri);
        // Se muestra mientras carga; si al terminar no hay imagen, CheckLoaded lo oculta.
        _logo.IsVisible = hasUrl;
        _image.Source = hasUrl ? ImageSource.FromUri(uri!) : null;
    }

    /// <summary>Si la imagen terminó de cargar pero no tiene tamaño, es que ha fallado: se deja la inicial.</summary>
    private void CheckLoaded()
    {
        var size = ((IView)_image).Measure(double.PositiveInfinity, double.PositiveInfinity);
        if (size.Width <= 0 || size.Height <= 0)
            _logo.IsVisible = false;
    }

    private void UpdateShape()
    {
        _avatar.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = CornerRadius };
        _logo.StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = CornerRadius };
    }
}

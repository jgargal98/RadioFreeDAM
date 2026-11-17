namespace RadioFreeDAM.Views.Controls;

/// <summary>
/// Three small bars that bounce like an equalizer. Marks the station that is playing in the lists.
/// It is only an animation (the real audio levels are not available); it runs only while visible.
/// </summary>
public class EqualizerView : ContentView
{
    private const double MinHeight = 4;
    private const double MaxHeight = 16;

    // Each bar has its own period so they never move in sync.
    private static readonly uint[] Periods = [520, 380, 450];

    public static readonly BindableProperty BarColorProperty = BindableProperty.Create(
        nameof(BarColor), typeof(Color), typeof(EqualizerView), Colors.White,
        propertyChanged: (view, _, _) => ((EqualizerView)view).ApplyColor());

    private readonly BoxView[] _bars;

    public EqualizerView()
    {
        _bars = Periods
            .Select(_ => new BoxView { WidthRequest = 3, HeightRequest = MinHeight, CornerRadius = 1.5, VerticalOptions = LayoutOptions.End })
            .ToArray();

        Content = new HorizontalStackLayout
        {
            Spacing = 2,
            HeightRequest = MaxHeight,
            VerticalOptions = LayoutOptions.Center,
            Children = { _bars[0], _bars[1], _bars[2] }
        };

        ApplyColor();
    }

    public Color BarColor
    {
        get => (Color)GetValue(BarColorProperty);
        set => SetValue(BarColorProperty, value);
    }

    // Start and stop with the control: a list recycles views, so animations must not outlive them.
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler is null)
            StopAnimation();
        else
            StartAnimation();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == nameof(IsVisible))
        {
            if (IsVisible && Handler is not null)
                StartAnimation();
            else
                StopAnimation();
        }
    }

    private void StartAnimation()
    {
        if (!IsVisible)
            return;

        for (var i = 0; i < _bars.Length; i++)
        {
            var bar = _bars[i];
            // |sin| makes the bar go up and down; each bar starts at a different point of its cycle.
            var offset = i * 0.27;
            var animation = new Animation(
                t => bar.HeightRequest = MinHeight + (MaxHeight - MinHeight) * Math.Abs(Math.Sin(t * Math.PI)),
                offset, offset + 1);
            animation.Commit(this, $"bar{i}", 16, Periods[i], Easing.Linear, repeat: () => true);
        }
    }

    private void StopAnimation()
    {
        for (var i = 0; i < _bars.Length; i++)
            this.AbortAnimation($"bar{i}");
    }

    private void ApplyColor()
    {
        foreach (var bar in _bars)
            bar.Color = BarColor;
    }
}

namespace RadioFreeDAM.Views.Controls;

/// <summary>
/// Ondas de sonido decorativas para el fondo del Reproductor.
/// Mientras <see cref="IsAnimating"/> es true se desplazan y crecen; al pararse se aplanan poco a poco.
/// Es una animación, no el volumen real: MediaElement no da acceso a los datos del audio.
/// </summary>
public class SoundWavesView : GraphicsView, IDrawable
{
    public static readonly BindableProperty WaveColorProperty = BindableProperty.Create(
        nameof(WaveColor), typeof(Color), typeof(SoundWavesView), Colors.Gray,
        propertyChanged: (view, _, _) => ((SoundWavesView)view).Invalidate());

    public static readonly BindableProperty IsAnimatingProperty = BindableProperty.Create(
        nameof(IsAnimating), typeof(bool), typeof(SoundWavesView), false,
        propertyChanged: (view, _, _) => ((SoundWavesView)view).UpdateTimer());

    // Altura de las ondas: 1 = sonando, valor pequeño = casi planas (en reposo).
    private const float PlayingAmplitude = 1f;
    private const float IdleAmplitude = 0.12f;

    private readonly IDispatcherTimer? _timer;
    private float _phase;
    private float _amplitude = IdleAmplitude;

    public SoundWavesView()
    {
        Drawable = this;
        InputTransparent = true;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(33); // ~30 imágenes por segundo
        _timer.Tick += (_, _) => Step();

        // No animar mientras la pantalla no se ve (ahorra batería).
        Loaded += (_, _) => UpdateTimer();
        Unloaded += (_, _) => _timer.Stop();
    }

    public Color WaveColor
    {
        get => (Color)GetValue(WaveColorProperty);
        set => SetValue(WaveColorProperty, value);
    }

    public bool IsAnimating
    {
        get => (bool)GetValue(IsAnimatingProperty);
        set => SetValue(IsAnimatingProperty, value);
    }

    private float TargetAmplitude => IsAnimating ? PlayingAmplitude : IdleAmplitude;

    private void UpdateTimer()
    {
        if (_timer is null)
            return;

        // Sigue corriendo hasta llegar a la altura objetivo, para que el cambio sea suave.
        if (IsLoaded && (IsAnimating || Math.Abs(_amplitude - TargetAmplitude) > 0.01f))
            _timer.Start();
        else
            _timer.Stop();
    }

    private void Step()
    {
        _amplitude += (TargetAmplitude - _amplitude) * 0.08f;
        if (IsAnimating)
            _phase += 0.09f;

        Invalidate();
        UpdateTimer();
    }

    public void Draw(ICanvas canvas, RectF rect)
    {
        var middle = rect.Height / 2;
        var maxHeight = rect.Height * 0.32f;

        // Tres ondas con distinta frecuencia, velocidad y transparencia.
        DrawWave(canvas, rect, middle, maxHeight, frequency: 1.6f, speed: 1.0f, offset: 0f, alpha: 1f, width: 3f);
        DrawWave(canvas, rect, middle, maxHeight * 0.75f, frequency: 2.3f, speed: -1.3f, offset: 1.7f, alpha: 0.65f, width: 2.5f);
        DrawWave(canvas, rect, middle, maxHeight * 0.55f, frequency: 3.1f, speed: 1.7f, offset: 3.4f, alpha: 0.4f, width: 2f);
    }

    private void DrawWave(ICanvas canvas, RectF rect, float middle, float height,
        float frequency, float speed, float offset, float alpha, float width)
    {
        var path = new PathF();
        const int steps = 60;

        for (var i = 0; i <= steps; i++)
        {
            var x = rect.Width * i / steps;
            var t = (float)i / steps;
            // Más altas en el centro y planas en los bordes, como la forma de un sonido.
            var envelope = MathF.Sin(MathF.PI * t);
            var y = middle + MathF.Sin(t * frequency * MathF.Tau + _phase * speed + offset) * height * envelope * _amplitude;

            if (i == 0)
                path.MoveTo(x, y);
            else
                path.LineTo(x, y);
        }

        canvas.StrokeColor = WaveColor.WithAlpha(WaveColor.Alpha * alpha);
        canvas.StrokeSize = width;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.DrawPath(path);
    }
}

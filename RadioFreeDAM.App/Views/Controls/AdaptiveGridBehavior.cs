namespace RadioFreeDAM.Views.Controls;

/// <summary>
/// Sets the number of columns of a grid CollectionView from its width, so the cards never get too narrow.
/// A fixed number per device does not work: on a computer the width depends on the window and on the side panel.
/// </summary>
public class AdaptiveGridBehavior : Behavior<CollectionView>
{
    /// <summary>Narrowest a card is allowed to be, in device-independent units.</summary>
    public double MinItemWidth { get; set; } = 190;

    /// <summary>The grid never has fewer columns than this (a phone shows two).</summary>
    public int MinSpan { get; set; } = 2;

    protected override void OnAttachedTo(CollectionView view)
    {
        base.OnAttachedTo(view);
        view.SizeChanged += OnSizeChanged;
    }

    protected override void OnDetachingFrom(CollectionView view)
    {
        view.SizeChanged -= OnSizeChanged;
        base.OnDetachingFrom(view);
    }

    private void OnSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not CollectionView { ItemsLayout: GridItemsLayout layout, Width: > 0 } view)
            return;

        // n cards and n-1 gaps must fit: n * min + (n - 1) * gap <= width.
        var gap = layout.HorizontalItemSpacing;
        var span = Math.Max(MinSpan, (int)((view.Width + gap) / (MinItemWidth + gap)));

        if (layout.Span != span)
            layout.Span = span;
    }
}

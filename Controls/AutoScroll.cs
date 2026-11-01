using Avalonia;
using Avalonia.Controls;

namespace OrbitalDocking.Controls;

public static class AutoScroll
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<ScrollViewer, bool>("IsEnabled", typeof(AutoScroll));

    static AutoScroll()
    {
        IsEnabledProperty.Changed.AddClassHandler<ScrollViewer>((viewer, e) =>
        {
            viewer.ScrollChanged -= OnScrollChanged;
            if (e.NewValue is true)
            {
                viewer.ScrollChanged += OnScrollChanged;
                viewer.ScrollToEnd();
            }
        });
    }

    public static bool GetIsEnabled(ScrollViewer viewer) => viewer.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(ScrollViewer viewer, bool value) => viewer.SetValue(IsEnabledProperty, value);

    private static void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer viewer && e.ExtentDelta.Y > 0)
            viewer.ScrollToEnd();
    }
}

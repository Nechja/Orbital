using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace OrbitalDocking.Controls;

/// <summary>
/// Renders a stroke-based 24x24 icon geometry (see Styles/Icons.axaml) using the inherited Foreground.
/// Usage: &lt;c:Icon Data="{StaticResource Icon.Play}"/&gt;
/// </summary>
public class Icon : TemplatedControl
{
    public static readonly StyledProperty<Geometry?> DataProperty =
        AvaloniaProperty.Register<Icon, Geometry?>(nameof(Data));

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Icon, double>(nameof(StrokeThickness), 2.0);

    public Geometry? Data
    {
        get => GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }
}

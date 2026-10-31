using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace OrbitalDocking.Converters;

/// <summary>Resolves an icon resource key (e.g. "Icon.Box") to its Geometry from application resources.</summary>
public sealed class IconKeyConverter : IValueConverter
{
    public static readonly IconKeyConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string key && Application.Current is { } app && app.TryGetResource(key, app.ActualThemeVariant, out var geometry)
            ? geometry
            : null;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

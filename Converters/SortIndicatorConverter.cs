using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace OrbitalDocking.Converters;

public sealed class SortIndicatorConverter : IMultiValueConverter
{
    public static readonly SortIndicatorConverter Instance = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2 || values[0] is not string sortColumn || values[1] is not bool descending || parameter is not string column)
            return null;
        if (!string.Equals(sortColumn, column, StringComparison.Ordinal))
            return null;

        var key = descending ? "Icon.ChevronDown" : "Icon.ChevronUp";
        return Application.Current is { } app && app.TryGetResource(key, app.ActualThemeVariant, out var geometry) ? geometry : null;
    }
}

using System;

namespace OrbitalDocking.Extensions;

public static class StackColorExtensions
{
    // Cool hues that sit with the teal/purple theme; no warm oranges or salmons.
    private static readonly string[] Colors =
    {
        "#4ECDC4", // Teal
        "#7CC4F5", // Sky
        "#B19CD9", // Lavender
        "#7FE0B8", // Mint
        "#F08CB8", // Rose
        "#8FA3F0", // Periwinkle
        "#5FD3E6", // Aqua
        "#D6A5F0"  // Lilac
    };

    public static string GetStackColor(this string? stackName, string? fallbackColor = null)
    {
        if (string.IsNullOrEmpty(stackName))
            return fallbackColor ?? "#4ECDC4";
        
        var hash = stackName.GetHashCode();
        var index = Math.Abs(hash) % Colors.Length;
        return Colors[index];
    }
}
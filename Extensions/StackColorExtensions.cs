using System;

namespace OrbitalDocking.Extensions;

public static class StackColorExtensions
{
    private static readonly string[] Colors = 
    {
        "#4ECDC4", // Teal
        "#95E1D3", // Light teal
        "#FFB347", // Orange
        "#87CEEB", // Sky blue
        "#DDA0DD", // Plum
        "#98D8C8", // Mint
        "#FFA07A", // Light salmon
        "#B19CD9"  // Light purple
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
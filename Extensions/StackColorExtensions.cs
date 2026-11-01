using System;

namespace OrbitalDocking.Extensions;

public static class StackColorExtensions
{
    private static readonly string[] Colors =
    {
        "#4ECDC4",
        "#7CC4F5",
        "#B19CD9",
        "#7FE0B8",
        "#F08CB8",
        "#8FA3F0",
        "#5FD3E6",
        "#D6A5F0"
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

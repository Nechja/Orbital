using System;

namespace OrbitalDocking.Extensions;

public static class DateTimeExtensions
{
    public static string ToRelativeTime(this DateTime dateTime, bool useFormattedDateForOld = false)
    {
        var timeSpan = DateTime.UtcNow - dateTime.ToUniversalTime();
        
        if (timeSpan.TotalMinutes < 1)
            return "just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)timeSpan.TotalDays} days ago";
        
        // For very old items, either show formatted date (containers) or relative time (images/volumes/networks)
        if (useFormattedDateForOld)
            return dateTime.ToString("MMM dd, yyyy");
            
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} months ago";
        
        return $"{(int)(timeSpan.TotalDays / 365)} years ago";
    }
}
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using OrbitalDocking.Models;

namespace OrbitalDocking.ViewModels;

public partial class ImageViewModel : ObservableObject
{
    private readonly ImageInfo _image;

    public ImageViewModel(ImageInfo image)
    {
        _image = image;
    }

    public string FullId => _image.Id;
    public string Id => _image.Id.Length > 19 ? _image.Id.Substring(7, 12) : _image.Id;
    public string Repository => string.IsNullOrEmpty(_image.Repository) || _image.Repository == "<none>" ? "unnamed" : _image.Repository;
    public string Tag => string.IsNullOrEmpty(_image.Tag) || _image.Tag == "<none>" ? "latest" : _image.Tag;
    public long Size => _image.Size;
    public DateTime Created => _image.Created;
    public string SizeFormatted => FormatSize(_image.Size);
    public string CreatedRelative => GetRelativeTime(_image.Created);

    private string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }

        return $"{size:0.##} {sizes[order]}";
    }

    private string GetRelativeTime(DateTime dateTime)
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
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} months ago";
        
        return $"{(int)(timeSpan.TotalDays / 365)} years ago";
    }
}
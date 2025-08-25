using System;
using CommunityToolkit.Mvvm.ComponentModel;
using OrbitalDocking.Extensions;
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
    public string CreatedRelative => _image.Created.ToRelativeTime();

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

}
using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OrbitalDocking.Models;

namespace OrbitalDocking.ViewModels;

public partial class VolumeViewModel : ObservableObject
{
    private readonly VolumeInfo _volume;

    public VolumeViewModel(VolumeInfo volume)
    {
        _volume = volume;
    }

    public string Name => _volume.Name;
    public string ShortName => _volume.Name.Length > 25 ? _volume.Name.Substring(0, 22) + "..." : _volume.Name;
    public string Driver => _volume.Driver;
    public string MountPoint => _volume.MountPoint;
    public DateTime Created => _volume.Created;
    public string CreatedRelative => GetRelativeTime(_volume.Created);
    public int LabelCount => _volume.Labels?.Count ?? 0;
    public string Scope => _volume.Options?.ContainsKey("scope") == true ? _volume.Options["scope"] : "local";
    
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
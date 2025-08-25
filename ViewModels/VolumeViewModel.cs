using System;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OrbitalDocking.Extensions;
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
    public string CreatedRelative => _volume.Created.ToRelativeTime();
    public int LabelCount => _volume.Labels?.Count ?? 0;
    public string Scope => _volume.Options?.ContainsKey("scope") == true ? _volume.Options["scope"] : "local";
    
}
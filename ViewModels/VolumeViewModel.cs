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
    public string Driver => _volume.Driver;
    public string MountPoint => _volume.MountPoint;
}
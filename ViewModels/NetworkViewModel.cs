using System;
using CommunityToolkit.Mvvm.ComponentModel;
using OrbitalDocking.Extensions;
using OrbitalDocking.Models;

namespace OrbitalDocking.ViewModels;

public partial class NetworkViewModel : ObservableObject
{
    private readonly NetworkInfo _network;

    public NetworkViewModel(NetworkInfo network)
    {
        _network = network;
    }

    public string Id => _network.Id;
    public string ShortId => _network.Id.Length > 12 ? _network.Id.Substring(0, 12) : _network.Id;
    public string Name => _network.Name;
    public string ShortName => _network.Name.Length > 25 ? _network.Name.Substring(0, 22) + "..." : _network.Name;
    public string Driver => _network.Driver;
    public string Scope => _network.Scope;
    public bool Internal => _network.Internal;
    public bool Attachable => _network.Attachable;
    public DateTime Created => _network.Created;
    public string CreatedRelative => _network.Created.ToRelativeTime();
    public bool IsRemovable => !IsBuiltIn;
    public bool IsBuiltIn => _network.Name == "bridge" || _network.Name == "host" || _network.Name == "none";
    
}
using System;
using CommunityToolkit.Mvvm.ComponentModel;
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
    public string CreatedRelative => GetRelativeTime(_network.Created);
    public bool IsRemovable => !IsBuiltIn;
    public bool IsBuiltIn => _network.Name == "bridge" || _network.Name == "host" || _network.Name == "none";
    
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
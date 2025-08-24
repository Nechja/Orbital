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
    public string Name => _network.Name;
    public string Driver => _network.Driver;
    public string Scope => _network.Scope;
    public bool Internal => _network.Internal;
}
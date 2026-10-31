using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ErrorOr;
using OrbitalDocking.Configuration;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels.Pages;

public sealed partial class NetworksPageViewModel : ResourcePageViewModel<NetworkViewModel>
{
    public NetworksPageViewModel(IDockerService docker, AppState state, IUiDispatcher ui)
        : base(docker, state, ui, AppConstants.Timing.NetworkRefreshInterval)
    {
    }

    public override string Title => "Networks";
    public override string IconKey => "Icon.Network";

    protected override async Task<ErrorOr<IReadOnlyList<NetworkViewModel>>> LoadAsync()
    {
        var result = await Docker.GetNetworksAsync();
        if (result.IsError) return result.Errors;
        return result.Value.OrderBy(n => n.Name).Select(n => new NetworkViewModel(n)).ToList();
    }

    protected override bool Matches(NetworkViewModel network, string search) =>
        ContainsIgnoreCase(network.Name, search) || ContainsIgnoreCase(network.Driver, search) || ContainsIgnoreCase(network.Id, search);

    [RelayCommand]
    private Task RemoveNetworkAsync(NetworkViewModel network)
    {
        if (network.IsBuiltIn)
        {
            State.StatusMessage = $"Cannot remove built-in network {network.Name}";
            return Task.CompletedTask;
        }

        return RunAsync($"Removing network {network.Name}...", $"Removed network {network.Name}",
            () => Docker.RemoveNetworkAsync(network.Id));
    }

    [RelayCommand]
    private Task PruneNetworksAsync() =>
        RunAsync("Pruning networks...", "Networks pruned", () => Docker.PruneNetworksAsync());
}

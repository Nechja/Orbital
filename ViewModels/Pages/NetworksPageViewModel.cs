using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErrorOr;
using OrbitalDocking.Configuration;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels.Pages;

public sealed partial class NetworksPageViewModel : ResourcePageViewModel<NetworkViewModel>
{
    public NetworksPageViewModel(IDockerService docker, IDialogService dialogs, AppState state, IUiDispatcher ui)
        : base(docker, dialogs, state, ui, AppConstants.Timing.NetworkRefreshInterval)
    {
    }

    public override string Title => "Networks";
    public override string IconKey => "Icon.Network";
    protected override string Noun => "network";
    protected override string DefaultSortColumn => "name";

    protected override async Task<ErrorOr<IReadOnlyList<NetworkViewModel>>> LoadAsync()
    {
        var result = await Docker.GetNetworksAsync();
        if (result.IsError) return result.Errors;
        return result.Value.Select(n => new NetworkViewModel(n)).ToList();
    }

    protected override bool Matches(NetworkViewModel network, string search) =>
        ContainsIgnoreCase(network.Name, search) || ContainsIgnoreCase(network.Driver, search) || ContainsIgnoreCase(network.Id, search);

    protected override IComparable? SortKey(NetworkViewModel network, string column) => column switch
    {
        "driver" => network.Driver,
        "scope" => network.Scope,
        "created" => network.Created,
        _ => network.Name,
    };

    protected override string Describe(NetworkViewModel network) => network.Name;

    protected override Task<ErrorOr<Success>> RemoveAsync(NetworkViewModel network) =>
        network.IsBuiltIn
            ? Task.FromResult<ErrorOr<Success>>(Error.Validation("Network.BuiltIn", $"Cannot remove built-in network {network.Name}"))
            : Docker.RemoveNetworkAsync(network.Id);

    protected override Task<ErrorOr<Success>> PruneAsync() => Docker.PruneNetworksAsync();
}

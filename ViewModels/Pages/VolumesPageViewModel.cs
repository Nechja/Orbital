using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ErrorOr;
using OrbitalDocking.Configuration;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels.Pages;

public sealed partial class VolumesPageViewModel : ResourcePageViewModel<VolumeViewModel>
{
    public VolumesPageViewModel(IDockerService docker, AppState state, IUiDispatcher ui)
        : base(docker, state, ui, AppConstants.Timing.VolumeRefreshInterval)
    {
    }

    public override string Title => "Volumes";
    public override string IconKey => "Icon.Database";

    protected override async Task<ErrorOr<IReadOnlyList<VolumeViewModel>>> LoadAsync()
    {
        var result = await Docker.GetVolumesAsync();
        if (result.IsError) return result.Errors;
        return result.Value.OrderBy(v => v.Name).Select(v => new VolumeViewModel(v)).ToList();
    }

    protected override bool Matches(VolumeViewModel volume, string search) =>
        ContainsIgnoreCase(volume.Name, search) || ContainsIgnoreCase(volume.Driver, search);

    [RelayCommand]
    private Task RemoveVolumeAsync(VolumeViewModel volume) =>
        RunAsync($"Removing volume {volume.Name}...", $"Removed volume {volume.Name}",
            () => Docker.RemoveVolumeAsync(volume.Name, force: false));

    [RelayCommand]
    private Task PruneVolumesAsync() =>
        RunAsync("Pruning volumes...", "Volumes pruned", () => Docker.PruneVolumesAsync());
}

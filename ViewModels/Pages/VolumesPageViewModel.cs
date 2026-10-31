using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ErrorOr;
using OrbitalDocking.Configuration;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels.Pages;

public sealed partial class VolumesPageViewModel : ResourcePageViewModel<VolumeViewModel>
{
    public VolumesPageViewModel(IDockerService docker, IDialogService dialogs, AppState state, IUiDispatcher ui)
        : base(docker, dialogs, state, ui, AppConstants.Timing.VolumeRefreshInterval)
    {
    }

    public override string Title => "Volumes";
    public override string IconKey => "Icon.Database";
    protected override string Noun => "volume";
    protected override string DefaultSortColumn => "name";

    protected override async Task<ErrorOr<IReadOnlyList<VolumeViewModel>>> LoadAsync()
    {
        var result = await Docker.GetVolumesAsync();
        if (result.IsError) return result.Errors;
        return result.Value.Select(v => new VolumeViewModel(v)).ToList();
    }

    protected override bool Matches(VolumeViewModel volume, string search) =>
        ContainsIgnoreCase(volume.Name, search) || ContainsIgnoreCase(volume.Driver, search) || ContainsIgnoreCase(volume.MountPoint, search);

    protected override IComparable? SortKey(VolumeViewModel volume, string column) => column switch
    {
        "driver" => volume.Driver,
        "created" => volume.Created,
        _ => volume.Name,
    };

    protected override string Describe(VolumeViewModel volume) => volume.ShortName;

    protected override Task<ErrorOr<Success>> RemoveAsync(VolumeViewModel volume) =>
        Docker.RemoveVolumeAsync(volume.Name, force: false);

    protected override Task<ErrorOr<Success>> PruneAsync() => Docker.PruneVolumesAsync();
}

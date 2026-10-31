using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using ErrorOr;
using OrbitalDocking.Configuration;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels.Pages;

public sealed partial class ImagesPageViewModel : ResourcePageViewModel<ImageViewModel>
{
    private readonly IDialogService _dialogs;

    public ImagesPageViewModel(IDockerService docker, IDialogService dialogs, AppState state, IUiDispatcher ui)
        : base(docker, state, ui, AppConstants.Timing.ImageRefreshInterval)
    {
        _dialogs = dialogs;
    }

    public override string Title => "Images";
    public override string IconKey => "Icon.Layers";

    protected override async Task<ErrorOr<IReadOnlyList<ImageViewModel>>> LoadAsync()
    {
        var result = await Docker.GetImagesAsync();
        if (result.IsError) return result.Errors;
        return result.Value.OrderBy(i => i.Repository).ThenBy(i => i.Tag).Select(i => new ImageViewModel(i)).ToList();
    }

    protected override bool Matches(ImageViewModel image, string search) =>
        ContainsIgnoreCase(image.Repository, search) || ContainsIgnoreCase(image.Tag, search) || ContainsIgnoreCase(image.Id, search);

    [RelayCommand]
    private async Task RunImageAsync(ImageViewModel image)
    {
        var created = await _dialogs.ShowCreateContainerDialogAsync(image.Repository, image.Tag);
        if (created)
            State.StatusMessage = $"Container created and started from {image.Repository}:{image.Tag}";
    }

    [RelayCommand]
    private Task RemoveImageAsync(ImageViewModel image) =>
        RunAsync($"Removing {image.Repository}:{image.Tag}...", $"Removed {image.Repository}:{image.Tag}",
            () => Docker.RemoveImageAsync(image.FullId, force: true));

    [RelayCommand]
    private Task PullImageAsync(ImageViewModel image) =>
        RunAsync($"Pulling {image.Repository}:{image.Tag}...", $"Pulled {image.Repository}:{image.Tag}",
            () => Docker.PullImageAsync($"{image.Repository}:{image.Tag}"));

    [RelayCommand]
    private Task PruneImagesAsync() =>
        RunAsync("Pruning images...", "Images pruned", () => Docker.PruneImagesAsync());
}

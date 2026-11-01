using System;
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
        : base(docker, dialogs, state, ui, AppConstants.Timing.ImageRefreshInterval)
    {
        _dialogs = dialogs;
    }

    public override string Title => "Images";
    public override string IconKey => "Icon.Layers";
    protected override string Noun => "image";
    protected override string DefaultSortColumn => "name";

    protected override async Task<ErrorOr<IReadOnlyList<ImageViewModel>>> LoadAsync()
    {
        var result = await Docker.GetImagesAsync();
        if (result.IsError) return result.Errors;
        return result.Value.Select(i => new ImageViewModel(i)).ToList();
    }

    protected override bool Matches(ImageViewModel image, string search) =>
        ContainsIgnoreCase(image.Repository, search) || ContainsIgnoreCase(image.Tag, search) || ContainsIgnoreCase(image.Id, search);

    protected override IComparable? SortKey(ImageViewModel image, string column) => column switch
    {
        "tag" => image.Tag,
        "size" => image.Size,
        "created" => image.Created,
        _ => $"{image.Repository}:{image.Tag}",
    };

    protected override string Describe(ImageViewModel image) => $"{image.Repository}:{image.Tag}";

    protected override Task<ErrorOr<Success>> RemoveAsync(ImageViewModel image) =>
        Docker.RemoveImageAsync(image.FullId, force: true);

    protected override Task<ErrorOr<Success>> PruneAsync() => Docker.PruneImagesAsync();

    [RelayCommand]
    private async Task RunImageAsync(ImageViewModel image)
    {
        var created = await _dialogs.ShowCreateContainerDialogAsync(image.Repository, image.Tag);
        if (created)
            State.StatusMessage = $"Container created and started from {Describe(image)}";
    }

    [RelayCommand]
    private Task PullImageAsync(ImageViewModel image) =>
        RunAsync($"Pulling {Describe(image)}...", $"Pulled {Describe(image)}", () => Docker.PullImageAsync(Describe(image)));
}

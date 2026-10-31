using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using OrbitalDocking.Models;
using OrbitalDocking.ViewModels.Dialogs;
using OrbitalDocking.Views.Dialogs;

namespace OrbitalDocking.Services;

public sealed class DialogService : IDialogService
{
    private readonly IDockerService _docker;

    public DialogService(IDockerService docker)
    {
        _docker = docker;
    }

    public Task<bool> ShowConfirmationAsync(string title, string message) =>
        new ConfirmationDialog { Title = title, Message = message }.ShowDialog<bool>(Owner);

    public Task<VolumeRemovalChoice> ShowVolumeRemovalDialogAsync(string subject, IReadOnlyList<VolumeMount> volumes)
    {
        var dialog = new VolumeRemovalDialog
        {
            ContainerName = subject,
            VolumeNames = volumes.Select(v => v.Name).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList()
        };
        return dialog.ShowDialog<VolumeRemovalChoice>(Owner);
    }

    public async Task<bool> ShowCreateContainerDialogAsync(string repository, string tag)
    {
        var viewModel = new CreateContainerDialogViewModel(_docker, this, repository, tag);
        var dialog = new CreateContainerDialog { DataContext = viewModel };
        await dialog.ShowDialog(Owner);
        return viewModel.DialogResult == true;
    }

    private static Window Owner =>
        (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow
        ?? throw new InvalidOperationException("No main window to own the dialog");
}

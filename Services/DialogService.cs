using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using OrbitalDocking.Models;
using OrbitalDocking.ViewModels;
using OrbitalDocking.Views;
using OrbitalDocking.Views.Dialogs;

namespace OrbitalDocking.Services;

public class DialogService(Func<string, string, LogsViewModel> logsViewModelFactory) : IDialogService
{
    public void ShowLogsWindow(string containerId, string containerName, Window owner)
    {
        var logsViewModel = logsViewModelFactory(containerId, containerName);
        var logsWindow = new LogsWindow(logsViewModel)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        logsWindow.Show(owner);
    }
    
    public async Task<bool> ShowConfirmationAsync(string title, string message, Window owner)
    {
        var dialog = new ConfirmationDialog
        {
            Title = title,
            Message = message
        };
        
        var result = await dialog.ShowDialog<bool>(owner);
        return result;
    }
    
    public async Task<VolumeRemovalChoice> ShowVolumeRemovalDialogAsync(string containerName, List<VolumeMount> volumes, Window owner)
    {
        var namedVolumes = volumes
            .Where(v => !string.IsNullOrEmpty(v.Name))
            .Select(v => v.Name)
            .Distinct()
            .ToList();

        var dialog = new VolumeRemovalDialog
        {
            ContainerName = containerName,
            VolumeNames = namedVolumes
        };
        
        var result = await dialog.ShowDialog<VolumeRemovalChoice>(owner);
        return result;
    }
}
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using OrbitalDocking.ViewModels;
using OrbitalDocking.Views;

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
        // TODO return true, implement proper dialog later
        await Task.CompletedTask;
        return true;
    }
}
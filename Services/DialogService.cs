using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using OrbitalDocking.ViewModels;
using OrbitalDocking.Views;

namespace OrbitalDocking.Services;

public class DialogService : IDialogService
{
    private readonly Func<string, string, LogsViewModel> _logsViewModelFactory;
    
    public DialogService(Func<string, string, LogsViewModel> logsViewModelFactory)
    {
        _logsViewModelFactory = logsViewModelFactory;
    }
    
    public void ShowLogsWindow(string containerId, string containerName, Window owner)
    {
        var logsViewModel = _logsViewModelFactory(containerId, containerName);
        var logsWindow = new LogsWindow(logsViewModel)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        logsWindow.Show(owner);
    }
    
    public async Task<bool> ShowConfirmationAsync(string title, string message, Window owner)
    {
        // For now, return true. We'll implement proper dialog later
        await Task.CompletedTask;
        return true;
    }
}
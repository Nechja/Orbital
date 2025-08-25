using System.Threading.Tasks;
using Avalonia.Controls;

namespace OrbitalDocking.Services;

public interface IDialogService
{
    void ShowLogsWindow(string containerId, string containerName, Window owner);
    Task<bool> ShowConfirmationAsync(string title, string message, Window owner);
}
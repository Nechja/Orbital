using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using OrbitalDocking.Models;

namespace OrbitalDocking.Services;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message, Window owner);
    Task<VolumeRemovalChoice> ShowVolumeRemovalDialogAsync(string containerName, List<VolumeMount> volumes, Window owner);
}

public enum VolumeRemovalChoice
{
    Cancel,
    RemoveContainerOnly,
    RemoveContainerAndVolumes
}
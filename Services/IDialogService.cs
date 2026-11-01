using System.Collections.Generic;
using System.Threading.Tasks;
using OrbitalDocking.Models;

namespace OrbitalDocking.Services;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task<VolumeRemovalChoice> ShowVolumeRemovalDialogAsync(string subject, IReadOnlyList<VolumeMount> volumes);
    Task<bool> ShowCreateContainerDialogAsync(string repository, string tag);
}

public enum VolumeRemovalChoice
{
    Cancel,
    RemoveContainerOnly,
    RemoveContainerAndVolumes
}

using System.Collections.Generic;
using System.Threading.Tasks;
using OrbitalDocking.Models;

namespace OrbitalDocking.Services;

public interface IDialogService
{
    Task<bool> ShowConfirmationAsync(string title, string message);
    Task<VolumeRemovalChoice> ShowVolumeRemovalDialogAsync(string subject, IReadOnlyList<VolumeMount> volumes);
    /// <summary>Shows the create-container dialog for an image. Returns true if a container was created.</summary>
    Task<bool> ShowCreateContainerDialogAsync(string repository, string tag);
}

public enum VolumeRemovalChoice
{
    Cancel,
    RemoveContainerOnly,
    RemoveContainerAndVolumes
}

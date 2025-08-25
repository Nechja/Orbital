using Docker.DotNet.Models;
using OrbitalDocking.Models;

namespace OrbitalDocking.Services;

public interface IDockerMapper
{
    ContainerInfo MapToContainerInfo(ContainerListResponse container);
    ContainerInfo MapToContainerInfo(ContainerInspectResponse container);
    ImageInfo MapToImageInfo(ImagesListResponse image);
    VolumeInfo MapToVolumeInfo(VolumeResponse volume);
    NetworkInfo MapToNetworkInfo(NetworkResponse network);
}
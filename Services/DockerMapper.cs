using System;
using System.Collections.Generic;
using System.Linq;
using Docker.DotNet.Models;
using OrbitalDocking.Models;

namespace OrbitalDocking.Services;

public class DockerMapper : IDockerMapper
{
    public ContainerInfo MapToContainerInfo(ContainerListResponse container)
    {
        return new ContainerInfo(
            Id: container.ID,
            Name: container.Names.FirstOrDefault()?.TrimStart('/') ?? string.Empty,
            Image: container.Image,
            State: ParseContainerState(container.State),
            Created: container.Created,
            Status: container.Status,
            Labels: container.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
            Ports: container.Ports?.Select(p => new PortMapping(
                p.PrivatePort.ToString(),
                p.PublicPort.ToString(),
                p.Type,
                p.IP ?? string.Empty)).ToList() ?? new List<PortMapping>());
    }

    public ImageInfo MapToImageInfo(ImagesListResponse image)
    {
        return new ImageInfo(
            Id: image.ID,
            Repository: image.RepoTags?.FirstOrDefault()?.Split(':')[0] ?? "<none>",
            Tag: image.RepoTags?.FirstOrDefault()?.Split(':').LastOrDefault() ?? "<none>",
            Size: image.Size,
            Created: image.Created,
            Architecture: string.Empty,
            OS: string.Empty,
            RepoTags: image.RepoTags?.ToList() ?? new List<string>(),
            Labels: image.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>());
    }

    public ContainerInfo MapToContainerInfo(ContainerInspectResponse container)
    {
        return new ContainerInfo(
            Id: container.ID,
            Name: container.Name.TrimStart('/'),
            Image: container.Image,
            State: ParseContainerState(container.State.Status),
            Created: container.Created,
            Status: container.State.Status,
            Labels: container.Config.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
            Ports: new List<PortMapping>());
    }

    public VolumeInfo MapToVolumeInfo(VolumeResponse volume)
    {
        return new VolumeInfo(
            Name: volume.Name,
            Driver: volume.Driver,
            MountPoint: volume.Mountpoint,
            Created: DateTime.TryParse(volume.CreatedAt, out var created) ? created : DateTime.MinValue,
            Labels: volume.Labels?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>(),
            Options: volume.Options?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>());
    }

    public NetworkInfo MapToNetworkInfo(NetworkResponse network)
    {
        return new NetworkInfo(
            Id: network.ID,
            Name: network.Name,
            Driver: network.Driver,
            Scope: network.Scope,
            Internal: network.Internal,
            Attachable: network.Attachable,
            Created: network.Created,
            Options: network.Options?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, string>());
    }

    private Models.ContainerState ParseContainerState(string state)
    {
        return state?.ToLowerInvariant() switch
        {
            "running" => Models.ContainerState.Running,
            "paused" => Models.ContainerState.Paused,
            "exited" => Models.ContainerState.Exited,
            "created" => Models.ContainerState.Created,
            "restarting" => Models.ContainerState.Restarting,
            "dead" => Models.ContainerState.Dead,
            "removing" => Models.ContainerState.Removing,
            _ => Models.ContainerState.Exited
        };
    }
}
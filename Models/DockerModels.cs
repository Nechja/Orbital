using System;
using System.Collections.Generic;

namespace OrbitalDocking.Models;

public record ContainerInfo(
    string Id,
    string Name,
    string Image,
    ContainerState State,
    DateTime Created,
    string Status,
    Dictionary<string, string> Labels,
    List<PortMapping> Ports,
    List<VolumeMount>? Volumes = null);

public record VolumeMount(
    string Name,
    string Source,
    string Destination,
    bool ReadWrite,
    string Driver);

public record struct PortMapping(
    string PrivatePort,
    string PublicPort,
    string Type,
    string IP);

public record ImageInfo(
    string Id,
    string Repository,
    string Tag,
    long Size,
    DateTime Created,
    string Architecture,
    string OS,
    List<string> RepoTags,
    Dictionary<string, string> Labels);

public record VolumeInfo(
    string Name,
    string Driver,
    string MountPoint,
    DateTime Created,
    Dictionary<string, string> Labels,
    Dictionary<string, string> Options);

public record NetworkInfo(
    string Id,
    string Name,
    string Driver,
    string Scope,
    bool Internal,
    bool Attachable,
    DateTime Created,
    Dictionary<string, string> Options);

public record struct ContainerStats(
    double CpuPercent,
    long MemoryUsage,
    long MemoryLimit,
    double MemoryPercent,
    long NetworkRx,
    long NetworkTx,
    long BlockRead,
    long BlockWrite,
    DateTime Timestamp);

public record ComposeProject(
    string Name,
    string Path,
    List<string> Services,
    Dictionary<string, ContainerInfo> Containers);

public record StackInfo(
    string Name,
    List<ContainerInfo> Containers,
    int RunningCount,
    int StoppedCount,
    int PausedCount)
{
    public bool AllRunning => RunningCount == Containers.Count && RunningCount > 0;
    public bool AllStopped => StoppedCount == Containers.Count && StoppedCount > 0;
    public bool Mixed => !AllRunning && !AllStopped;
    public string CollectiveState => AllRunning ? "Running" : AllStopped ? "Stopped" : "Mixed";
}

public record DockerSystemInfo(
    string ServerVersion,
    string ApiVersion,
    string OS,
    string Architecture,
    int Containers,
    int ContainersRunning,
    int ContainersPaused,
    int ContainersStopped,
    int Images,
    long MemoryTotal,
    string Driver);

public record struct ResourceUsage(
    double CpuUsagePercent,
    long MemoryUsageMB,
    long MemoryTotalMB,
    long DiskUsageGB,
    long DiskTotalGB);

public record LogEntry(
    DateTime Timestamp,
    string Message,
    LogLevel Level,
    string Stream);

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error,
    Fatal
}
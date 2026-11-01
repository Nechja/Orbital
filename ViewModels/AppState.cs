using CommunityToolkit.Mvvm.ComponentModel;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels;

public enum DockerConnection { Connecting, Connected, Disconnected }

public partial class AppState : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _dockerVersion = "Connecting...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDockerConnected))]
    [NotifyPropertyChangedFor(nameof(IsDockerConnecting))]
    private DockerConnection _dockerConnection = DockerConnection.Connecting;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpdateAvailable))]
    [NotifyPropertyChangedFor(nameof(UpdateLabel))]
    private UpdateCheckResult? _latestUpdate;

    public bool IsUpdateAvailable => LatestUpdate?.IsUpdateAvailable == true;
    public string UpdateLabel => LatestUpdate is { IsUpdateAvailable: true, Latest: { } v } ? $"v{v} available" : string.Empty;

    public bool IsDockerConnected => DockerConnection == DockerConnection.Connected;
    public bool IsDockerConnecting => DockerConnection == DockerConnection.Connecting;

    public void SetConnected(string serverVersion)
    {
        DockerVersion = $"v{serverVersion}";
        DockerConnection = DockerConnection.Connected;
    }

    public void SetDisconnected()
    {
        DockerVersion = "Disconnected";
        DockerConnection = DockerConnection.Disconnected;
    }
}

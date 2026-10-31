using CommunityToolkit.Mvvm.ComponentModel;

namespace OrbitalDocking.ViewModels;

public enum DockerConnection { Connecting, Connected, Disconnected }

/// <summary>
/// Cross-page state shown in the shell: the status bar message and Docker daemon connectivity.
/// Registered as a singleton; pages write to it, the main window binds to it.
/// </summary>
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

using CommunityToolkit.Mvvm.ComponentModel;

namespace OrbitalDocking.ViewModels.Dialogs;

public partial class PortMappingViewModel : ObservableObject
{
    [ObservableProperty]
    private string _hostPort = "";

    [ObservableProperty]
    private string _containerPort = "";

    [ObservableProperty]
    private string _protocol = "tcp";

    public PortMappingViewModel()
    {
    }

    public PortMappingViewModel(string containerPort, string protocol = "tcp")
    {
        ContainerPort = containerPort;
        Protocol = protocol;
        // Auto-suggest same port for host
        HostPort = containerPort;
    }
}
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using OrbitalDocking.Models;

namespace OrbitalDocking.ViewModels;

public partial class StackViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isExpanded = true;
    
    [ObservableProperty]
    private ObservableCollection<ContainerViewModel> _containers = new();
    
    public string Name { get; }
    public string Color { get; }
    
    public StackViewModel(string name)
    {
        Name = name;
        Color = GetStackColor(name);
    }
    
    public int ContainerCount => Containers.Count;
    public int RunningCount => Containers.Count(c => c.State == ContainerState.Running);
    public int StoppedCount => Containers.Count(c => c.State == ContainerState.Exited || c.State == ContainerState.Created);
    public int PausedCount => Containers.Count(c => c.State == ContainerState.Paused);
    
    public bool AllRunning => RunningCount == ContainerCount && ContainerCount > 0;
    public bool AllStopped => StoppedCount == ContainerCount && ContainerCount > 0;
    public bool Mixed => !AllRunning && !AllStopped;
    
    public string CollectiveState => AllRunning ? "All Running" : AllStopped ? "All Stopped" : "Mixed";
    
    public string StateColor => AllRunning ? "#4ECDC4" : AllStopped ? "#666666" : "#FFB347";
    
    private string GetStackColor(string stackName)
    {
        var colors = new[]
        {
            "#4ECDC4", // Teal
            "#95E1D3", // Light teal
            "#FFB347", // Orange
            "#87CEEB", // Sky blue
            "#DDA0DD", // Plum
            "#98D8C8", // Mint
            "#FFA07A", // Light salmon
            "#B19CD9"  // Light purple
        };
        
        var hash = stackName.GetHashCode();
        var index = System.Math.Abs(hash) % colors.Length;
        return colors[index];
    }
}
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrbitalDocking.Extensions;
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
        Color = name.GetStackColor();
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
    
    public string ExpandSymbol => IsExpanded ? "−" : "+";
    
    [RelayCommand]
    private void ToggleExpand()
    {
        IsExpanded = !IsExpanded;
    }
    
}
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OrbitalDocking.Extensions;
using OrbitalDocking.Models;

namespace OrbitalDocking.ViewModels;

public partial class StackViewModel : ObservableObject
{
    public StackViewModel(string name)
    {
        Name = name;
        Color = name.GetStackColor();
    }

    public string Name { get; }

    /// <summary>A stable hue derived from the stack name, used to color its header.</summary>
    public string Color { get; }

    [ObservableProperty]
    private bool _isExpanded = true;

    public ObservableCollection<ContainerViewModel> Containers { get; } = new();

    public int ContainerCount => Containers.Count;
    public int RunningCount => Containers.Count(c => c.IsRunning);
    public int StoppedCount => Containers.Count(c => c.IsStopped);

    public bool AllRunning => RunningCount == ContainerCount && ContainerCount > 0;
    public bool AllStopped => StoppedCount == ContainerCount && ContainerCount > 0;
    public string CollectiveState => AllRunning ? "all running" : AllStopped ? "all stopped" : "mixed";

    /// <summary>Call after the Containers collection or their states change.</summary>
    public void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(ContainerCount));
        OnPropertyChanged(nameof(RunningCount));
        OnPropertyChanged(nameof(StoppedCount));
        OnPropertyChanged(nameof(AllRunning));
        OnPropertyChanged(nameof(AllStopped));
        OnPropertyChanged(nameof(CollectiveState));
    }

    [RelayCommand]
    private void ToggleExpand() => IsExpanded = !IsExpanded;
}

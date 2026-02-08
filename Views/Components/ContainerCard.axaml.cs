using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using OrbitalDocking.ViewModels;

namespace OrbitalDocking.Views.Components;

public partial class ContainerCard : UserControl
{
    public ContainerCard()
    {
        InitializeComponent();

        // Monitor size changes for responsive layout
        PropertyChanged += OnCardPropertyChanged;
    }

    private void OnCardPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(Bounds) && DataContext is ContainerViewModel vm)
        {
            var width = Bounds.Width;
            vm.IsCompactMode = width < 700; // Breakpoint at 700px
        }
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Wire up commands to the MainWindowViewModel
        if (DataContext is ContainerViewModel container)
        {
            var mainWindow = this.FindAncestorOfType<Window>();
            if (mainWindow?.DataContext is MainWindowViewModel mainVm)
            {
                var startBtn = this.FindControl<Button>("StartButton");
                var stopBtn = this.FindControl<Button>("StopButton");
                var restartBtn = this.FindControl<Button>("RestartButton");
                var removeBtn = this.FindControl<Button>("RemoveButton");
                var logsBtn = this.FindControl<Button>("LogsButton");
                var expandBtn = this.FindControl<Button>("ExpandButton");
                var toggleExpandBtn = this.FindControl<ToggleButton>("ToggleExpandButton");

                // Compact menu items
                var menuStartBtn = this.FindControl<MenuItem>("MenuStartButton");
                var menuStopBtn = this.FindControl<MenuItem>("MenuStopButton");
                var menuRestartBtn = this.FindControl<MenuItem>("MenuRestartButton");
                var menuRemoveBtn = this.FindControl<MenuItem>("MenuRemoveButton");
                var menuLogsBtn = this.FindControl<MenuItem>("MenuLogsButton");
                var compactToggleExpandBtn = this.FindControl<ToggleButton>("CompactToggleExpandButton");
                
                if (startBtn != null)
                {
                    startBtn.Command = mainVm.StartContainerCommand;
                    startBtn.CommandParameter = container;
                }
                
                if (stopBtn != null)
                {
                    stopBtn.Command = mainVm.StopContainerCommand;
                    stopBtn.CommandParameter = container;
                }
                
                if (restartBtn != null)
                {
                    restartBtn.Command = mainVm.RestartContainerCommand;
                    restartBtn.CommandParameter = container;
                }
                
                if (removeBtn != null)
                {
                    removeBtn.Command = mainVm.RemoveContainerCommand;
                    removeBtn.CommandParameter = container;
                }
                
                if (logsBtn != null)
                {
                    logsBtn.Command = mainVm.ShowContainerLogsCommand;
                    logsBtn.CommandParameter = container;
                }
                
                if (expandBtn != null)
                {
                    expandBtn.Click += (s, e) =>
                    {
                        container.IsExpanded = !container.IsExpanded;
                        if (container.IsExpanded && container.IsRunning)
                        {
                            container.StartStatsMonitoring();
                        }
                        else
                        {
                            container.StopStatsMonitoring();
                        }
                    };
                }
                
                if (toggleExpandBtn != null)
                {
                    toggleExpandBtn.Click += (s, e) =>
                    {
                        if (container.IsExpanded && container.IsRunning)
                        {
                            container.StartStatsMonitoring();
                        }
                        else
                        {
                            container.StopStatsMonitoring();
                        }
                    };
                }

                // Wire up compact menu items
                if (menuStartBtn != null)
                {
                    menuStartBtn.Command = mainVm.StartContainerCommand;
                    menuStartBtn.CommandParameter = container;
                }

                if (menuStopBtn != null)
                {
                    menuStopBtn.Command = mainVm.StopContainerCommand;
                    menuStopBtn.CommandParameter = container;
                }

                if (menuRestartBtn != null)
                {
                    menuRestartBtn.Command = mainVm.RestartContainerCommand;
                    menuRestartBtn.CommandParameter = container;
                }

                if (menuRemoveBtn != null)
                {
                    menuRemoveBtn.Command = mainVm.RemoveContainerCommand;
                    menuRemoveBtn.CommandParameter = container;
                }

                if (menuLogsBtn != null)
                {
                    menuLogsBtn.Command = mainVm.ShowContainerLogsCommand;
                    menuLogsBtn.CommandParameter = container;
                }

                if (compactToggleExpandBtn != null)
                {
                    compactToggleExpandBtn.Click += (s, e) =>
                    {
                        if (container.IsExpanded && container.IsRunning)
                        {
                            container.StartStatsMonitoring();
                        }
                        else
                        {
                            container.StopStatsMonitoring();
                        }
                    };
                }
            }
        }
    }
}
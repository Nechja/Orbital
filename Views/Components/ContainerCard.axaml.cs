using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using OrbitalDocking.ViewModels;

namespace OrbitalDocking.Views.Components;

public partial class ContainerCard : UserControl
{
    public ContainerCard()
    {
        InitializeComponent();
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
                var logsBtn = this.FindControl<Button>("LogsButton");
                var expandBtn = this.FindControl<Button>("ExpandButton");
                
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
            }
        }
    }
}
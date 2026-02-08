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
        PropertyChanged += OnCardPropertyChanged;
    }

    private void OnCardPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(Bounds) && DataContext is ContainerViewModel vm)
            vm.IsCompactMode = Bounds.Width < 700;
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (DataContext is not ContainerViewModel container) return;

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

            var menuStartBtn = this.FindControl<MenuItem>("MenuStartButton");
            var menuStopBtn = this.FindControl<MenuItem>("MenuStopButton");
            var menuRestartBtn = this.FindControl<MenuItem>("MenuRestartButton");
            var menuRemoveBtn = this.FindControl<MenuItem>("MenuRemoveButton");
            var menuLogsBtn = this.FindControl<MenuItem>("MenuLogsButton");
            var compactToggleExpandBtn = this.FindControl<ToggleButton>("CompactToggleExpandButton");

            if (startBtn is not null)
            {
                startBtn.Command = mainVm.StartContainerCommand;
                startBtn.CommandParameter = container;
            }

            if (stopBtn is not null)
            {
                stopBtn.Command = mainVm.StopContainerCommand;
                stopBtn.CommandParameter = container;
            }

            if (restartBtn is not null)
            {
                restartBtn.Command = mainVm.RestartContainerCommand;
                restartBtn.CommandParameter = container;
            }

            if (removeBtn is not null)
            {
                removeBtn.Command = mainVm.RemoveContainerCommand;
                removeBtn.CommandParameter = container;
            }

            if (logsBtn is not null)
            {
                logsBtn.Command = mainVm.ShowContainerLogsCommand;
                logsBtn.CommandParameter = container;
            }

            if (expandBtn is not null)
            {
                expandBtn.Click += (s, e) =>
                {
                    container.IsExpanded = !container.IsExpanded;
                    if (container.IsExpanded && container.IsRunning)
                        container.StartStatsMonitoring();
                    else
                        container.StopStatsMonitoring();
                };
            }

            if (toggleExpandBtn is not null)
            {
                toggleExpandBtn.Click += (s, e) =>
                {
                    if (container.IsExpanded && container.IsRunning)
                        container.StartStatsMonitoring();
                    else
                        container.StopStatsMonitoring();
                };
            }

            if (menuStartBtn is not null)
            {
                menuStartBtn.Command = mainVm.StartContainerCommand;
                menuStartBtn.CommandParameter = container;
            }

            if (menuStopBtn is not null)
            {
                menuStopBtn.Command = mainVm.StopContainerCommand;
                menuStopBtn.CommandParameter = container;
            }

            if (menuRestartBtn is not null)
            {
                menuRestartBtn.Command = mainVm.RestartContainerCommand;
                menuRestartBtn.CommandParameter = container;
            }

            if (menuRemoveBtn is not null)
            {
                menuRemoveBtn.Command = mainVm.RemoveContainerCommand;
                menuRemoveBtn.CommandParameter = container;
            }

            if (menuLogsBtn is not null)
            {
                menuLogsBtn.Command = mainVm.ShowContainerLogsCommand;
                menuLogsBtn.CommandParameter = container;
            }

            if (compactToggleExpandBtn is not null)
            {
                compactToggleExpandBtn.Click += (s, e) =>
                {
                    if (container.IsExpanded && container.IsRunning)
                        container.StartStatsMonitoring();
                    else
                        container.StopStatsMonitoring();
                };
            }
        }
    }
}
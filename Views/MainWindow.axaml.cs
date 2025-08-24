using Avalonia.Controls;
using Avalonia.Interactivity;
using OrbitalDocking.ViewModels;

namespace OrbitalDocking.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnContainerClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ContainerViewModel container)
        {
            container.ToggleExpanded();
        }
    }

    private async void OnStopClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true; // Prevent bubble to container click
        if (sender is Button button && button.DataContext is ContainerViewModel container)
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm != null)
            {
                vm.SelectedContainer = container;
                await vm.StopContainerCommand.ExecuteAsync(null);
            }
        }
    }

    private async void OnStartClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button && button.DataContext is ContainerViewModel container)
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm != null)
            {
                vm.SelectedContainer = container;
                await vm.StartContainerCommand.ExecuteAsync(null);
            }
        }
    }

    private async void OnRestartClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button && button.DataContext is ContainerViewModel container)
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm != null)
            {
                vm.SelectedContainer = container;
                await vm.RestartContainerCommand.ExecuteAsync(null);
            }
        }
    }

    private async void OnRemoveClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button && button.DataContext is ContainerViewModel container)
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm != null)
            {
                vm.SelectedContainer = container;
                await vm.RemoveContainerCommand.ExecuteAsync(null);
            }
        }
    }

    private void OnViewLogsClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button && button.DataContext is ContainerViewModel container)
        {
            var logsWindow = new LogsWindow(container.Id, container.Name)
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            logsWindow.Show(this);
        }
    }

    private async void OnImageRemoveClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button && button.DataContext is ImageViewModel image)
        {
            var vm = DataContext as MainWindowViewModel;
            if (vm != null)
            {
                await vm.RemoveImageCommand.ExecuteAsync(image);
            }
        }
    }
}
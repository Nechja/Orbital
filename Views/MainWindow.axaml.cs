using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OrbitalDocking.Services;
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
            // Get the LogsViewModel factory from DI
            var logsViewModelFactory = ServiceLocator.GetService<Func<string, string, LogsViewModel>>();
            var logsViewModel = logsViewModelFactory(container.Id, container.Name);
            
            var logsWindow = new LogsWindow(logsViewModel)
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
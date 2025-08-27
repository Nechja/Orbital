using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using OrbitalDocking.ViewModels;

namespace OrbitalDocking.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Set the window icon
        var assets = AssetLoader.Open(new Uri("avares://OrbitalDocking/Assets/orbital.ico"));
        Icon = new WindowIcon(assets);
        
        // Set the MainWindow reference when DataContext is set
        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.MainWindow = this;
            }
        };
        
        // Handle window closing for tray
        Closing += OnWindowClosing;
    }
    
    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // If we have a tray icon, minimize to tray instead of closing
        if (DataContext is MainWindowViewModel vm && vm.TrayService != null)
        {
            e.Cancel = true;
            Hide();
        }
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
        try
        {
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping container: {ex.Message}");
        }
    }

    private async void OnStartClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        try
        {
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error starting container: {ex.Message}");
        }
    }

    private async void OnRestartClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        try
        {
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error restarting container: {ex.Message}");
        }
    }

    private async void OnRemoveClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        try
        {
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing container: {ex.Message}");
        }
    }


    private async void OnImageRemoveClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        try
        {
            if (sender is Button button && button.DataContext is ImageViewModel image)
            {
                var vm = DataContext as MainWindowViewModel;
                if (vm != null)
                {
                    await vm.RemoveImageCommand.ExecuteAsync(image);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing image: {ex.Message}");
        }
    }
}
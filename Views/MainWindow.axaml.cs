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

        var assets = AssetLoader.Open(new Uri("avares://OrbitalDocking/Assets/orbital.ico"));
        Icon = new WindowIcon(assets);

        DataContextChanged += (_, _) =>
        {
            if (DataContext is MainWindowViewModel vm)
                vm.MainWindow = this;
        };

        Closing += OnWindowClosing;
        PropertyChanged += OnWindowPropertyChanged;
    }

    private void OnWindowPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(Bounds) && DataContext is MainWindowViewModel vm)
            vm.IsSmallScreen = Bounds.Width < 1000;
    }
    
    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm && vm.TrayService is not null)
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
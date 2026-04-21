using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.Input;
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

    private void OnStopClick(object? sender, RoutedEventArgs e) =>
        InvokeContainerCommand(sender, e, vm => vm.StopContainerCommand);

    private void OnStartClick(object? sender, RoutedEventArgs e) =>
        InvokeContainerCommand(sender, e, vm => vm.StartContainerCommand);

    private void OnRestartClick(object? sender, RoutedEventArgs e) =>
        InvokeContainerCommand(sender, e, vm => vm.RestartContainerCommand);

    private void OnRemoveClick(object? sender, RoutedEventArgs e) =>
        InvokeContainerCommand(sender, e, vm => vm.RemoveContainerCommand);

    private void OnImageRemoveClick(object? sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is not Button { DataContext: ImageViewModel image }) return;
        if (DataContext is not MainWindowViewModel vm) return;

        _ = ExecuteAsync(vm, () => vm.RemoveImageCommand.ExecuteAsync(image));
    }

    private void InvokeContainerCommand(
        object? sender,
        RoutedEventArgs e,
        Func<MainWindowViewModel, IAsyncRelayCommand> commandSelector)
    {
        e.Handled = true;
        if (sender is not Button { DataContext: ContainerViewModel container }) return;
        if (DataContext is not MainWindowViewModel vm) return;

        vm.SelectedContainer = container;
        _ = ExecuteAsync(vm, () => commandSelector(vm).ExecuteAsync(null));
    }

    private static async Task ExecuteAsync(MainWindowViewModel vm, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            vm.StatusMessage = $"Error: {ex.Message}";
        }
    }
}

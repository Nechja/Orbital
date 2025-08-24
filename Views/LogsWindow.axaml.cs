using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using OrbitalDocking.ViewModels;
using System;

namespace OrbitalDocking.Views;

public partial class LogsWindow : Window
{
    private readonly ScrollViewer? _scrollViewer;
    private readonly LogsViewModel? _viewModel;

    public LogsWindow()
    {
        InitializeComponent();
        _scrollViewer = this.FindControl<ScrollViewer>("LogsScrollViewer");
    }

    public LogsWindow(string containerId, string containerName) : this()
    {
        _viewModel = new LogsViewModel(containerId, containerName);
        DataContext = _viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LogsViewModel.LogsContent) && _viewModel?.AutoScroll == true)
        {
            Dispatcher.UIThread.Post(() =>
            {
                _scrollViewer?.ScrollToEnd();
            });
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel?.Dispose();
        base.OnClosed(e);
    }
}
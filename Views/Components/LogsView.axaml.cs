using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using OrbitalDocking.ViewModels;

namespace OrbitalDocking.Views.Components;

public partial class LogsView : UserControl
{
    private INotifyPropertyChanged? _currentViewModel;

    public LogsView()
    {
        InitializeComponent();
        this.GetObservable(DataContextProperty).Subscribe(OnDataContextChanged);
    }

    private void OnDataContextChanged(object? newDataContext)
    {
        if (_currentViewModel is not null)
            _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _currentViewModel = newDataContext as INotifyPropertyChanged;

        if (_currentViewModel is not null)
            _currentViewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LogsViewModel.LogsContent) &&
            sender is LogsViewModel vm &&
            vm.AutoScroll)
        {
            var scrollViewer = this.FindControl<ScrollViewer>("LogsScrollViewer");
            scrollViewer?.ScrollToEnd();
        }
    }
}

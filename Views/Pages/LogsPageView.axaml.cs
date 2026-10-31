using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using OrbitalDocking.ViewModels.Pages;

namespace OrbitalDocking.Views.Pages;

public partial class LogsPageView : UserControl
{
    private INotifyPropertyChanged? _currentViewModel;

    public LogsPageView()
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
        if (e.PropertyName == nameof(LogsPageViewModel.LogsContent) &&
            sender is LogsPageViewModel { AutoScroll: true })
        {
            this.FindControl<ScrollViewer>("LogsScrollViewer")?.ScrollToEnd();
        }
    }
}

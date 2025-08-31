using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OrbitalDocking.Services;

namespace OrbitalDocking.Views.Dialogs;

public partial class VolumeRemovalDialog : Window, INotifyPropertyChanged
{
    private string _containerName = string.Empty;
    private List<string> _volumeNames = new();
    
    public string ContainerName 
    { 
        get => _containerName;
        set
        {
            _containerName = value;
            OnPropertyChanged();
        }
    }
    
    public List<string> VolumeNames 
    { 
        get => _volumeNames;
        set
        {
            _volumeNames = value;
            OnPropertyChanged();
        }
    }
    
    public VolumeRemovalDialog()
    {
        InitializeComponent();
        DataContext = this;
    }
    
    public new event PropertyChangedEventHandler? PropertyChanged;
    
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(VolumeRemovalChoice.Cancel);
    }
    
    private void OnContainerOnlyClick(object? sender, RoutedEventArgs e)
    {
        Close(VolumeRemovalChoice.RemoveContainerOnly);
    }
    
    private void OnContainerAndVolumesClick(object? sender, RoutedEventArgs e)
    {
        Close(VolumeRemovalChoice.RemoveContainerAndVolumes);
    }
}
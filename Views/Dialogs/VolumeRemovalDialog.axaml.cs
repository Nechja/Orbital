using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using OrbitalDocking.Services;

namespace OrbitalDocking.Views.Dialogs;

public partial class VolumeRemovalDialog : Window
{
    public string ContainerName { get; set; } = string.Empty;
    public List<string> VolumeNames { get; set; } = new();
    
    public VolumeRemovalDialog()
    {
        InitializeComponent();
        DataContext = this;
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
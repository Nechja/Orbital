using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using OrbitalDocking.ViewModels;

namespace OrbitalDocking.Views.Components;

public partial class ImageCard : UserControl
{
    public ImageCard()
    {
        InitializeComponent();
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Wire up commands to the MainWindowViewModel
        if (DataContext is ImageViewModel image)
        {
            var mainWindow = this.FindAncestorOfType<Window>();
            if (mainWindow?.DataContext is MainWindowViewModel mainVm)
            {
                var runBtn = this.FindControl<Button>("RunButton");
                var removeBtn = this.FindControl<Button>("RemoveButton");
                var pullBtn = this.FindControl<Button>("PullButton");
                
                if (runBtn != null)
                {
                    runBtn.Command = mainVm.RunImageCommand;
                    runBtn.CommandParameter = image;
                }
                
                if (removeBtn != null)
                {
                    removeBtn.Command = mainVm.RemoveImageCommand;
                    removeBtn.CommandParameter = image;
                }
                
                if (pullBtn != null)
                {
                    pullBtn.Command = mainVm.PullImageCommand;
                    pullBtn.CommandParameter = image;
                }
            }
        }
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using OrbitalDocking.ViewModels;

namespace OrbitalDocking.Views.Components;

public partial class StackGroup : UserControl
{
    public StackGroup()
    {
        InitializeComponent();
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        if (DataContext is StackViewModel stack)
        {
            var mainWindow = this.FindAncestorOfType<Window>();
            if (mainWindow?.DataContext is MainWindowViewModel mainVm)
            {
                var startStackBtn = this.FindControl<Button>("StartStackButton");
                var stopStackBtn = this.FindControl<Button>("StopStackButton");
                
                if (startStackBtn != null)
                {
                    startStackBtn.Command = mainVm.StartStackCommand;
                    startStackBtn.CommandParameter = stack;
                }
                
                if (stopStackBtn != null)
                {
                    stopStackBtn.Command = mainVm.StopStackCommand;
                    stopStackBtn.CommandParameter = stack;
                }
            }
        }
    }
}
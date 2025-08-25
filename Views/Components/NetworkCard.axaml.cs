using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using OrbitalDocking.ViewModels;

namespace OrbitalDocking.Views.Components;

public partial class NetworkCard : UserControl
{
    public NetworkCard()
    {
        InitializeComponent();
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        // Wire up commands to the MainWindowViewModel
        if (DataContext is NetworkViewModel network)
        {
            var mainWindow = this.FindAncestorOfType<Window>();
            if (mainWindow?.DataContext is MainWindowViewModel mainVm)
            {
                var removeBtn = this.FindControl<Button>("RemoveButton");
                
                if (removeBtn != null)
                {
                    removeBtn.Command = mainVm.RemoveNetworkCommand;
                    removeBtn.CommandParameter = network;
                }
            }
        }
    }
}
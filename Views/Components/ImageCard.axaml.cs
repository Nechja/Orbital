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

        // Monitor size changes for responsive layout
        PropertyChanged += OnCardPropertyChanged;
    }

    private void OnCardPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(Bounds) && DataContext is ImageViewModel vm)
        {
            var width = Bounds.Width;
            vm.IsCompactMode = width < 600; // Breakpoint at 600px
        }
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

                // Compact menu items
                var menuRunBtn = this.FindControl<MenuItem>("MenuRunButton");
                var menuRemoveBtn = this.FindControl<MenuItem>("MenuRemoveButton");
                var menuPullBtn = this.FindControl<MenuItem>("MenuPullButton");

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

                // Wire up compact menu items
                if (menuRunBtn != null)
                {
                    menuRunBtn.Command = mainVm.RunImageCommand;
                    menuRunBtn.CommandParameter = image;
                }

                if (menuRemoveBtn != null)
                {
                    menuRemoveBtn.Command = mainVm.RemoveImageCommand;
                    menuRemoveBtn.CommandParameter = image;
                }

                if (menuPullBtn != null)
                {
                    menuPullBtn.Command = mainVm.PullImageCommand;
                    menuPullBtn.CommandParameter = image;
                }
            }
        }
    }
}
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using OrbitalDocking.ViewModels;

namespace OrbitalDocking.Views.Components;

public partial class ImageCard : UserControl
{
    private bool _commandsWired = false;

    public ImageCard()
    {
        InitializeComponent();
        PropertyChanged += OnCardPropertyChanged;
    }

    private void OnCardPropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name == nameof(Bounds) && DataContext is ImageViewModel vm)
            vm.IsCompactMode = Bounds.Width < 600;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_commandsWired) return;
        if (DataContext is not ImageViewModel image) return;

        _commandsWired = true;

        var mainWindow = this.FindAncestorOfType<Window>();
        if (mainWindow?.DataContext is MainWindowViewModel mainVm)
        {
            var runBtn = this.FindControl<Button>("RunButton");
            var removeBtn = this.FindControl<Button>("RemoveButton");
            var pullBtn = this.FindControl<Button>("PullButton");

            var menuRunBtn = this.FindControl<MenuItem>("MenuRunButton");
            var menuRemoveBtn = this.FindControl<MenuItem>("MenuRemoveButton");
            var menuPullBtn = this.FindControl<MenuItem>("MenuPullButton");

            if (runBtn is not null)
            {
                runBtn.Command = mainVm.RunImageCommand;
                runBtn.CommandParameter = image;
            }

            if (removeBtn is not null)
            {
                removeBtn.Command = mainVm.RemoveImageCommand;
                removeBtn.CommandParameter = image;
            }

            if (pullBtn is not null)
            {
                pullBtn.Command = mainVm.PullImageCommand;
                pullBtn.CommandParameter = image;
            }

            if (menuRunBtn is not null)
            {
                menuRunBtn.Command = mainVm.RunImageCommand;
                menuRunBtn.CommandParameter = image;
            }

            if (menuRemoveBtn is not null)
            {
                menuRemoveBtn.Command = mainVm.RemoveImageCommand;
                menuRemoveBtn.CommandParameter = image;
            }

            if (menuPullBtn is not null)
            {
                menuPullBtn.Command = mainVm.PullImageCommand;
                menuPullBtn.CommandParameter = image;
            }
        }
    }
}
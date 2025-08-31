using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OrbitalDocking.Views.Dialogs;

public partial class ConfirmationDialog : Window
{
    public string Message { get; set; } = string.Empty;
    
    public ConfirmationDialog()
    {
        InitializeComponent();
        DataContext = this;
    }
    
    private void OnConfirmClick(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
    
    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
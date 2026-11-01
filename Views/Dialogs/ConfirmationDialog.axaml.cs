using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace OrbitalDocking.Views.Dialogs;

/// <summary>A yes/no prompt. Title is the question, Message the consequence. Closes with true on confirm.</summary>
public partial class ConfirmationDialog : Window
{
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<ConfirmationDialog, string>(nameof(Message), string.Empty);

    public static readonly StyledProperty<string> ConfirmTextProperty =
        AvaloniaProperty.Register<ConfirmationDialog, string>(nameof(ConfirmText), "Confirm");

    public ConfirmationDialog()
    {
        InitializeComponent();
        DataContext = this;
    }

    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public string ConfirmText
    {
        get => GetValue(ConfirmTextProperty);
        set => SetValue(ConfirmTextProperty, value);
    }

    private void OnConfirmClick(object? sender, RoutedEventArgs e) => Close(true);

    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(false);
}

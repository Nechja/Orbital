using CommunityToolkit.Mvvm.ComponentModel;

namespace OrbitalDocking.ViewModels.Dialogs;

public partial class EnvironmentVariableViewModel : ObservableObject
{
    [ObservableProperty]
    private string _key = "";

    [ObservableProperty]
    private string _value = "";

    public EnvironmentVariableViewModel()
    {
    }

    public EnvironmentVariableViewModel(string key, string value = "")
    {
        Key = key;
        Value = value;
    }
}
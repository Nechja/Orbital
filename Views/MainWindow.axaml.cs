using System;
using Avalonia.Controls;
using Avalonia.Platform;

namespace OrbitalDocking.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://OrbitalDocking/Assets/orbital.ico")));
    }
}

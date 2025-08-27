using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;

namespace OrbitalDocking.Services;

public class TrayService : ITrayService, IDisposable
{
    private TrayIcon? _trayIcon;
    private Window? _mainWindow;
    
    public event EventHandler? ShowRequested;
    public event EventHandler? ExitRequested;
    
    public void Initialize()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _mainWindow = desktop.MainWindow;
            
            _trayIcon = new TrayIcon
            {
                ToolTipText = "Orbital Docking - Docker Desktop Alternative",
                Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://OrbitalDocking/Assets/orbital.ico"))),
                IsVisible = true
            };
            
            _trayIcon.Clicked += OnTrayIconClicked;
            
            var contextMenu = new NativeMenu();
            
            var showItem = new NativeMenuItem("Show Orbital");
            showItem.Click += (_, _) => ShowWindow();
            contextMenu.Add(showItem);
            
            contextMenu.Add(new NativeMenuItemSeparator());
            
            var exitItem = new NativeMenuItem("Exit");
            exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
            contextMenu.Add(exitItem);
            
            _trayIcon.Menu = contextMenu;
            
            var trayIcons = new TrayIcons();
            trayIcons.Add(_trayIcon);
            TrayIcon.SetIcons(Application.Current, trayIcons);
        }
    }
    
    private void OnTrayIconClicked(object? sender, EventArgs e)
    {
        ToggleWindow();
    }
    
    public void ShowWindow()
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            _mainWindow.Topmost = true;
            _mainWindow.Topmost = false;
        }
        ShowRequested?.Invoke(this, EventArgs.Empty);
    }
    
    public void HideWindow()
    {
        _mainWindow?.Hide();
    }
    
    public void ToggleWindow()
    {
        if (_mainWindow != null)
        {
            if (_mainWindow.IsVisible)
            {
                HideWindow();
            }
            else
            {
                ShowWindow();
            }
        }
    }
    
    public void Dispose()
    {
        if (_trayIcon != null)
        {
            _trayIcon.Clicked -= OnTrayIconClicked;
            _trayIcon.IsVisible = false;
            _trayIcon.Dispose();
            _trayIcon = null;
        }
    }
}
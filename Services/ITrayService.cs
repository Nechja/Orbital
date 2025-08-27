using System;

namespace OrbitalDocking.Services;

public interface ITrayService
{
    void Initialize();
    void ShowWindow();
    void HideWindow();
    void ToggleWindow();
    void Dispose();
    
    event EventHandler? ShowRequested;
    event EventHandler? ExitRequested;
}
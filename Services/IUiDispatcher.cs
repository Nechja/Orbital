using System;
using System.Threading.Tasks;

namespace OrbitalDocking.Services;

/// <summary>
/// Marshals work onto the UI thread. Abstracted so page view models can be exercised in tests
/// without an Avalonia platform.
/// </summary>
public interface IUiDispatcher
{
    bool CheckAccess();
    Task InvokeAsync(Action action);
}

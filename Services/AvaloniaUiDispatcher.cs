using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace OrbitalDocking.Services;

public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

    public Task InvokeAsync(Action action) =>
        CheckAccess() ? Run(action) : Dispatcher.UIThread.InvokeAsync(action).GetTask();

    private static Task Run(Action action)
    {
        action();
        return Task.CompletedTask;
    }
}

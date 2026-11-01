using OrbitalDocking.Services;

namespace OrbitalDocking.Tests.TestSupport;

public sealed class ImmediateUiDispatcher : IUiDispatcher
{
    public bool CheckAccess() => true;

    public Task InvokeAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }
}

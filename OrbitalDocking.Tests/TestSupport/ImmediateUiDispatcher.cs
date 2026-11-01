using OrbitalDocking.Services;

namespace OrbitalDocking.Tests.TestSupport;

/// <summary>Runs UI-thread work inline so page view models can be driven synchronously in tests.</summary>
public sealed class ImmediateUiDispatcher : IUiDispatcher
{
    public bool CheckAccess() => true;

    public Task InvokeAsync(Action action)
    {
        action();
        return Task.CompletedTask;
    }
}

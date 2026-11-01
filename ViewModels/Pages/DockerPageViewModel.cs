using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using OrbitalDocking.Services;

namespace OrbitalDocking.ViewModels.Pages;

public abstract class DockerPageViewModel : PageViewModel
{
    private readonly SemaphoreSlim _refreshGate = new(1, 1);
    private volatile bool _disposed;

    protected DockerPageViewModel(IDockerService docker, AppState state, IUiDispatcher ui)
    {
        Docker = docker;
        State = state;
        Ui = ui;
    }

    protected IDockerService Docker { get; }
    protected AppState State { get; }
    protected IUiDispatcher Ui { get; }
    protected CompositeDisposable Subscriptions { get; } = new();
    protected bool IsDisposed => _disposed;

    public abstract Task RefreshAsync();

    protected async Task GuardedRefreshAsync(Func<Task> body)
    {
        if (_disposed || !await _refreshGate.WaitAsync(0)) return;
        try
        {
            await body();
        }
        catch (ObjectDisposedException) { }
        catch (Exception ex) when (!_disposed)
        {
            await Ui.InvokeAsync(() => State.StatusMessage = $"Error: {ex.Message}");
        }
        finally
        {
            _refreshGate.Release();
        }
    }

    protected void StartPeriodicRefresh(TimeSpan dueTime, TimeSpan period) =>
        Subscriptions.Add(Observable.Timer(dueTime, period).Subscribe(_ => RefreshAsync()));

    protected async Task RunAsync(string inProgress, string done, Func<Task<ErrorOr<Success>>> operation)
    {
        State.StatusMessage = inProgress;
        var result = await operation();
        State.StatusMessage = result.IsError ? result.FirstError.Description : done;
        await RefreshAsync();
    }

    public override void Dispose()
    {
        _disposed = true;
        Subscriptions.Dispose();
    }
}

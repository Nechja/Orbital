using System;
using System.Threading.Tasks;

namespace OrbitalDocking.Services;

public interface IUiDispatcher
{
    bool CheckAccess();
    Task InvokeAsync(Action action);
}

using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OrbitalDocking.ViewModels.Pages;

public abstract partial class PageViewModel : ObservableObject, IDisposable
{
    public abstract string Title { get; }

    public abstract string IconKey { get; }

    [ObservableProperty]
    private int? _count;

    public virtual void Activate() { }

    public virtual void Dispose() { }
}

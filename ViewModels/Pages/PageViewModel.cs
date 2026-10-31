using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace OrbitalDocking.ViewModels.Pages;

/// <summary>
/// A top-level page hosted by the main window. The shell lists pages in the sidebar
/// (Title, IconKey, Count) and calls <see cref="Activate"/> when one is shown.
/// </summary>
public abstract partial class PageViewModel : ObservableObject, IDisposable
{
    public abstract string Title { get; }

    /// <summary>Resource key of the sidebar icon, e.g. "Icon.Box" (see Styles/Icons.axaml).</summary>
    public abstract string IconKey { get; }

    /// <summary>Optional badge shown next to the title in the sidebar.</summary>
    [ObservableProperty]
    private int? _count;

    /// <summary>Called by the shell each time the page becomes the current page.</summary>
    public virtual void Activate() { }

    public virtual void Dispose() { }
}

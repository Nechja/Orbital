using OrbitalDocking.Services;

namespace OrbitalDocking.Tests.TestSupport;

public sealed class InMemoryPreferencesStore : IPreferencesStore
{
    public Preferences Current { get; private set; } = new();
    public Preferences Load() => Current;
    public void Save(Preferences preferences) => Current = preferences;
}

using FluentAssertions;
using OrbitalDocking.Models;
using OrbitalDocking.Services;
using Xunit;

namespace OrbitalDocking.Tests.Services;

public class PreferencesStoreTests
{
    [Fact]
    public void RoundTrips_ThroughAFileItCreates()
    {
        var path = Path.Combine(Path.GetTempPath(), "orbital-tests", Guid.NewGuid().ToString("N"), "preferences.json");
        var store = new JsonPreferencesStore(path);

        store.Load().Should().Be(new Preferences(), "missing file yields defaults");

        store.Save(new Preferences(ThemeMode.Soft, SidebarCollapsed: true));

        new JsonPreferencesStore(path).Load().Should().Be(new Preferences(ThemeMode.Soft, true));
        File.ReadAllText(path).Should().Contain("\"Soft\"", "enums are stored by name so the file is readable");
        Directory.Delete(Path.GetDirectoryName(path)!, recursive: true);
    }

    [Fact]
    public void Load_FallsBackToDefaultsOnCorruptFile()
    {
        var path = Path.Combine(Path.GetTempPath(), "orbital-tests-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, "{ not json");

        new JsonPreferencesStore(path).Load().Should().Be(new Preferences());
        File.Delete(path);
    }
}

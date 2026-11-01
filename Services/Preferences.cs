using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using OrbitalDocking.Models;

namespace OrbitalDocking.Services;

/// <summary>User preferences that survive restarts. Add fields here; the store round-trips them as JSON.</summary>
public sealed record Preferences(
    ThemeMode Theme = ThemeMode.Dark,
    bool SidebarCollapsed = false);

public interface IPreferencesStore
{
    Preferences Load();
    void Save(Preferences preferences);
}

/// <summary>Stores preferences as JSON under the user's config directory (e.g. ~/.config/Orbital on Linux).</summary>
public sealed class JsonPreferencesStore : IPreferencesStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;

    public JsonPreferencesStore()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Orbital", "preferences.json"))
    {
    }

    public JsonPreferencesStore(string path)
    {
        _path = path;
    }

    public Preferences Load()
    {
        try
        {
            if (!File.Exists(_path)) return new Preferences();
            return JsonSerializer.Deserialize<Preferences>(File.ReadAllText(_path), Options) ?? new Preferences();
        }
        catch (Exception)
        {
            return new Preferences();
        }
    }

    public void Save(Preferences preferences)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(preferences, Options));
        }
        catch (Exception)
        {
            // Preferences are a convenience; never fail the app over them.
        }
    }
}

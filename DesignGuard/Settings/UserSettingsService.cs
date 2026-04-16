using System.IO;
using System.Text.Json;

namespace DesignGuard.Settings;

/// <summary>Lokale voorkeuren (geen cloud).</summary>
public sealed class UserSettingsService
{
    private static readonly JsonSerializerOptions Opts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _path;

    public UserSettingsService(string appDataDirectory)
    {
        Directory.CreateDirectory(appDataDirectory);
        _path = Path.Combine(appDataDirectory, "user-settings.json");
        Current = Load();
    }

    public UserAppSettings Current { get; private set; }

    public void Reload() => Current = Load();

    public void Save()
    {
        var json = JsonSerializer.Serialize(Current, Opts);
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, json);
        File.Copy(tmp, _path, overwrite: true);
        File.Delete(tmp);
    }

    public void SetPackDisabled(string packId, bool disabled)
    {
        if (disabled)
        {
            if (!Current.DisabledPackIds.Contains(packId))
                Current.DisabledPackIds.Add(packId);
        }
        else
            Current.DisabledPackIds.Remove(packId);

        Save();
    }

    private UserAppSettings Load()
    {
        try
        {
            if (!File.Exists(_path)) return new UserAppSettings();
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<UserAppSettings>(json, Opts) ?? new UserAppSettings();
        }
        catch
        {
            return new UserAppSettings();
        }
    }
}

using System.Text.Json;
using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.Services.Implementations;

/// <summary>
/// Implements <see cref="ISettingsService"/> by reading and writing a local
/// <c>appsettings.json</c> file in the application data directory.
/// </summary>
public sealed class LocalSettingsService : ISettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    private readonly string _settingsPath;

    /// <summary>Initialises <see cref="LocalSettingsService"/> using the default app-data folder.</summary>
    public LocalSettingsService()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AiComposer");

        Directory.CreateDirectory(appDataDir);
        _settingsPath = Path.Combine(appDataDir, "appsettings.json");
    }

    /// <inheritdoc/>
    public async Task<AppSettings> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_settingsPath))
            return new AppSettings();

        try
        {
            var json = await File.ReadAllTextAsync(_settingsPath, ct);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch (Exception)
        {
            return new AppSettings();
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_settingsPath, json, ct);
    }
}

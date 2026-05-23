using AiComposer.Maui.Models;

namespace AiComposer.Maui.Services.Abstractions;

/// <summary>Reads and writes application settings from persistent storage.</summary>
public interface ISettingsService
{
    /// <summary>Loads current settings from storage.</summary>
    Task<AppSettings> LoadAsync(CancellationToken ct = default);

    /// <summary>Persists the given settings to storage.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}

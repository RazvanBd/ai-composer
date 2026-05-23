using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.Services.Implementations;

/// <summary>
/// Implements <see cref="IWorkspaceService"/> by reading the workspace path
/// from persisted settings and delegating folder picking to the platform.
/// </summary>
public sealed class FileWorkspaceService : IWorkspaceService
{
    private readonly ISettingsService _settingsService;
    private string? _currentWorkspacePath;

    /// <inheritdoc/>
    public string? CurrentWorkspacePath => _currentWorkspacePath;

    /// <summary>Initialises <see cref="FileWorkspaceService"/> with the required settings service.</summary>
    public FileWorkspaceService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <inheritdoc/>
    public async Task<string?> OpenWorkspaceAsync()
    {
        var result = await FolderPicker.Default.PickAsync(CancellationToken.None);
        if (result.IsSuccessful && !string.IsNullOrWhiteSpace(result.Folder?.Path))
        {
            _currentWorkspacePath = result.Folder.Path;
            var settings = await _settingsService.LoadAsync();
            settings.WorkspacePath = _currentWorkspacePath;
            await _settingsService.SaveAsync(settings);
            return _currentWorkspacePath;
        }

        return null;
    }

    /// <inheritdoc/>
    public void SetWorkspacePath(string path)
    {
        _currentWorkspacePath = path;
    }
}

namespace AiComposer.Maui.Services.Abstractions;

/// <summary>Provides workspace selection and current-path access.</summary>
public interface IWorkspaceService
{
    /// <summary>Gets the currently open workspace path, or null if none is open.</summary>
    string? CurrentWorkspacePath { get; }

    /// <summary>Prompts the user to open a workspace folder and sets it as the current workspace.</summary>
    /// <returns>The chosen path, or null if the user cancelled.</returns>
    Task<string?> OpenWorkspaceAsync();

    /// <summary>Sets the workspace path without showing a folder picker.</summary>
    void SetWorkspacePath(string path);
}

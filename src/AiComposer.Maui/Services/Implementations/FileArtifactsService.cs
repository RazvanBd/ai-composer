using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.Services.Implementations;

/// <summary>
/// Implements <see cref="IArtifactsService"/> by reading Markdown files
/// directly from the workspace folder on disk.
/// </summary>
public sealed class FileArtifactsService : IArtifactsService
{
    private readonly IWorkspaceService _workspaceService;
    private IReadOnlyList<ArtifactItem>? _cachedArtifacts;

    /// <summary>Initialises <see cref="FileArtifactsService"/>.</summary>
    public FileArtifactsService(IWorkspaceService workspaceService)
    {
        _workspaceService = workspaceService;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ArtifactItem>> LoadArtifactsAsync(CancellationToken ct = default)
    {
        var workspace = _workspaceService.CurrentWorkspacePath;
        if (string.IsNullOrWhiteSpace(workspace) || !Directory.Exists(workspace))
        {
            _cachedArtifacts = [];
            return _cachedArtifacts;
        }

        var mdFiles = Directory.EnumerateFiles(workspace, "*.md", SearchOption.AllDirectories);
        var items = new List<ArtifactItem>();

        foreach (var file in mdFiles)
        {
            ct.ThrowIfCancellationRequested();
            var content = await File.ReadAllTextAsync(file, ct);
            var (id, type, title) = ParseFrontmatter(content);
            items.Add(new ArtifactItem
            {
                Id = id,
                Type = type,
                Title = title,
                FilePath = file,
                Content = content,
            });
        }

        _cachedArtifacts = items;
        return _cachedArtifacts;
    }

    /// <inheritdoc/>
    public Task<string> ReadArtifactContentAsync(string filePath, CancellationToken ct = default)
        => File.ReadAllTextAsync(filePath, ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ArtifactItem>> ValidateAsync(CancellationToken ct = default)
    {
        var items = _cachedArtifacts ?? await LoadArtifactsAsync(ct);

        foreach (var item in items)
        {
            item.ValidationStatus = (!string.IsNullOrWhiteSpace(item.Id) &&
                                     !string.IsNullOrWhiteSpace(item.Type) &&
                                     !string.IsNullOrWhiteSpace(item.Title))
                ? "Valid"
                : "Invalid";
        }

        return items;
    }

    // -------------------------------------------------------------------------
    // Private helpers — minimal YAML frontmatter extraction
    // -------------------------------------------------------------------------

    private static (string id, string type, string title) ParseFrontmatter(string content)
    {
        string id = string.Empty, type = string.Empty, title = string.Empty;
        if (!content.StartsWith("---", StringComparison.Ordinal))
            return (id, type, title);

        var end = content.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0)
            return (id, type, title);

        var frontmatter = content[3..end];
        foreach (var line in frontmatter.Split('\n'))
        {
            var colonIdx = line.IndexOf(':', StringComparison.Ordinal);
            if (colonIdx < 0) continue;

            var key = line[..colonIdx].Trim();
            var value = line[(colonIdx + 1)..].Trim();

            if (key == "id") id = value;
            else if (key == "type") type = value;
            else if (key == "title") title = value;
        }

        return (id, type, title);
    }
}

using AiComposer.Maui.Models;

namespace AiComposer.Maui.Services.Abstractions;

/// <summary>Lists, reads, and validates Markdown artifacts from the current workspace.</summary>
public interface IArtifactsService
{
    /// <summary>Loads all artifacts from the workspace artifacts folder.</summary>
    Task<IReadOnlyList<ArtifactItem>> LoadArtifactsAsync(CancellationToken ct = default);

    /// <summary>Reads the raw Markdown content of a single artifact file.</summary>
    Task<string> ReadArtifactContentAsync(string filePath, CancellationToken ct = default);

    /// <summary>
    /// Validates all artifacts and returns them with updated <see cref="ArtifactItem.ValidationStatus"/>.
    /// An artifact is Valid when it has a non-empty id, type, and title in its YAML frontmatter.
    /// </summary>
    Task<IReadOnlyList<ArtifactItem>> ValidateAsync(CancellationToken ct = default);
}

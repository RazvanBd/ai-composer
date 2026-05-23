using AiComposer.Maui.Models;

namespace AiComposer.Maui.Services.Abstractions;

/// <summary>Lists and reads Markdown artifacts from the current workspace.</summary>
public interface IArtifactsService
{
    /// <summary>Loads all artifacts from the workspace artifacts folder.</summary>
    Task<IReadOnlyList<ArtifactItem>> LoadArtifactsAsync(CancellationToken ct = default);

    /// <summary>Reads the raw Markdown content of a single artifact file.</summary>
    Task<string> ReadArtifactContentAsync(string filePath, CancellationToken ct = default);
}

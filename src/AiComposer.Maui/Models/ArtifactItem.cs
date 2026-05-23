namespace AiComposer.Maui.Models;

/// <summary>Represents a single artifact file in the workspace.</summary>
public sealed class ArtifactItem
{
    /// <summary>Unique identifier (e.g., "T-101").</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Artifact type: project_summary, epic, rule, ticket, adr.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Human-readable title from frontmatter.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Full path to the Markdown file on disk.</summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>Raw Markdown content of the artifact file.</summary>
    public string Content { get; init; } = string.Empty;
}

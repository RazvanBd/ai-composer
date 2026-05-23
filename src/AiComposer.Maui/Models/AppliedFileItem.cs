namespace AiComposer.Maui.Models;

/// <summary>Represents a file affected by a run and its inferred change kind.</summary>
public sealed class AppliedFileItem
{
    /// <summary>Relative path of the file within the ticket output folder.</summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>Absolute path of the file on disk.</summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>Change kind badge: "New" or "Modified".</summary>
    public string ChangeKind { get; init; } = "Modified";
}

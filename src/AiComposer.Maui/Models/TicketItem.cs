namespace AiComposer.Maui.Models;

/// <summary>Represents a ticket with its current lifecycle state.</summary>
public sealed class TicketItem
{
    /// <summary>Unique ticket identifier (e.g., "T-101").</summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable ticket title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Lifecycle state: draft, ready, running, blocked, done, paused_human_intervention.</summary>
    public string State { get; init; } = string.Empty;

    /// <summary>Epic identifier this ticket belongs to.</summary>
    public string EpicId { get; init; } = string.Empty;

    /// <summary>Full path to the ticket Markdown file.</summary>
    public string FilePath { get; init; } = string.Empty;
}

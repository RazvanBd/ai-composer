namespace AiComposer.Maui.Models;

/// <summary>Represents a single execution run for a ticket.</summary>
public sealed class RunSessionItem
{
    /// <summary>Ticket ID this run belongs to.</summary>
    public string TicketId { get; init; } = string.Empty;

    /// <summary>Timestamp when the run started.</summary>
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>Timestamp when the run finished, or null if still running.</summary>
    public DateTimeOffset? FinishedAt { get; set; }

    /// <summary>Exit code of the CLI process (0 = success).</summary>
    public int? ExitCode { get; set; }

    /// <summary>Accumulated stdout + stderr output from the run.</summary>
    public string Output { get; set; } = string.Empty;
}

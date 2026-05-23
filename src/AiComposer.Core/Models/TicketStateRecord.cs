namespace AiComposer.Core.Models;

/// <summary>Persisted state record for a single ticket in the orchestrator state machine.</summary>
public sealed class TicketStateRecord
{
    /// <summary>Ticket identifier.</summary>
    public string TicketId { get; set; } = string.Empty;

    /// <summary>Current lifecycle state (see <see cref="TicketState"/>).</summary>
    public string State { get; set; } = TicketState.WaitingForCoder;

    /// <summary>Number of failed attempts (used by the circuit breaker).</summary>
    public int Attempts { get; set; }

    /// <summary>ISO-8601 timestamp of the last state change.</summary>
    public string UpdatedAt { get; set; } = string.Empty;

    /// <summary>Last error message, or null when none.</summary>
    public string? LastError { get; set; }
}

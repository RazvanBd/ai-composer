namespace AiComposer.Core.Models;

/// <summary>Single observability event emitted by an agent action (OTel-compatible shape).</summary>
public sealed class TraceEvent
{
    /// <summary>Globally unique trace identifier (UUID).</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>Ticket the action belongs to.</summary>
    public string TicketId { get; set; } = string.Empty;

    /// <summary>Agent role that generated this event (e.g. "tech_lead_orchestrator").</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Input token count for this LLM call.</summary>
    public int TokensIn { get; set; }

    /// <summary>Output token count for this LLM call.</summary>
    public int TokensOut { get; set; }

    /// <summary>Estimated USD cost for this call.</summary>
    public double CostUsd { get; set; }

    /// <summary>Outcome of the action ("success", "failure", etc.).</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Human-readable message describing the action.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>ISO-8601 UTC timestamp set automatically by the logger.</summary>
    public string Timestamp { get; set; } = string.Empty;
}

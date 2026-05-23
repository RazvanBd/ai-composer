namespace AiComposer.Core.Models;

/// <summary>Assembled strongly-typed execution context for a single ticket.</summary>
public sealed class TicketContext
{
    /// <summary>Identifier matching the source ticket artifact.</summary>
    public string TicketId { get; set; } = string.Empty;

    /// <summary>Human-readable ticket title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Acceptance criteria (Given/When/Then format).</summary>
    public List<string> AcceptanceCriteria { get; set; } = new();

    /// <summary>Static context always injected into the prompt (project summary, coding conventions).</summary>
    public Dictionary<string, object> StaticContext { get; set; } = new();

    /// <summary>Dynamic context assembled just-in-time (linked artifacts, ADRs).</summary>
    public Dictionary<string, object> DynamicContext { get; set; } = new();

    /// <summary>Security constraints for this ticket (sandbox policy, mock env).</summary>
    public Dictionary<string, object> SecurityContext { get; set; } = new();
}

namespace AiComposer.Core.Models;

/// <summary>Finite state constants for the orchestrator ticket lifecycle (state machine).</summary>
public static class TicketState
{
    /// <summary>Ticket is waiting for the Coder agent to start.</summary>
    public const string WaitingForCoder = "waiting_for_coder";

    /// <summary>Coder is actively iterating on this ticket.</summary>
    public const string CoderIterating = "coder_iterating";

    /// <summary>Coder iteration passed; waiting for Reviewer.</summary>
    public const string WaitingForReview = "waiting_for_review";

    /// <summary>Review passed; waiting for QA validation.</summary>
    public const string WaitingForQa = "waiting_for_qa";

    /// <summary>The external agent failed and the ticket needs another coder attempt.</summary>
    public const string CoderFailed = "coder_failed";

    /// <summary>All automated checks passed; PR is ready for human review.</summary>
    public const string ReadyForHumanReview = "ready_for_human_review";

    /// <summary>Circuit breaker triggered — ticket requires human intervention.</summary>
    public const string PausedHumanIntervention = "paused_human_intervention";
}

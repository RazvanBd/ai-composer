namespace AiComposer.Core.Agents;

/// <summary>Abstraction for provider-backed ticket agent execution.</summary>
public interface IAgentClient
{
    /// <summary>Executes a ticket-scoped agent request and returns the normalized result.</summary>
    Task<AgentExecutionResult> ExecuteAsync(
        AgentExecutionRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}

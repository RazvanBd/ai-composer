using System.Text.Json.Serialization;
using AiComposer.Core.Models;

namespace AiComposer.Core.Agents;

/// <summary>Markdown artifact payload passed to an external agent provider.</summary>
public sealed class AgentArtifact
{
    /// <summary>Stable artifact identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Artifact type from YAML frontmatter.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Human-readable artifact title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Artifact source path.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Frontmatter metadata that should influence the agent response.</summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>Markdown body content.</summary>
    public string Body { get; set; } = string.Empty;
}

/// <summary>Runtime options for spawning ticket-scoped agents.</summary>
public sealed class AgentExecutionOptions
{
    /// <summary>Provider identifier, for example "deepseek".</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Provider model name.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Logical agent role, for example "coder" or "reviewer".</summary>
    public string Role { get; set; } = "coder";

    /// <summary>User-authored task prompt forwarded to the provider.</summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>When true, request metadata is written but no provider call is made.</summary>
    public bool DryRun { get; set; }

    /// <summary>When true, provider output is streamed live to the caller.</summary>
    public bool LiveOutput { get; set; }

    /// <summary>Workspace where generated files should be written.</summary>
    public string? ApplyFilesWorkspace { get; set; }

    /// <summary>When true, existing files inside the target workspace may be overwritten.</summary>
    public bool AllowOverwrite { get; set; }
}

/// <summary>Typed chat-completion request sent to an external provider.</summary>
public sealed class AgentExecutionRequest
{
    /// <summary>Provider identifier.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Provider model name.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Ticket identifier the request belongs to.</summary>
    public string TicketId { get; set; } = string.Empty;

    /// <summary>Logical agent role requested by the caller.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Sanitized ticket context supplied to the agent.</summary>
    public TicketContext TicketContext { get; set; } = new();

    /// <summary>Artifacts included in the prompt payload.</summary>
    public List<AgentArtifact> Artifacts { get; set; } = new();

    /// <summary>System message used to constrain the model.</summary>
    public string SystemPrompt { get; set; } = string.Empty;

    /// <summary>User message containing the task plus artifact payload.</summary>
    public string UserPrompt { get; set; } = string.Empty;

    /// <summary>Preferred provider response format.</summary>
    public string ResponseFormat { get; set; } = "text";
}

/// <summary>Normalized completion result returned by an external provider.</summary>
public sealed class AgentExecutionResult
{
    /// <summary>Provider identifier that generated this result.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Provider response identifier.</summary>
    public string ResponseId { get; set; } = string.Empty;

    /// <summary>Provider model name that generated the output.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Logical role associated with the response.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Generated response body.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Provider finish reason, if any.</summary>
    public string FinishReason { get; set; } = string.Empty;

    /// <summary>Prompt token count reported by the provider.</summary>
    public int PromptTokens { get; set; }

    /// <summary>Completion token count reported by the provider.</summary>
    public int CompletionTokens { get; set; }

    /// <summary>Prompt tokens served from cache, when supported.</summary>
    public int PromptCacheHitTokens { get; set; }

    /// <summary>Prompt tokens billed as cache misses, when supported.</summary>
    public int PromptCacheMissTokens { get; set; }

    /// <summary>Estimated USD cost computed from token usage.</summary>
    public double CostUsd { get; set; }
}

/// <summary>On-disk audit metadata for a single spawned agent run.</summary>
public sealed class AgentRunAudit
{
    /// <summary>UTC timestamp for this run.</summary>
    public string CreatedAt { get; set; } = string.Empty;

    /// <summary>Provider identifier.</summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>Provider model name.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Ticket identifier.</summary>
    public string TicketId { get; set; } = string.Empty;

    /// <summary>Logical role requested for the run.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Relative path to the generated ticket context file.</summary>
    public string ContextFile { get; set; } = string.Empty;

    /// <summary>IDs of artifacts supplied to the provider.</summary>
    public List<string> ArtifactIds { get; set; } = new();

    /// <summary>Character length of the user prompt.</summary>
    public int PromptLength { get; set; }

    /// <summary>Whether the provider call was skipped intentionally.</summary>
    public bool DryRun { get; set; }

    /// <summary>Provider response identifier, when available.</summary>
    public string? ResponseId { get; set; }

    /// <summary>Provider finish reason, when available.</summary>
    public string? FinishReason { get; set; }

    /// <summary>Prompt token count reported by the provider.</summary>
    public int PromptTokens { get; set; }

    /// <summary>Completion token count reported by the provider.</summary>
    public int CompletionTokens { get; set; }

    /// <summary>Prompt cache-hit tokens reported by the provider.</summary>
    public int PromptCacheHitTokens { get; set; }

    /// <summary>Prompt cache-miss tokens reported by the provider.</summary>
    public int PromptCacheMissTokens { get; set; }

    /// <summary>Estimated USD cost for the run.</summary>
    public double CostUsd { get; set; }

    /// <summary>Error details when the provider call failed.</summary>
    public string? Error { get; set; }

    /// <summary>Paths applied into the workspace, when file application is enabled.</summary>
    public List<string> AppliedFiles { get; set; } = new();
}

/// <summary>Structured file plan returned by a coder agent.</summary>
public sealed class GeneratedFilePlan
{
    /// <summary>Short explanation of the generated change set.</summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>Files to create or update.</summary>
    public List<GeneratedFileEntry> Files { get; set; } = new();
}

/// <summary>Single text file emitted by the model.</summary>
public sealed class GeneratedFileEntry
{
    /// <summary>Relative path inside the target workspace.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>UTF-8 text content to write.</summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>Configuration for the DeepSeek chat-completions client.</summary>
public sealed class DeepSeekClientOptions
{
    /// <summary>Provider API key.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Provider base URL in OpenAI-compatible format.</summary>
    public string BaseUrl { get; set; } = "https://api.deepseek.com";

    /// <summary>Default model used when the request does not override it.</summary>
    public string Model { get; set; } = "deepseek-v4-flash";

    /// <summary>Sampling temperature applied to requests.</summary>
    public double Temperature { get; set; } = 0.2;
}

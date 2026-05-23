using System.Text;
using System.Text.Json;
using AiComposer.Core.Agents;
using AiComposer.Core.Markdown;
using AiComposer.Core.Models;
using AiComposer.Core.Security;
using AiComposer.Core.State;
using AiComposer.Core.Telemetry;

namespace AiComposer.Core.Orchestration;

/// <summary>
/// MD-driven orchestrator: loads the knowledge graph from markdown artifacts,
/// assembles strongly-typed <see cref="TicketContext"/> per ticket, persists
/// lifecycle state, and emits trace events for cost observability.
/// </summary>
public sealed class Orchestrator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly string[] CodingConventions =
    [
        "No hardcoded secrets — always read from environment variables",
        "Use environment variables for all external service integrations",
        "Add concise XML <summary> tags to every public method created or modified",
    ];

    private readonly StateStore _stateStore;
    private readonly TraceLogger _traceLogger;

    /// <summary>Initialises the orchestrator with its required infrastructure services.</summary>
    public Orchestrator(StateStore stateStore, TraceLogger traceLogger)
    {
        _stateStore = stateStore;
        _traceLogger = traceLogger;
    }

    /// <summary>
    /// Loads all markdown artifacts from <paramref name="artifactsDir"/>,
    /// processes every ticket node, writes per-ticket <c>context.json</c> files
    /// into <paramref name="outputDir"/>, and returns the processed ticket IDs.
    /// </summary>
    public IReadOnlyList<string> ProcessTickets(string artifactsDir, string outputDir)
        => ProcessTicketsAsync(artifactsDir, outputDir).GetAwaiter().GetResult();

    /// <summary>
    /// Loads all markdown artifacts, generates per-ticket context, and optionally
    /// spawns provider-backed agent runs for each ticket.
    /// </summary>
    public async Task<IReadOnlyList<string>> ProcessTicketsAsync(
        string artifactsDir,
        string outputDir,
        AgentExecutionOptions? agentOptions = null,
        IAgentClient? agentClient = null,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (agentOptions is not null && !agentOptions.DryRun && agentClient is null)
            throw new ArgumentNullException(nameof(agentClient), "An agent client is required when dry-run mode is disabled.");

        var graph = MarkdownLoader.LoadArtifacts(artifactsDir);
        Directory.CreateDirectory(outputDir);
        var processedIds = new List<string>();

        foreach (var node in graph.Values.Where(n => n.Type == "ticket"))
        {
            cancellationToken.ThrowIfCancellationRequested();
            _stateStore.EnsureTicket(node.Id);
            _stateStore.Transition(node.Id, TicketState.CoderIterating);

            var context = BuildTicketContext(node, graph);

            var ticketDir = Path.Combine(outputDir, node.Id);
            Directory.CreateDirectory(ticketDir);
            File.WriteAllText(
                Path.Combine(ticketDir, "context.json"),
                JsonSerializer.Serialize(context, JsonOptions));

            _traceLogger.Log(new TraceEvent
            {
                TraceId = Guid.NewGuid().ToString(),
                TicketId = node.Id,
                Role = "tech_lead_orchestrator",
                TokensIn = 1200,
                TokensOut = 450,
                CostUsd = 0.025,
                Status = "success",
                Message = "Generated strongly-typed context from markdown artifacts",
            });

            if (agentOptions is not null)
            {
                var request = BuildAgentExecutionRequest(node, graph, context, agentOptions);
                var runDirectory = CreateRunDirectory(ticketDir, agentOptions.Role);
                var auditPath = Path.Combine(runDirectory, "agent-request.json");
                var responsePath = Path.Combine(runDirectory, "agent-response.md");
                var appliedFilesPath = Path.Combine(runDirectory, "applied-files.json");
                var audit = new AgentRunAudit
                {
                    CreatedAt = DateTime.UtcNow.ToString("O"),
                    Provider = agentOptions.Provider,
                    Model = request.Model,
                    TicketId = node.Id,
                    Role = agentOptions.Role,
                    ContextFile = Path.GetRelativePath(runDirectory, Path.Combine(ticketDir, "context.json")),
                    ArtifactIds = request.Artifacts.Select(a => a.Id).ToList(),
                    PromptLength = agentOptions.Prompt.Length,
                    DryRun = agentOptions.DryRun,
                };

                if (agentOptions.DryRun)
                {
                    File.WriteAllText(
                        responsePath,
                        """
                        # Dry run

                        No provider call was executed. Review `agent-request.json` and rerun without `--dry-run` to invoke DeepSeek.
                        """);
                    File.WriteAllText(auditPath, JsonSerializer.Serialize(audit, JsonOptions));
                }
                else
                {
                    try
                    {
                        if (agentOptions.LiveOutput && progress is not null)
                            progress.Report($"{Environment.NewLine}--- {node.Id} ({agentOptions.Role}) ---{Environment.NewLine}");

                        var result = await agentClient!.ExecuteAsync(
                            request,
                            agentOptions.LiveOutput ? progress : null,
                            cancellationToken);

                        if (agentOptions.LiveOutput && progress is not null)
                            progress.Report(Environment.NewLine);
                        File.WriteAllText(responsePath, result.Content + Environment.NewLine);

                        if (!string.IsNullOrWhiteSpace(agentOptions.ApplyFilesWorkspace))
                        {
                            var appliedFiles = ApplyGeneratedFiles(
                                agentOptions.ApplyFilesWorkspace,
                                result,
                                agentOptions.AllowOverwrite);
                            audit.AppliedFiles = appliedFiles;
                            File.WriteAllText(appliedFilesPath, JsonSerializer.Serialize(appliedFiles, JsonOptions));
                        }

                        audit.ResponseId = result.ResponseId;
                        audit.FinishReason = result.FinishReason;
                        audit.PromptTokens = result.PromptTokens;
                        audit.CompletionTokens = result.CompletionTokens;
                        audit.PromptCacheHitTokens = result.PromptCacheHitTokens;
                        audit.PromptCacheMissTokens = result.PromptCacheMissTokens;
                        audit.CostUsd = result.CostUsd;
                        File.WriteAllText(auditPath, JsonSerializer.Serialize(audit, JsonOptions));

                        _traceLogger.Log(new TraceEvent
                        {
                            TraceId = string.IsNullOrWhiteSpace(result.ResponseId) ? Guid.NewGuid().ToString() : result.ResponseId,
                            TicketId = node.Id,
                            Role = $"{agentOptions.Role}_agent",
                            TokensIn = result.PromptTokens,
                            TokensOut = result.CompletionTokens,
                            CostUsd = result.CostUsd,
                            Status = "success",
                            Message = $"Spawned {agentOptions.Role} agent via {agentOptions.Provider}",
                        });
                    }
                    catch (Exception ex)
                    {
                        audit.Error = ex.Message;
                        File.WriteAllText(auditPath, JsonSerializer.Serialize(audit, JsonOptions));
                        _traceLogger.Log(new TraceEvent
                        {
                            TraceId = Guid.NewGuid().ToString(),
                            TicketId = node.Id,
                            Role = $"{agentOptions.Role}_agent",
                            TokensIn = 0,
                            TokensOut = 0,
                            CostUsd = 0,
                            Status = "error",
                            Message = ex.Message,
                        });
                        _stateStore.Transition(node.Id, TicketState.CoderFailed, ex.Message, incrementAttempt: true);
                        throw;
                    }
                }
            }

            _stateStore.Transition(node.Id, TicketState.WaitingForReview);
            processedIds.Add(node.Id);
        }

        return processedIds.AsReadOnly();
    }

    /// <summary>
    /// Builds a <see cref="TicketContext"/> for a single ticket by collecting
    /// static project context, dynamically linked artifacts, and security constraints.
    /// </summary>
    public TicketContext BuildTicketContext(ArtifactNode ticket, Dictionary<string, ArtifactNode> graph)
    {
        var acceptance = ticket.Metadata.TryGetValue("acceptance", out var rawAcc) && rawAcc is List<string> list
            ? list
            : [];

        var linked = ticket.Links
            .Where(graph.ContainsKey)
            .Select(id => graph[id])
            .Select(n => (object)new Dictionary<string, string>
            {
                ["id"] = n.Id,
                ["type"] = n.Type,
                ["title"] = n.Title,
                ["path"] = n.Path,
            })
            .ToList();

        var adrs = graph.Values
            .Where(n => n.Type == "adr" && n.Metadata.TryGetValue("ticket", out var t) && t?.ToString() == ticket.Id)
            .Select(n => n.Body)
            .ToList<object>();

        var projectSummaries = graph.Values
            .Where(n => n.Type == "project_summary")
            .Select(n => n.Body)
            .ToList<object>();

        return new TicketContext
        {
            TicketId = ticket.Id,
            Title = ticket.Title,
            AcceptanceCriteria = acceptance,
            StaticContext = new Dictionary<string, object>
            {
                ["projectSummary"] = projectSummaries,
                ["codingConventions"] = CodingConventions,
            },
            DynamicContext = new Dictionary<string, object>
            {
                ["linkedArtifacts"] = linked,
                ["adrs"] = adrs,
            },
            SecurityContext = new Dictionary<string, object>
            {
                ["sandboxRequired"] = true,
                ["mockEnvironment"] = SecurityHelper.BuildMockEnvironment(),
            },
        };
    }

    /// <summary>Builds the provider request payload for a single ticket.</summary>
    public AgentExecutionRequest BuildAgentExecutionRequest(
        ArtifactNode ticket,
        Dictionary<string, ArtifactNode> graph,
        TicketContext context,
        AgentExecutionOptions options)
    {
        var artifacts = new List<ArtifactNode> { ticket };
        artifacts.AddRange(ticket.Links.Where(graph.ContainsKey).Select(id => graph[id]));
        artifacts.AddRange(graph.Values.Where(n => n.Type == "adr" && n.Metadata.TryGetValue("ticket", out var t) && t?.ToString() == ticket.Id));
        artifacts.AddRange(graph.Values.Where(n => n.Type == "project_summary"));

        var distinctArtifacts = artifacts
            .GroupBy(n => n.Id, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderBy(n => n.Id, StringComparer.Ordinal)
            .Select(n => new AgentArtifact
            {
                Id = n.Id,
                Type = n.Type,
                Title = n.Title,
                Path = n.Path,
                Metadata = new Dictionary<string, object>(n.Metadata, StringComparer.OrdinalIgnoreCase),
                Body = n.Body,
            })
            .ToList();

        var sanitizedContext = new TicketContext
        {
            TicketId = context.TicketId,
            Title = context.Title,
            AcceptanceCriteria = [.. context.AcceptanceCriteria],
            StaticContext = new Dictionary<string, object>(context.StaticContext, StringComparer.Ordinal),
            DynamicContext = new Dictionary<string, object>(context.DynamicContext, StringComparer.Ordinal),
            SecurityContext = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["sandboxRequired"] = true,
            },
        };

        var artifactPayload = JsonSerializer.Serialize(distinctArtifacts, JsonOptions);
        var contextPayload = JsonSerializer.Serialize(sanitizedContext, JsonOptions);
        var writesFiles = !string.IsNullOrWhiteSpace(options.ApplyFilesWorkspace);

        return new AgentExecutionRequest
        {
            Provider = options.Provider,
            Model = options.Model,
            TicketId = ticket.Id,
            Role = options.Role,
            TicketContext = sanitizedContext,
            Artifacts = distinctArtifacts,
            ResponseFormat = writesFiles ? "json_object" : "text",
            SystemPrompt =
                writesFiles
                    ? $$"""
                    You are the {{options.Role}} agent in the AI Composer workflow.
                    Work only from the provided ticket context and Markdown artifacts.
                    Return only valid JSON with this exact shape:
                    {
                      "summary": "short summary",
                      "files": [
                        {
                          "path": "relative/path.ext",
                          "content": "full UTF-8 text file content"
                        }
                      ]
                    }
                    Rules:
                    - output only JSON, no markdown fences and no prose outside JSON
                    - every file path must be relative and must not contain drive letters, leading slashes, or `..`
                    - return only text files
                    - include every file needed for the requested change set
                    - if information is missing, still return JSON and explain the gap in `summary`
                    """
                    : $"""
                    You are the {options.Role} agent in the AI Composer workflow.
                    Work only from the provided ticket context and Markdown artifacts.
                    Do not invent files, methods, APIs, or architectural constraints that are not present here.
                    Keep outputs aligned with .NET 9 / C# and preserve the project conventions around environment variables, sandboxed execution, and XML <summary> tags for public methods.
                    If important information is missing, state that clearly instead of guessing.
                    """,
            UserPrompt =
                writesFiles
                    ? $$"""
                    User prompt:
                    {{options.Prompt}}

                    Target workspace:
                    {{options.ApplyFilesWorkspace}}

                    Ticket context (JSON):
                    {{contextPayload}}

                    Artifact payload (JSON):
                    {{artifactPayload}}

                    Generate the concrete files required for this ticket and return only the JSON object described in the system instructions.
                    """
                    : $"""
                    User prompt:
                    {options.Prompt}

                    Ticket context (JSON):
                    {contextPayload}

                    Artifact payload (JSON):
                    {artifactPayload}

                    Produce the best possible response for the {options.Role} role using only the material above.
                    """,
        };
    }

    private static List<string> ApplyGeneratedFiles(string workspace, AgentExecutionResult result, bool allowOverwrite)
    {
        var plan = ParseGeneratedFilePlan(result.Content);
        if (plan.Files.Count == 0)
            throw new InvalidOperationException("Agent returned no files to apply.");

        var root = Path.GetFullPath(workspace);
        Directory.CreateDirectory(root);

        var normalized = new List<(string RelativePath, string FullPath, string Content)>();
        foreach (var entry in plan.Files)
        {
            if (string.IsNullOrWhiteSpace(entry.Path))
                throw new InvalidOperationException("Generated file path cannot be empty.");
            if (string.IsNullOrWhiteSpace(entry.Content))
                throw new InvalidOperationException($"Generated file '{entry.Path}' has empty content.");
            if (Path.IsPathRooted(entry.Path))
                throw new InvalidOperationException($"Generated file path must be relative: {entry.Path}");

            var cleanedRelative = entry.Path.Replace('/', Path.DirectorySeparatorChar).Trim();
            if (cleanedRelative.StartsWith(Path.DirectorySeparatorChar))
                throw new InvalidOperationException($"Generated file path must not start at filesystem root: {entry.Path}");

            var segments = cleanedRelative.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => segment == ".."))
                throw new InvalidOperationException($"Generated file path must stay inside the workspace: {entry.Path}");

            var fullPath = Path.GetFullPath(Path.Combine(root, cleanedRelative));
            if (!SecurityHelper.IsPathInsideWorkspace(root, fullPath))
                throw new InvalidOperationException($"Generated file path escapes the workspace: {entry.Path}");
            if (IsBinaryLikePath(cleanedRelative))
                throw new InvalidOperationException($"Binary outputs are not supported for automatic application: {entry.Path}");
            if (!allowOverwrite && File.Exists(fullPath))
                throw new InvalidOperationException($"Refusing to overwrite existing file without --allow-overwrite: {entry.Path}");

            normalized.Add((cleanedRelative, fullPath, entry.Content));
        }

        foreach (var item in normalized)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(item.FullPath)!);
            File.WriteAllText(item.FullPath, item.Content);
        }

        return normalized.Select(item => item.RelativePath).ToList();
    }

    private static GeneratedFilePlan ParseGeneratedFilePlan(string content)
    {
        var json = ExtractJsonObject(content);
        var normalizedJson = NormalizeLooseJsonStrings(json);
        var plan = JsonSerializer.Deserialize<GeneratedFilePlan>(normalizedJson, JsonOptions);
        if (plan is null)
            throw new InvalidOperationException("Generated file plan could not be parsed.");

        return plan;
    }

    private static string ExtractJsonObject(string content)
    {
        var trimmed = content.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstBrace = trimmed.IndexOf('{');
            var lastBrace = trimmed.LastIndexOf('}');
            if (firstBrace >= 0 && lastBrace > firstBrace)
                trimmed = trimmed[firstBrace..(lastBrace + 1)];
        }

        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            return trimmed;

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
            return trimmed[start..(end + 1)];

        throw new InvalidOperationException("Generated response did not contain a JSON object.");
    }

    private static string NormalizeLooseJsonStrings(string json)
    {
        var builder = new StringBuilder(json.Length);
        var insideString = false;
        var escaping = false;

        for (var index = 0; index < json.Length; index++)
        {
            var ch = json[index];

            if (!insideString)
            {
                builder.Append(ch);
                if (ch == '"')
                    insideString = true;

                continue;
            }

            if (escaping)
            {
                builder.Append(ch);
                escaping = false;
                continue;
            }

            if (ch == '\\')
            {
                builder.Append(ch);
                escaping = true;
                continue;
            }

            if (ch == '"')
            {
                builder.Append(ch);
                insideString = false;
                continue;
            }

            if (ch == '\r')
            {
                if (index + 1 < json.Length && json[index + 1] == '\n')
                    index++;

                builder.Append("\\n");
                continue;
            }

            if (ch == '\n')
            {
                builder.Append("\\n");
                continue;
            }

            if (char.IsControl(ch))
            {
                builder.Append($"\\u{(int)ch:x4}");
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    private static bool IsBinaryLikePath(string relativePath)
    {
        var extension = Path.GetExtension(relativePath);
        return extension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".gif", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".ico", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateRunDirectory(string ticketDir, string role)
    {
        var safeRole = new string(role.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray()).Trim('-');
        if (string.IsNullOrWhiteSpace(safeRole))
            safeRole = "agent";

        var runDirectory = Path.Combine(
            ticketDir,
            "runs",
            $"{DateTime.UtcNow:yyyyMMddTHHmmssfffZ}-{safeRole}");
        Directory.CreateDirectory(runDirectory);
        return runDirectory;
    }
}

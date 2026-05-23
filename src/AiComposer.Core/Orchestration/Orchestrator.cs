using System.Text.Json;
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
    {
        var graph = MarkdownLoader.LoadArtifacts(artifactsDir);
        Directory.CreateDirectory(outputDir);
        var processedIds = new List<string>();

        foreach (var node in graph.Values.Where(n => n.Type == "ticket"))
        {
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
}

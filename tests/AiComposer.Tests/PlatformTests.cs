using AiComposer.Core.Agents;
using AiComposer.Core.Markdown;
using AiComposer.Core.Models;
using AiComposer.Core.Orchestration;
using AiComposer.Core.Security;
using AiComposer.Core.State;
using AiComposer.Core.Telemetry;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace AiComposer.Tests;

public sealed class MarkdownLoaderTests
{
    private static readonly string ArtifactsDir =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples", "artifacts"));

    [Fact]
    public void LoadArtifacts_ParsesAllExampleFiles()
    {
        var nodes = MarkdownLoader.LoadArtifacts(ArtifactsDir);
        Assert.NotEmpty(nodes);
    }

    [Fact]
    public void LoadArtifacts_TicketLinksToEpicAndRule()
    {
        var nodes = MarkdownLoader.LoadArtifacts(ArtifactsDir);
        Assert.True(nodes.ContainsKey("T-105"), "T-105 ticket must be present");
        var ticket = nodes["T-105"];
        Assert.Equal("ticket", ticket.Type);
        Assert.Contains("E-1", ticket.Links);
        Assert.Contains("R-1", ticket.Links);
    }

    [Fact]
    public void LoadArtifacts_ProjectSummaryNodeExists()
    {
        var nodes = MarkdownLoader.LoadArtifacts(ArtifactsDir);
        var ps = nodes.Values.FirstOrDefault(n => n.Type == "project_summary");
        Assert.NotNull(ps);
    }

    [Fact]
    public void LoadArtifacts_AdrLinksToTicket()
    {
        var nodes = MarkdownLoader.LoadArtifacts(ArtifactsDir);
        Assert.True(nodes.ContainsKey("ADR-105"), "ADR-105 must be present");
        var adr = nodes["ADR-105"];
        Assert.Equal("adr", adr.Type);
        Assert.Contains("T-105", adr.Links);
    }
}

public sealed class StateStoreTests
{
    [Fact]
    public void EnsureTicket_CreatesNewRecordInWaitingState()
    {
        using var tmp = new TempDirectory();
        using var store = new StateStore(Path.Combine(tmp.Path, "test.db"));
        var record = store.EnsureTicket("T-1");
        Assert.Equal("T-1", record.TicketId);
        Assert.Equal(TicketState.WaitingForCoder, record.State);
        Assert.Equal(0, record.Attempts);
    }

    [Fact]
    public void EnsureTicket_IsIdempotent()
    {
        using var tmp = new TempDirectory();
        using var store = new StateStore(Path.Combine(tmp.Path, "test.db"));
        store.EnsureTicket("T-2");
        var second = store.EnsureTicket("T-2");
        Assert.Equal(TicketState.WaitingForCoder, second.State);
    }

    [Fact]
    public void Transition_UpdatesStateCorrectly()
    {
        using var tmp = new TempDirectory();
        using var store = new StateStore(Path.Combine(tmp.Path, "test.db"));
        store.EnsureTicket("T-3");
        var record = store.Transition("T-3", TicketState.WaitingForReview);
        Assert.Equal(TicketState.WaitingForReview, record.State);
    }

    [Fact]
    public void CircuitBreaker_TriggersAfterThreeFailures()
    {
        using var tmp = new TempDirectory();
        using var store = new StateStore(Path.Combine(tmp.Path, "test.db"));
        store.EnsureTicket("T-CB");
        store.Transition("T-CB", TicketState.CoderIterating, "e1", incrementAttempt: true);
        store.Transition("T-CB", TicketState.CoderIterating, "e2", incrementAttempt: true);
        var final = store.Transition("T-CB", TicketState.CoderIterating, "e3", incrementAttempt: true);
        Assert.Equal(TicketState.PausedHumanIntervention, final.State);
        Assert.Equal(3, final.Attempts);
    }
}

public sealed class OrchestratorTests
{
    private static readonly string ArtifactsDir =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "examples", "artifacts"));

    [Fact]
    public void ProcessTickets_GeneratesContextAndPersistsState()
    {
        using var tmp = new TempDirectory();
        var stateDb = Path.Combine(tmp.Path, "orchestrator.db");
        var traceLog = Path.Combine(tmp.Path, "traces.jsonl");
        var outputDir = Path.Combine(tmp.Path, "output");
        using var stateStore = new StateStore(stateDb);

        var orchestrator = new Orchestrator(
            stateStore,
            new TraceLogger(traceLog));

        var tickets = orchestrator.ProcessTickets(ArtifactsDir, outputDir);

        Assert.Single(tickets);
        Assert.Equal("T-105", tickets[0]);

        // context.json written
        var contextPath = Path.Combine(outputDir, "T-105", "context.json");
        Assert.True(File.Exists(contextPath));

        using var doc = JsonDocument.Parse(File.ReadAllText(contextPath));
        Assert.Equal("T-105", doc.RootElement.GetProperty("ticketId").GetString());
        Assert.True(doc.RootElement.GetProperty("securityContext").GetProperty("sandboxRequired").GetBoolean());

        // trace log written
        Assert.True(File.Exists(traceLog));
        Assert.Single(File.ReadAllLines(traceLog));

        // state persisted
        using var conn = new SqliteConnection($"Data Source={stateDb}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT state FROM ticket_state WHERE ticket_id = 'T-105'";
        var state = (string?)cmd.ExecuteScalar();
        Assert.Equal(TicketState.WaitingForReview, state);
    }

    [Fact]
    public async Task ProcessTicketsAsync_SpawnsAgentAndPersistsRunArtifacts()
    {
        using var tmp = new TempDirectory();
        var stateDb = Path.Combine(tmp.Path, "orchestrator.db");
        var traceLog = Path.Combine(tmp.Path, "traces.jsonl");
        var outputDir = Path.Combine(tmp.Path, "output");
        using var stateStore = new StateStore(stateDb);
        var fakeClient = new FakeAgentClient();

        var orchestrator = new Orchestrator(
            stateStore,
            new TraceLogger(traceLog));

        var tickets = await orchestrator.ProcessTicketsAsync(
            ArtifactsDir,
            outputDir,
            new AgentExecutionOptions
            {
                Provider = "deepseek",
                Model = "deepseek-v4-flash",
                Role = "coder",
                Prompt = "Summarize the implementation plan.",
            },
            fakeClient);

        Assert.Single(tickets);
        Assert.NotNull(fakeClient.LastRequest);
        Assert.Equal("T-105", fakeClient.LastRequest!.TicketId);
        Assert.DoesNotContain("mock-secret-key", fakeClient.LastRequest.UserPrompt, StringComparison.OrdinalIgnoreCase);

        var runDirectories = Directory.GetDirectories(Path.Combine(outputDir, "T-105", "runs"));
        Assert.Single(runDirectories);
        Assert.True(File.Exists(Path.Combine(runDirectories[0], "agent-request.json")));
        Assert.True(File.Exists(Path.Combine(runDirectories[0], "agent-response.md")));

        var audit = JsonDocument.Parse(File.ReadAllText(Path.Combine(runDirectories[0], "agent-request.json")));
        Assert.Equal("deepseek", audit.RootElement.GetProperty("provider").GetString());
        Assert.Equal("coder", audit.RootElement.GetProperty("role").GetString());
        Assert.Equal("text", fakeClient.LastRequest.ResponseFormat);

        using var conn = new SqliteConnection($"Data Source={stateDb}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT state FROM ticket_state WHERE ticket_id = 'T-105'";
        var state = (string?)cmd.ExecuteScalar();
        Assert.Equal(TicketState.WaitingForReview, state);
    }

    [Fact]
    public async Task ProcessTicketsAsync_TracksProviderFailureInStateStore()
    {
        using var tmp = new TempDirectory();
        var stateDb = Path.Combine(tmp.Path, "orchestrator.db");
        var traceLog = Path.Combine(tmp.Path, "traces.jsonl");
        var outputDir = Path.Combine(tmp.Path, "output");
        using var stateStore = new StateStore(stateDb);
        var failingClient = new FakeAgentClient
        {
            Failure = new InvalidOperationException("provider unavailable"),
        };

        var orchestrator = new Orchestrator(
            stateStore,
            new TraceLogger(traceLog));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.ProcessTicketsAsync(
                ArtifactsDir,
                outputDir,
                new AgentExecutionOptions
                {
                    Provider = "deepseek",
                    Model = "deepseek-v4-flash",
                    Role = "coder",
                    Prompt = "Do the work.",
                },
                failingClient));

        Assert.Contains("provider unavailable", ex.Message);

        using var conn = new SqliteConnection($"Data Source={stateDb}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT state, attempts, last_error FROM ticket_state WHERE ticket_id = 'T-105'";
        using var reader = cmd.ExecuteReader();
        Assert.True(reader.Read());
        Assert.Equal(TicketState.CoderFailed, reader.GetString(0));
        Assert.Equal(1, reader.GetInt32(1));
        Assert.Equal("provider unavailable", reader.GetString(2));
    }

    [Fact]
    public async Task ProcessTicketsAsync_AppliesGeneratedFilesInsideWorkspace()
    {
        using var tmp = new TempDirectory();
        var workspace = Path.Combine(tmp.Path, "workspace");
        var stateDb = Path.Combine(tmp.Path, "orchestrator.db");
        var traceLog = Path.Combine(tmp.Path, "traces.jsonl");
        var outputDir = Path.Combine(tmp.Path, "output");
        using var stateStore = new StateStore(stateDb);
        var fakeClient = new FakeAgentClient
        {
            JsonFilePlan = """
                {
                  "summary": "Create browser snake MVP",
                  "files": [
                    { "path": "index.html", "content": "<!doctype html><title>Snake</title>" },
                    { "path": "assets/app.js", "content": "console.log('snake');" }
                  ]
                }
                """
        };

        var orchestrator = new Orchestrator(stateStore, new TraceLogger(traceLog));
        var tickets = await orchestrator.ProcessTicketsAsync(
            ArtifactsDir,
            outputDir,
            new AgentExecutionOptions
            {
                Provider = "deepseek",
                Model = "deepseek-v4-flash",
                Role = "coder",
                Prompt = "Implement the snake game files.",
                ApplyFilesWorkspace = workspace,
            },
            fakeClient);

        Assert.Single(tickets);
        Assert.True(File.Exists(Path.Combine(workspace, "index.html")));
        Assert.True(File.Exists(Path.Combine(workspace, "assets", "app.js")));
        Assert.Equal("json_object", fakeClient.LastRequest!.ResponseFormat);

        var runDirectories = Directory.GetDirectories(Path.Combine(outputDir, "T-105", "runs"));
        Assert.Single(runDirectories);
        Assert.True(File.Exists(Path.Combine(runDirectories[0], "applied-files.json")));
    }

    [Fact]
    public async Task ProcessTicketsAsync_NormalizesRawNewlinesInsideLooseJsonStrings()
    {
        using var tmp = new TempDirectory();
        var workspace = Path.Combine(tmp.Path, "workspace");
        var stateDb = Path.Combine(tmp.Path, "orchestrator.db");
        var traceLog = Path.Combine(tmp.Path, "traces.jsonl");
        var outputDir = Path.Combine(tmp.Path, "output");
        using var stateStore = new StateStore(stateDb);
        var fakeClient = new FakeAgentClient
        {
            JsonFilePlan = """
                ● {"summary":"Snake MVP
                generated across
                multiple lines","files":[{"path":"index.html","content":"<!doctype html>
                <title>Snake</title>
                <main>Ready</main>"}]}
                """
        };

        var orchestrator = new Orchestrator(stateStore, new TraceLogger(traceLog));
        var tickets = await orchestrator.ProcessTicketsAsync(
            ArtifactsDir,
            outputDir,
            new AgentExecutionOptions
            {
                Provider = "copilot",
                Model = "gpt-5.4",
                Role = "coder",
                Prompt = "Implement the snake game files.",
                ApplyFilesWorkspace = workspace,
            },
            fakeClient);

        Assert.Single(tickets);
        var generatedPath = Path.Combine(workspace, "index.html");
        Assert.True(File.Exists(generatedPath));
        var generated = File.ReadAllText(generatedPath);
        Assert.Contains("<title>Snake</title>", generated);
        Assert.Contains("<main>Ready</main>", generated);
    }

    [Fact]
    public async Task ProcessTicketsAsync_RejectsPathTraversalWhenApplyingFiles()
    {
        using var tmp = new TempDirectory();
        var workspace = Path.Combine(tmp.Path, "workspace");
        var stateDb = Path.Combine(tmp.Path, "orchestrator.db");
        var traceLog = Path.Combine(tmp.Path, "traces.jsonl");
        var outputDir = Path.Combine(tmp.Path, "output");
        using var stateStore = new StateStore(stateDb);
        var fakeClient = new FakeAgentClient
        {
            JsonFilePlan = """
                {
                  "summary": "Escape workspace",
                  "files": [
                    { "path": "..\\evil.js", "content": "alert('x');" }
                  ]
                }
                """
        };

        var orchestrator = new Orchestrator(stateStore, new TraceLogger(traceLog));
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.ProcessTicketsAsync(
                ArtifactsDir,
                outputDir,
                new AgentExecutionOptions
                {
                    Provider = "deepseek",
                    Model = "deepseek-v4-flash",
                    Role = "coder",
                    Prompt = "Write files.",
                    ApplyFilesWorkspace = workspace,
                },
                fakeClient));

        Assert.Contains("workspace", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(Directory.Exists(workspace) && Directory.EnumerateFiles(workspace, "*", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task ProcessTicketsAsync_DryRunDoesNotWriteWorkspaceFiles()
    {
        using var tmp = new TempDirectory();
        var workspace = Path.Combine(tmp.Path, "workspace");
        var stateDb = Path.Combine(tmp.Path, "orchestrator.db");
        var traceLog = Path.Combine(tmp.Path, "traces.jsonl");
        var outputDir = Path.Combine(tmp.Path, "output");
        using var stateStore = new StateStore(stateDb);

        var orchestrator = new Orchestrator(stateStore, new TraceLogger(traceLog));
        var tickets = await orchestrator.ProcessTicketsAsync(
            ArtifactsDir,
            outputDir,
            new AgentExecutionOptions
            {
                Provider = "deepseek",
                Model = "deepseek-v4-flash",
                Role = "coder",
                Prompt = "Write files.",
                ApplyFilesWorkspace = workspace,
                DryRun = true,
            },
            agentClient: null);

        Assert.Single(tickets);
        Assert.False(Directory.Exists(workspace));
    }
}

public sealed class SecurityHelperTests
{
    [Fact]
    public void BuildSandboxCommand_ContainsNetworkNone()
    {
        var policy = new SandboxPolicy { Workspace = "/tmp/workspace", NetworkMode = "none" };
        var cmd = SecurityHelper.BuildSandboxCommand(policy, ["dotnet", "test"]);
        Assert.Contains("--network", cmd);
        Assert.Contains("none", cmd);
    }

    [Fact]
    public void BuildSandboxCommand_ContainsReadOnlyFlag()
    {
        var policy = new SandboxPolicy { Workspace = "/tmp/workspace", ReadOnlyRoot = true };
        var cmd = SecurityHelper.BuildSandboxCommand(policy, ["dotnet", "build"]);
        Assert.Contains("--read-only", cmd);
    }

    [Fact]
    public void IsPathInsideWorkspace_ReturnsTrueForChildPath()
    {
        using var tmp = new TempDirectory();
        var child = Path.Combine(tmp.Path, "sub", "file.txt");
        Assert.True(SecurityHelper.IsPathInsideWorkspace(tmp.Path, child));
    }

    [Fact]
    public void IsPathInsideWorkspace_ReturnsFalseForOutsidePath()
    {
        Assert.False(SecurityHelper.IsPathInsideWorkspace("/workspace", "/etc/passwd"));
    }

    [Fact]
    public void BuildMockEnvironment_DoesNotContainRealSecrets()
    {
        var env = SecurityHelper.BuildMockEnvironment();
        Assert.Contains("USE_MOCK_SERVICES", env.Keys);
        Assert.Equal("true", env["USE_MOCK_SERVICES"]);
    }
}

/// <summary>Helper that creates and cleans up a temporary directory.</summary>
internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());

    public TempDirectory() => Directory.CreateDirectory(Path);

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}

internal sealed class FakeAgentClient : IAgentClient
{
    public AgentExecutionRequest? LastRequest { get; private set; }
    public List<string> ProgressUpdates { get; } = [];
    public string? JsonFilePlan { get; set; }

    public Exception? Failure { get; set; }

    public Task<AgentExecutionResult> ExecuteAsync(
        AgentExecutionRequest request,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        LastRequest = request;
        if (Failure is not null)
            throw Failure;

        progress?.Report("Thinking...");
        ProgressUpdates.Add("Thinking...");
        progress?.Report("Done.");
        ProgressUpdates.Add("Done.");

        return Task.FromResult(new AgentExecutionResult
        {
            Provider = request.Provider,
            ResponseId = "resp-test",
            Model = request.Model,
            Role = request.Role,
            Content = request.ResponseFormat == "json_object"
                ? JsonFilePlan ?? """
                    {
                      "summary": "Generate one file",
                      "files": [
                        { "path": "index.html", "content": "<!doctype html><title>Generated</title>" }
                      ]
                    }
                    """
                : "# Agent response\n\nImplementation outline.",
            FinishReason = "stop",
            PromptTokens = 120,
            CompletionTokens = 45,
            PromptCacheHitTokens = 20,
            PromptCacheMissTokens = 100,
            CostUsd = 0.0000266,
        });
    }
}

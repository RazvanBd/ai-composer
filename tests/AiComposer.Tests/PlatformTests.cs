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
        var store = new StateStore(Path.Combine(tmp.Path, "test.db"));
        var record = store.EnsureTicket("T-1");
        Assert.Equal("T-1", record.TicketId);
        Assert.Equal(TicketState.WaitingForCoder, record.State);
        Assert.Equal(0, record.Attempts);
    }

    [Fact]
    public void EnsureTicket_IsIdempotent()
    {
        using var tmp = new TempDirectory();
        var store = new StateStore(Path.Combine(tmp.Path, "test.db"));
        store.EnsureTicket("T-2");
        var second = store.EnsureTicket("T-2");
        Assert.Equal(TicketState.WaitingForCoder, second.State);
    }

    [Fact]
    public void Transition_UpdatesStateCorrectly()
    {
        using var tmp = new TempDirectory();
        var store = new StateStore(Path.Combine(tmp.Path, "test.db"));
        store.EnsureTicket("T-3");
        var record = store.Transition("T-3", TicketState.WaitingForReview);
        Assert.Equal(TicketState.WaitingForReview, record.State);
    }

    [Fact]
    public void CircuitBreaker_TriggersAfterThreeFailures()
    {
        using var tmp = new TempDirectory();
        var store = new StateStore(Path.Combine(tmp.Path, "test.db"));
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

        var orchestrator = new Orchestrator(
            new StateStore(stateDb),
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
        if (Directory.Exists(Path))
            Directory.Delete(Path, recursive: true);
    }
}

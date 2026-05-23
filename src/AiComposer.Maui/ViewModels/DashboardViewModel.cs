using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.ViewModels;

/// <summary>ViewModel for the Dashboard page — overview and quick actions.</summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IArtifactsService _artifactsService;
    private readonly ITicketService _ticketService;

    [ObservableProperty]
    private string _workspacePath = "No workspace open";

    [ObservableProperty]
    private int _artifactCount;

    [ObservableProperty]
    private int _ticketCount;

    [ObservableProperty]
    private int _ticketsReady;

    [ObservableProperty]
    private int _ticketsDone;

    [ObservableProperty]
    private int _ticketsBlocked;

    [ObservableProperty]
    private int _ticketsRunning;

    [ObservableProperty]
    private string _lastRunSummary = "No runs yet";

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Initialises <see cref="DashboardViewModel"/>.</summary>
    public DashboardViewModel(
        IWorkspaceService workspaceService,
        IArtifactsService artifactsService,
        ITicketService ticketService)
    {
        _workspaceService = workspaceService;
        _artifactsService = artifactsService;
        _ticketService = ticketService;
    }

    /// <summary>Opens a workspace folder and refreshes the dashboard summary.</summary>
    [RelayCommand]
    private async Task OpenWorkspaceAsync()
    {
        var path = await _workspaceService.OpenWorkspaceAsync();
        if (path is not null)
        {
            WorkspacePath = path;
            await RefreshAsync();
        }
    }

    /// <summary>Validates all artifacts in the current workspace and updates their status.</summary>
    [RelayCommand]
    private async Task ValidateArtifactsAsync()
    {
        if (string.IsNullOrWhiteSpace(_workspaceService.CurrentWorkspacePath))
        {
            await Shell.Current.DisplayAlert("No Workspace", "Open a workspace before validating.", "OK");
            return;
        }

        IsLoading = true;
        try
        {
            var results = await _artifactsService.ValidateAsync();
            var invalid = results.Count(a => a.ValidationStatus == "Invalid");
            var valid = results.Count(a => a.ValidationStatus == "Valid");
            await Shell.Current.DisplayAlert(
                "Validation Complete",
                $"{valid} valid, {invalid} invalid out of {results.Count} artifact(s).",
                "OK");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Refreshes artifact and ticket counts from the current workspace.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsLoading = true;
        try
        {
            var artifacts = await _artifactsService.LoadArtifactsAsync();
            var tickets = await _ticketService.LoadTicketsAsync();
            ArtifactCount = artifacts.Count;
            TicketCount = tickets.Count;
            TicketsReady   = tickets.Count(t => string.Equals(t.State, "ready",   StringComparison.OrdinalIgnoreCase));
            TicketsDone    = tickets.Count(t => string.Equals(t.State, "done",    StringComparison.OrdinalIgnoreCase));
            TicketsBlocked = tickets.Count(t => string.Equals(t.State, "blocked", StringComparison.OrdinalIgnoreCase)
                                             || string.Equals(t.State, "paused_human_intervention", StringComparison.OrdinalIgnoreCase));
            TicketsRunning = tickets.Count(t => string.Equals(t.State, "running", StringComparison.OrdinalIgnoreCase));
            WorkspacePath = _workspaceService.CurrentWorkspacePath ?? "No workspace open";
            LastRunSummary = ResolveLastRunSummary(_workspaceService.CurrentWorkspacePath);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string ResolveLastRunSummary(string? workspacePath)
    {
        if (string.IsNullOrWhiteSpace(workspacePath))
            return "No runs yet";

        var tracesPath = Path.Combine(workspacePath, ".state", "traces.jsonl");
        if (File.Exists(tracesPath))
        {
            var lastWrite = File.GetLastWriteTime(tracesPath);
            return $"Last run: {lastWrite:yyyy-MM-dd HH:mm}";
        }

        var outputDir = Path.Combine(workspacePath, "output");
        if (Directory.Exists(outputDir))
        {
            var latest = Directory
                .EnumerateFiles(outputDir, "*", SearchOption.AllDirectories)
                .Select(f => File.GetLastWriteTime(f))
                .DefaultIfEmpty()
                .Max();

            if (latest != default)
                return $"Last run: {latest:yyyy-MM-dd HH:mm}";
        }

        return "No runs yet";
    }
}

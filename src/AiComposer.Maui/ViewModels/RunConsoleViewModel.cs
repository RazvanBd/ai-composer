using System.Collections.ObjectModel;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.ViewModels;

/// <summary>ViewModel for the Run Console page — live output streaming and run control.</summary>
public sealed partial class RunConsoleViewModel : ObservableObject, IQueryAttributable
{
    private readonly IRunService _runService;
    private readonly ITicketService _ticketService;
    private readonly IOutputService _outputService;
    private readonly ISettingsService _settingsService;
    private TaskCompletionSource<int>? _activeRunCompletionSource;
    private bool _stopRequested;

    [ObservableProperty]
    private ObservableCollection<string> _outputLines = [];

    [ObservableProperty]
    private ObservableCollection<AppliedFileItem> _appliedFiles = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyCanExecuteChangedFor(nameof(StartRunCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopRunCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunAllReadyCommand))]
    [NotifyCanExecuteChangedFor(nameof(OpenInViewerCommand))]
    private RunStatus _runStatus = RunStatus.Idle;

    [ObservableProperty]
    private string _ticketId = string.Empty;

    [ObservableProperty]
    private string _selectedTicketTitle = string.Empty;

    [ObservableProperty]
    private int _totalReadyTickets;

    [ObservableProperty]
    private int _currentTicketIndex;

    [ObservableProperty]
    private string _batchRunSummary = string.Empty;

    [ObservableProperty]
    private string _batchProgressText = string.Empty;

    [ObservableProperty]
    private string _auditStartTimestamp = "-";

    [ObservableProperty]
    private string _auditEndTimestamp = "-";

    [ObservableProperty]
    private string _auditDuration = "-";

    [ObservableProperty]
    private string _auditTicket = "-";

    [ObservableProperty]
    private string _auditFinalStatus = "-";

    [ObservableProperty]
    private string _auditPromptSummary = "Prompt metadata unavailable";

    [ObservableProperty]
    private string _auditTokenSummary = "Token usage unavailable";

    [ObservableProperty]
    private int _auditAppliedFilesCount;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OpenInViewerCommand))]
    private AppliedFileItem? _selectedAppliedFile;

    /// <summary>Gets whether a run is currently in progress.</summary>
    public bool IsRunning => RunStatus == RunStatus.Running;

    /// <summary>Initialises <see cref="RunConsoleViewModel"/> and subscribes to run events.</summary>
    public RunConsoleViewModel(
        IRunService runService,
        ITicketService ticketService,
        IOutputService outputService,
        ISettingsService settingsService)
    {
        _runService = runService;
        _ticketService = ticketService;
        _outputService = outputService;
        _settingsService = settingsService;
        _runService.OutputReceived += OnOutputReceived;
        _runService.RunCompleted += OnRunCompleted;
    }

    /// <inheritdoc/>
    void IQueryAttributable.ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("ticketId", out var id))
            TicketId = id?.ToString() ?? string.Empty;
        if (query.TryGetValue("ticketTitle", out var title))
            SelectedTicketTitle = title?.ToString() ?? string.Empty;

        // If a run was already started externally (e.g., from the Tickets page), reflect that.
        if (_runService.IsRunning && RunStatus == RunStatus.Idle)
            RunStatus = RunStatus.Running;
    }

    /// <summary>Starts a CLI run for the configured ticket ID.</summary>
    [RelayCommand(CanExecute = nameof(CanStartRun))]
    private async Task StartRunAsync()
    {
        _stopRequested = false;
        BatchRunSummary = string.Empty;
        TotalReadyTickets = 0;
        CurrentTicketIndex = 0;
        BatchProgressText = string.Empty;
        await ExecuteRunAsync(TicketId, SelectedTicketTitle);
    }

    /// <summary>Runs all tickets currently marked as ready in sequential order.</summary>
    [RelayCommand(CanExecute = nameof(CanRunAllReady))]
    private async Task RunAllReadyAsync()
    {
        var readyTickets = (await _ticketService.LoadTicketsAsync())
            .Where(t => string.Equals(t.State, "ready", StringComparison.OrdinalIgnoreCase))
            .ToList();

        TotalReadyTickets = readyTickets.Count;
        CurrentTicketIndex = 0;
        BatchRunSummary = string.Empty;
        BatchProgressText = string.Empty;
        _stopRequested = false;

        if (readyTickets.Count == 0)
        {
            BatchRunSummary = "No ready tickets found.";
            return;
        }

        var completed = 0;
        var failed = 0;
        var stopped = 0;

        for (var i = 0; i < readyTickets.Count; i++)
        {
            if (_stopRequested)
                break;

            var ticket = readyTickets[i];
            CurrentTicketIndex = i + 1;
            TicketId = ticket.Id;
            SelectedTicketTitle = ticket.Title;
            BatchProgressText = $"Running ticket {CurrentTicketIndex} of {TotalReadyTickets}: {ticket.Title}";

            var finalStatus = await ExecuteRunAsync(ticket.Id, ticket.Title);
            if (finalStatus == RunStatus.Completed)
            {
                completed++;
                continue;
            }

            if (finalStatus == RunStatus.Stopped)
            {
                stopped = TotalReadyTickets - completed - failed;
                break;
            }

            failed++;
        }

        if (_stopRequested && stopped == 0)
            stopped = TotalReadyTickets - completed - failed;

        if (stopped < 0)
            stopped = 0;

        BatchProgressText = string.Empty;
        BatchRunSummary = $"{completed} completed, {failed} failed, {stopped} stopped";
    }

    /// <summary>Stops the currently running CLI process.</summary>
    [RelayCommand(CanExecute = nameof(CanStopRun))]
    private void StopRun()
    {
        _stopRequested = true;
        _runService.StopRun();
        RunStatus = RunStatus.Stopped;
    }

    /// <summary>Clears all output lines.</summary>
    [RelayCommand]
    private void ClearOutput() => OutputLines.Clear();

    /// <summary>Opens the selected applied file in the Workspace Viewer page.</summary>
    [RelayCommand(CanExecute = nameof(CanOpenInViewer))]
    private Task OpenInViewerAsync()
    {
        if (SelectedAppliedFile is null || string.IsNullOrWhiteSpace(TicketId))
            return Task.CompletedTask;

        var route = $"//workspaceviewer?ticketId={Uri.EscapeDataString(TicketId)}&relativePath={Uri.EscapeDataString(SelectedAppliedFile.RelativePath)}";
        return Shell.Current.GoToAsync(route);
    }

    private bool CanStartRun() => !IsRunning && !string.IsNullOrWhiteSpace(TicketId);
    private bool CanStopRun() => IsRunning;
    private bool CanRunAllReady() => !IsRunning;
    private bool CanOpenInViewer() => SelectedAppliedFile is not null && !string.IsNullOrWhiteSpace(TicketId);

    private void OnOutputReceived(object? sender, string line)
    {
        MainThread.BeginInvokeOnMainThread(() => OutputLines.Add(line));
    }

    private void OnRunCompleted(object? sender, int exitCode)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Preserve Stopped if the user explicitly stopped the run.
            if (RunStatus != RunStatus.Stopped)
                RunStatus = exitCode == 0 ? RunStatus.Completed : RunStatus.Failed;
            OutputLines.Add($"[Process exited with code {exitCode}]");
            _activeRunCompletionSource?.TrySetResult(exitCode);
        });
    }

    private async Task<RunStatus> ExecuteRunAsync(string ticketId, string ticketTitle)
    {
        if (string.IsNullOrWhiteSpace(ticketId))
            return RunStatus.Idle;

        var startedAt = DateTimeOffset.Now;
        AuditStartTimestamp = startedAt.ToString("yyyy-MM-dd HH:mm:ss");
        AuditTicket = string.IsNullOrWhiteSpace(ticketTitle) ? ticketId : $"{ticketId} — {ticketTitle}";
        AuditFinalStatus = "Running";
        AuditEndTimestamp = "-";
        AuditDuration = "-";
        AuditPromptSummary = "Prompt metadata unavailable";
        AuditTokenSummary = "Token usage unavailable";
        AuditAppliedFilesCount = 0;
        SelectedAppliedFile = null;

        var filesBeforeRun = (await _outputService.ListGeneratedFilesAsync(ticketId))
            .ToDictionary(f => f.RelativePath, f => f.LastModified);

        OutputLines.Clear();
        AppliedFiles.Clear();
        RunStatus = RunStatus.Running;
        _activeRunCompletionSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            await _runService.StartRunAsync(ticketId);
            await _activeRunCompletionSource.Task;
        }
        catch (Exception ex)
        {
            RunStatus = RunStatus.Failed;
            OutputLines.Add($"[Error] {ex.Message}");
        }
        finally
        {
            var endedAt = DateTimeOffset.Now;
            AuditEndTimestamp = endedAt.ToString("yyyy-MM-dd HH:mm:ss");
            AuditDuration = $"{(endedAt - startedAt).TotalSeconds:F1}s";
            await RefreshAppliedFilesAndAuditAsync(ticketId, filesBeforeRun, startedAt, endedAt, ticketTitle);
            AuditFinalStatus = RunStatus.ToString();
            _activeRunCompletionSource = null;
        }

        return RunStatus;
    }

    private async Task RefreshAppliedFilesAndAuditAsync(
        string ticketId,
        Dictionary<string, DateTimeOffset> filesBeforeRun,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        string ticketTitle)
    {
        var filesAfterRun = await _outputService.ListGeneratedFilesAsync(ticketId);
        var changed = filesAfterRun
            .Where(file =>
                !filesBeforeRun.TryGetValue(file.RelativePath, out var previousModified)
                || file.LastModified > previousModified)
            .Select(file => new AppliedFileItem
            {
                RelativePath = file.RelativePath,
                FullPath = file.FullPath,
                ChangeKind = filesBeforeRun.ContainsKey(file.RelativePath) ? "Modified" : "New",
            })
            .OrderBy(file => file.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        AppliedFiles = new ObservableCollection<AppliedFileItem>(changed);
        AuditAppliedFilesCount = changed.Count;

        var settings = await _settingsService.LoadAsync();
        var outputRoot = settings.OutputPath;
        if (!string.IsNullOrWhiteSpace(outputRoot))
        {
            var runsRoot = Path.Combine(outputRoot, ticketId, "runs");
            if (Directory.Exists(runsRoot))
            {
                var latestRunDirectory = new DirectoryInfo(runsRoot)
                    .EnumerateDirectories()
                    .OrderByDescending(dir => dir.LastWriteTimeUtc)
                    .FirstOrDefault();

                if (latestRunDirectory is not null)
                {
                    var auditFile = Path.Combine(latestRunDirectory.FullName, "agent-request.json");
                    if (File.Exists(auditFile))
                        PopulateAuditFromFile(auditFile);
                }
            }
        }

        if (AuditPromptSummary == "Prompt metadata unavailable")
            AuditPromptSummary = $"Run window: {startedAt:HH:mm:ss} - {endedAt:HH:mm:ss}";

        AuditTicket = string.IsNullOrWhiteSpace(ticketTitle) ? ticketId : $"{ticketId} — {ticketTitle}";
    }

    private void PopulateAuditFromFile(string auditFilePath)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(auditFilePath));
            var root = document.RootElement;

            if (root.TryGetProperty("createdAt", out var createdAtElement))
                AuditStartTimestamp = createdAtElement.GetString() ?? AuditStartTimestamp;

            var promptLength = root.TryGetProperty("promptLength", out var promptLengthElement)
                ? promptLengthElement.GetInt32()
                : 0;
            AuditPromptSummary = promptLength > 0
                ? $"Prompt sent: {promptLength} chars"
                : "Prompt metadata unavailable";

            var promptTokens = root.TryGetProperty("promptTokens", out var promptTokensElement)
                ? promptTokensElement.GetInt32()
                : 0;
            var completionTokens = root.TryGetProperty("completionTokens", out var completionTokensElement)
                ? completionTokensElement.GetInt32()
                : 0;
            AuditTokenSummary = (promptTokens + completionTokens) > 0
                ? $"Tokens: prompt {promptTokens}, completion {completionTokens}"
                : "Token usage unavailable";
        }
        catch
        {
            AuditPromptSummary = "Prompt metadata unavailable";
            AuditTokenSummary = "Token usage unavailable";
        }
    }
}

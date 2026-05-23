using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.ViewModels;

/// <summary>ViewModel for the Run Console page — live output streaming and run control.</summary>
public sealed partial class RunConsoleViewModel : ObservableObject, IQueryAttributable
{
    private readonly IRunService _runService;

    [ObservableProperty]
    private ObservableCollection<string> _outputLines = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRunning))]
    [NotifyCanExecuteChangedFor(nameof(StartRunCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopRunCommand))]
    private RunStatus _runStatus = RunStatus.Idle;

    [ObservableProperty]
    private string _ticketId = string.Empty;

    [ObservableProperty]
    private string _selectedTicketTitle = string.Empty;

    /// <summary>Gets whether a run is currently in progress.</summary>
    public bool IsRunning => RunStatus == RunStatus.Running;

    /// <summary>Initialises <see cref="RunConsoleViewModel"/> and subscribes to run events.</summary>
    public RunConsoleViewModel(IRunService runService)
    {
        _runService = runService;
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
        OutputLines.Clear();
        RunStatus = RunStatus.Running;
        await _runService.StartRunAsync(TicketId);
    }

    /// <summary>Stops the currently running CLI process.</summary>
    [RelayCommand(CanExecute = nameof(CanStopRun))]
    private void StopRun()
    {
        _runService.StopRun();
        RunStatus = RunStatus.Stopped;
    }

    /// <summary>Clears all output lines.</summary>
    [RelayCommand]
    private void ClearOutput() => OutputLines.Clear();

    private bool CanStartRun() => !IsRunning && !string.IsNullOrWhiteSpace(TicketId);
    private bool CanStopRun() => IsRunning;

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
        });
    }
}

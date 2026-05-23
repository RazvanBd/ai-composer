using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.ViewModels;

/// <summary>ViewModel for the Run Console page — live output streaming and run control.</summary>
public sealed partial class RunConsoleViewModel : ObservableObject
{
    private readonly IRunService _runService;

    [ObservableProperty]
    private ObservableCollection<string> _outputLines = [];

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _ticketId = string.Empty;

    /// <summary>Initialises <see cref="RunConsoleViewModel"/> and subscribes to run events.</summary>
    public RunConsoleViewModel(IRunService runService)
    {
        _runService = runService;
        _runService.OutputReceived += OnOutputReceived;
        _runService.RunCompleted += OnRunCompleted;
    }

    /// <summary>Starts a CLI run for the configured ticket ID.</summary>
    [RelayCommand(CanExecute = nameof(CanStartRun))]
    private async Task StartRunAsync()
    {
        OutputLines.Clear();
        IsRunning = true;
        await _runService.StartRunAsync(TicketId);
    }

    /// <summary>Stops the currently running CLI process.</summary>
    [RelayCommand(CanExecute = nameof(CanStopRun))]
    private void StopRun()
    {
        _runService.StopRun();
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
            IsRunning = false;
            OutputLines.Add($"[Process exited with code {exitCode}]");
        });
    }
}

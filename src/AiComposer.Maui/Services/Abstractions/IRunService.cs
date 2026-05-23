using AiComposer.Maui.Models;

namespace AiComposer.Maui.Services.Abstractions;

/// <summary>Controls ticket runs and streams live CLI output.</summary>
public interface IRunService
{
    /// <summary>Raised each time a new line of output arrives from the CLI process.</summary>
    event EventHandler<string> OutputReceived;

    /// <summary>Raised when the run has finished (successfully or with an error).</summary>
    event EventHandler<int> RunCompleted;

    /// <summary>Gets whether a run is currently in progress.</summary>
    bool IsRunning { get; }

    /// <summary>Starts a CLI run for the given ticket.</summary>
    Task StartRunAsync(string ticketId, CancellationToken ct = default);

    /// <summary>Requests the current run to stop.</summary>
    void StopRun();
}

namespace AiComposer.Maui.ViewModels;

/// <summary>Lifecycle state of a CLI run session in the Run Console.</summary>
public enum RunStatus
{
    /// <summary>No run is active.</summary>
    Idle,

    /// <summary>A run is currently in progress.</summary>
    Running,

    /// <summary>The run finished successfully (exit code 0).</summary>
    Completed,

    /// <summary>The run finished with an error (non-zero exit code).</summary>
    Failed,

    /// <summary>The run was stopped by the user.</summary>
    Stopped,
}

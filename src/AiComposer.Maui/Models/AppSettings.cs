namespace AiComposer.Maui.Models;

/// <summary>Application settings persisted to appsettings.json.</summary>
public sealed class AppSettings
{
    /// <summary>Path to the artifacts workspace folder.</summary>
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Path to the output folder where generated files are written.</summary>
    public string OutputPath { get; set; } = string.Empty;

    /// <summary>Configured AI provider identifier.</summary>
    public string AiProvider { get; set; } = "openai";

    /// <summary>Model name for the selected AI provider.</summary>
    public string AiModel { get; set; } = string.Empty;

    /// <summary>API key used by the selected provider.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Whether runs can proceed without manual confirmation.</summary>
    public bool AutoApprove { get; set; }

    /// <summary>Execution timeout in minutes for a run.</summary>
    public int TimeoutMinutes { get; set; } = 30;

    /// <summary>Maximum number of retries when a run fails.</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>Path to the CLI executable to invoke for runs.</summary>
    public string CliExecutablePath { get; set; } = string.Empty;

    /// <summary>Whether to stream live output from CLI process.</summary>
    public bool LiveOutput { get; set; } = true;
}

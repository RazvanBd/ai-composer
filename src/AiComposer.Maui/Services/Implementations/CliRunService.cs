using System.Diagnostics;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.Services.Implementations;

/// <summary>
/// Implements <see cref="IRunService"/> by starting the AiComposer CLI as a child process
/// and streaming its stdout/stderr output as events. Does not duplicate engine logic.
/// </summary>
public sealed class CliRunService : IRunService, IDisposable
{
    private readonly ISettingsService _settingsService;
    private Process? _currentProcess;
    private CancellationTokenSource? _cts;

    /// <inheritdoc/>
    public event EventHandler<string>? OutputReceived;

    /// <inheritdoc/>
    public event EventHandler<int>? RunCompleted;

    /// <inheritdoc/>
    public bool IsRunning => _currentProcess is { HasExited: false };

    /// <summary>Initialises <see cref="CliRunService"/> with the required settings service.</summary>
    public CliRunService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <inheritdoc/>
    public async Task StartRunAsync(string ticketId, CancellationToken ct = default)
    {
        if (IsRunning)
            throw new InvalidOperationException("A run is already in progress. Stop it before starting a new one.");

        var settings = await _settingsService.LoadAsync(ct);
        var cliPath = ResolveCliPath(settings.CliExecutablePath);

        var args = BuildArguments(ticketId, settings);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var startInfo = new ProcessStartInfo
        {
            FileName = cliPath,
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        _currentProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        _currentProcess.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                OutputReceived?.Invoke(this, e.Data);
        };

        _currentProcess.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                OutputReceived?.Invoke(this, e.Data);
        };

        _currentProcess.Exited += (_, _) =>
        {
            var exitCode = _currentProcess?.ExitCode ?? -1;
            RunCompleted?.Invoke(this, exitCode);
        };

        _currentProcess.Start();
        _currentProcess.BeginOutputReadLine();
        _currentProcess.BeginErrorReadLine();

        _ = WaitForExitAsync(_cts.Token);
    }

    /// <inheritdoc/>
    public void StopRun()
    {
        if (_currentProcess is { HasExited: false })
        {
            try
            {
                _currentProcess.Kill(entireProcessTree: true);
            }
            catch (Exception)
            {
                // Process may have exited between the check and the kill — ignore.
            }
        }

        _cts?.Cancel();
    }

    /// <summary>Releases the current process resources.</summary>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _currentProcess?.Dispose();
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string ResolveCliPath(string configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
            return configuredPath;

        // Fall back to a sibling AiComposer.Cli executable next to the MAUI app.
        var appDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(appDir, "AiComposer.Cli.exe"),
            Path.Combine(appDir, "AiComposer.Cli"),
            Path.Combine(appDir, "..", "AiComposer.Cli", "AiComposer.Cli.exe"),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        // Return the configured path anyway — the OS will throw a meaningful error on Start().
        return string.IsNullOrWhiteSpace(configuredPath) ? "AiComposer.Cli" : configuredPath;
    }

    private static string BuildArguments(string ticketId, Models.AppSettings settings)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(settings.WorkspacePath))
            parts.Add($"--artifacts \"{settings.WorkspacePath}\"");

        if (!string.IsNullOrWhiteSpace(settings.OutputPath))
            parts.Add($"--output \"{settings.OutputPath}\"");

        if (!string.IsNullOrWhiteSpace(settings.AiProvider))
            parts.Add($"--provider {settings.AiProvider}");

        if (!string.IsNullOrWhiteSpace(settings.AiModel))
            parts.Add($"--copilot-model {settings.AiModel}");

        if (settings.LiveOutput)
            parts.Add("--live-output");

        return string.Join(" ", parts);
    }

    private async Task WaitForExitAsync(CancellationToken ct)
    {
        if (_currentProcess is null)
            return;

        try
        {
            await _currentProcess.WaitForExitAsync(ct);
        }
        catch (OperationCanceledException)
        {
            StopRun();
        }
    }
}

using System.Text.Json;
using AiComposer.Core.Models;

namespace AiComposer.Core.Telemetry;

/// <summary>
/// Appends <see cref="TraceEvent"/> records as newline-delimited JSON (JSONL)
/// for ticket-level cost and observability tracing.
/// </summary>
public sealed class TraceLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
    };

    private readonly string _logPath;
    private readonly object _sync = new();

    /// <summary>Initialises the logger and ensures the parent directory exists.</summary>
    public TraceLogger(string logPath)
    {
        _logPath = logPath;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logPath))!);
    }

    /// <summary>Appends a single trace event to the JSONL log file.</summary>
    public void Log(TraceEvent traceEvent)
    {
        traceEvent.Timestamp = DateTime.UtcNow.ToString("O");
        var line = JsonSerializer.Serialize(traceEvent, JsonOptions);
        lock (_sync)
        {
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
    }
}

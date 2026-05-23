using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.Services.Implementations;

/// <summary>
/// Implements <see cref="IOutputService"/> by listing files under the output folder
/// that corresponds to a given ticket.
/// </summary>
public sealed class FileOutputService : IOutputService
{
    private readonly ISettingsService _settingsService;

    /// <summary>Initialises <see cref="FileOutputService"/>.</summary>
    public FileOutputService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<GeneratedFileItem>> ListGeneratedFilesAsync(
        string ticketId,
        CancellationToken ct = default)
    {
        var settings = await _settingsService.LoadAsync(ct);
        var outputRoot = settings.OutputPath;

        if (string.IsNullOrWhiteSpace(outputRoot) || !Directory.Exists(outputRoot))
            return [];

        var ticketOutputDir = Path.Combine(outputRoot, ticketId);
        if (!Directory.Exists(ticketOutputDir))
            return [];

        return Directory
            .EnumerateFiles(ticketOutputDir, "*", SearchOption.AllDirectories)
            .Select(f =>
            {
                var info = new FileInfo(f);
                return new GeneratedFileItem
                {
                    RelativePath = Path.GetRelativePath(ticketOutputDir, f),
                    FullPath = f,
                    SizeBytes = info.Length,
                    LastModified = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
                };
            })
            .ToList();
    }

    /// <inheritdoc/>
    public Task<string> ReadFileContentAsync(string fullPath, CancellationToken ct = default)
        => File.ReadAllTextAsync(fullPath, ct);
}

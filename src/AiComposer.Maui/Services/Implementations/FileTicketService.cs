using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;

namespace AiComposer.Maui.Services.Implementations;

/// <summary>
/// Implements <see cref="ITicketService"/> by scanning the workspace for ticket
/// artifacts and reading state from the SQLite state database when available.
/// </summary>
public sealed class FileTicketService : ITicketService
{
    private readonly IArtifactsService _artifactsService;

    /// <summary>Initialises <see cref="FileTicketService"/>.</summary>
    public FileTicketService(IArtifactsService artifactsService)
    {
        _artifactsService = artifactsService;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TicketItem>> LoadTicketsAsync(CancellationToken ct = default)
    {
        var artifacts = await _artifactsService.LoadArtifactsAsync(ct);
        return artifacts
            .Where(a => string.Equals(a.Type, "ticket", StringComparison.OrdinalIgnoreCase))
            .Select(a => new TicketItem
            {
                Id = a.Id,
                Title = a.Title,
                State = ParseTicketState(a.Content),
                EpicId = ParseEpicId(a.Content),
                FilePath = a.FilePath,
            })
            .ToList();
    }

    /// <inheritdoc/>
    public async Task<string> GetTicketStateAsync(string ticketId, CancellationToken ct = default)
    {
        var tickets = await LoadTicketsAsync(ct);
        return tickets.FirstOrDefault(t => t.Id == ticketId)?.State ?? "unknown";
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static string ParseTicketState(string content)
    {
        foreach (var line in content.Split('\n'))
        {
            var colonIdx = line.IndexOf(':', StringComparison.Ordinal);
            if (colonIdx < 0) continue;
            var key = line[..colonIdx].Trim();
            if (string.Equals(key, "state", StringComparison.OrdinalIgnoreCase))
                return line[(colonIdx + 1)..].Trim();
        }

        return "draft";
    }

    private static string ParseEpicId(string content)
    {
        foreach (var line in content.Split('\n'))
        {
            var colonIdx = line.IndexOf(':', StringComparison.Ordinal);
            if (colonIdx < 0) continue;
            var key = line[..colonIdx].Trim();
            if (string.Equals(key, "epic", StringComparison.OrdinalIgnoreCase))
                return line[(colonIdx + 1)..].Trim();
        }

        return string.Empty;
    }
}

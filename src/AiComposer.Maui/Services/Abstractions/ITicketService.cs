using AiComposer.Maui.Models;

namespace AiComposer.Maui.Services.Abstractions;

/// <summary>Lists tickets and reads per-ticket lifecycle state.</summary>
public interface ITicketService
{
    /// <summary>Loads all ticket artifacts from the workspace.</summary>
    Task<IReadOnlyList<TicketItem>> LoadTicketsAsync(CancellationToken ct = default);

    /// <summary>Returns the current lifecycle state string for the given ticket ID.</summary>
    Task<string> GetTicketStateAsync(string ticketId, CancellationToken ct = default);
}

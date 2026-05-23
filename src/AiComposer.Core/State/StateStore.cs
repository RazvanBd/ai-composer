using AiComposer.Core.Models;
using Microsoft.Data.Sqlite;

namespace AiComposer.Core.State;

/// <summary>
/// Persistent orchestrator state machine backed by SQLite.
/// Resumes from the last persisted state after a restart, preventing duplicate LLM calls.
/// </summary>
public sealed class StateStore : IDisposable
{
    private const int CircuitBreakerThreshold = 3;

    private readonly string _connectionString;

    /// <summary>Initialises the state store and creates the database schema if needed.</summary>
    public StateStore(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dbPath))!);
        _connectionString = $"Data Source={dbPath}";
        InitDb();
    }

    /// <summary>Returns the current state record for a ticket, or null if not tracked yet.</summary>
    public TicketStateRecord? GetTicket(string ticketId)
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT ticket_id, state, attempts, updated_at, last_error FROM ticket_state WHERE ticket_id = @id";
        cmd.Parameters.AddWithValue("@id", ticketId);
        using var reader = cmd.ExecuteReader();
        return reader.Read() ? ReadRecord(reader) : null;
    }

    /// <summary>Returns existing state or inserts a fresh record in <see cref="TicketState.WaitingForCoder"/>.</summary>
    public TicketStateRecord EnsureTicket(string ticketId)
    {
        var existing = GetTicket(ticketId);
        if (existing is not null) return existing;

        var now = DateTime.UtcNow.ToString("O");
        var record = new TicketStateRecord
        {
            TicketId = ticketId,
            State = TicketState.WaitingForCoder,
            Attempts = 0,
            UpdatedAt = now,
        };

        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO ticket_state (ticket_id, state, attempts, updated_at, last_error) VALUES (@id, @state, @attempts, @updatedAt, @error)";
        cmd.Parameters.AddWithValue("@id", record.TicketId);
        cmd.Parameters.AddWithValue("@state", record.State);
        cmd.Parameters.AddWithValue("@attempts", record.Attempts);
        cmd.Parameters.AddWithValue("@updatedAt", record.UpdatedAt);
        cmd.Parameters.AddWithValue("@error", DBNull.Value);
        cmd.ExecuteNonQuery();

        return record;
    }

    /// <summary>
    /// Transitions a ticket to <paramref name="newState"/>.
    /// When <paramref name="incrementAttempt"/> is true, the attempt counter is incremented,
    /// and the circuit breaker kicks in after <see cref="CircuitBreakerThreshold"/> failures.
    /// </summary>
    public TicketStateRecord Transition(
        string ticketId,
        string newState,
        string? lastError = null,
        bool incrementAttempt = false)
    {
        var current = EnsureTicket(ticketId);
        var attempts = current.Attempts + (incrementAttempt ? 1 : 0);
        var resolvedState = attempts >= CircuitBreakerThreshold && incrementAttempt
            ? TicketState.PausedHumanIntervention
            : newState;

        var now = DateTime.UtcNow.ToString("O");
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "UPDATE ticket_state SET state = @state, attempts = @attempts, updated_at = @updatedAt, last_error = @error WHERE ticket_id = @id";
        cmd.Parameters.AddWithValue("@state", resolvedState);
        cmd.Parameters.AddWithValue("@attempts", attempts);
        cmd.Parameters.AddWithValue("@updatedAt", now);
        cmd.Parameters.AddWithValue("@error", (object?)lastError ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", ticketId);
        cmd.ExecuteNonQuery();

        return new TicketStateRecord
        {
            TicketId = ticketId,
            State = resolvedState,
            Attempts = attempts,
            UpdatedAt = now,
            LastError = lastError,
        };
    }

    /// <inheritdoc/>
    public void Dispose() => SqliteConnection.ClearAllPools();

    // ── private helpers ────────────────────────────────────────────────────────

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    private void InitDb()
    {
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS ticket_state (
                ticket_id  TEXT PRIMARY KEY,
                state      TEXT NOT NULL,
                attempts   INTEGER NOT NULL,
                updated_at TEXT NOT NULL,
                last_error TEXT
            )
            """;
        cmd.ExecuteNonQuery();
    }

    private static TicketStateRecord ReadRecord(SqliteDataReader reader) =>
        new()
        {
            TicketId = reader.GetString(0),
            State = reader.GetString(1),
            Attempts = reader.GetInt32(2),
            UpdatedAt = reader.GetString(3),
            LastError = reader.IsDBNull(4) ? null : reader.GetString(4),
        };
}

from __future__ import annotations

import sqlite3
from dataclasses import dataclass
from datetime import UTC, datetime
from pathlib import Path

from .models import TicketState


@dataclass(slots=True)
class TicketStateRecord:
    """Stored state for a ticket lifecycle."""

    ticket_id: str
    state: str
    attempts: int
    updated_at: str
    last_error: str | None = None


class StateStore:
    """Persistent orchestrator state machine backed by SQLite."""

    def __init__(self, db_path: str) -> None:
        self.db_path = db_path
        Path(db_path).parent.mkdir(parents=True, exist_ok=True)
        self._init_db()

    def _connect(self) -> sqlite3.Connection:
        return sqlite3.connect(self.db_path)

    def _init_db(self) -> None:
        with self._connect() as conn:
            conn.execute(
                """
                CREATE TABLE IF NOT EXISTS ticket_state (
                    ticket_id TEXT PRIMARY KEY,
                    state TEXT NOT NULL,
                    attempts INTEGER NOT NULL,
                    updated_at TEXT NOT NULL,
                    last_error TEXT
                )
                """
            )

    def get_ticket(self, ticket_id: str) -> TicketStateRecord | None:
        with self._connect() as conn:
            row = conn.execute(
                "SELECT ticket_id, state, attempts, updated_at, last_error FROM ticket_state WHERE ticket_id = ?",
                (ticket_id,),
            ).fetchone()
            if row is None:
                return None
            return TicketStateRecord(*row)

    def ensure_ticket(self, ticket_id: str) -> TicketStateRecord:
        existing = self.get_ticket(ticket_id)
        if existing:
            return existing
        now = datetime.now(tz=UTC).isoformat()
        record = TicketStateRecord(ticket_id, TicketState.WAITING_FOR_CODER, 0, now, None)
        with self._connect() as conn:
            conn.execute(
                """
                INSERT INTO ticket_state (ticket_id, state, attempts, updated_at, last_error)
                VALUES (?, ?, ?, ?, ?)
                """,
                (record.ticket_id, record.state, record.attempts, record.updated_at, record.last_error),
            )
        return record

    def transition(
        self,
        ticket_id: str,
        new_state: str,
        *,
        last_error: str | None = None,
        increment_attempt: bool = False,
    ) -> TicketStateRecord:
        current = self.ensure_ticket(ticket_id)
        attempts = current.attempts + (1 if increment_attempt else 0)
        if attempts >= 3 and increment_attempt:
            new_state = TicketState.PAUSED_HUMAN_INTERVENTION
        now = datetime.now(tz=UTC).isoformat()
        with self._connect() as conn:
            conn.execute(
                """
                UPDATE ticket_state
                SET state = ?, attempts = ?, updated_at = ?, last_error = ?
                WHERE ticket_id = ?
                """,
                (new_state, attempts, now, last_error, ticket_id),
            )
        return TicketStateRecord(ticket_id, new_state, attempts, now, last_error)


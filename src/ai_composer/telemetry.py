from __future__ import annotations

import json
from dataclasses import asdict, dataclass
from datetime import UTC, datetime
from pathlib import Path


@dataclass(slots=True)
class TraceEvent:
    """Single observability event for an agent action."""

    trace_id: str
    ticket_id: str
    role: str
    tokens_in: int
    tokens_out: int
    cost_usd: float
    status: str
    message: str


class TraceLogger:
    """JSONL logger for ticket-level tracing and cost visibility."""

    def __init__(self, log_path: str) -> None:
        self.log_path = Path(log_path)
        self.log_path.parent.mkdir(parents=True, exist_ok=True)

    def log(self, event: TraceEvent) -> None:
        payload = asdict(event)
        payload["timestamp"] = datetime.now(tz=UTC).isoformat()
        with self.log_path.open("a", encoding="utf-8") as handle:
            handle.write(json.dumps(payload, ensure_ascii=False) + "\n")


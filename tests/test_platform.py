from __future__ import annotations

import json
import sqlite3
import tempfile
import unittest
from pathlib import Path

import sys

sys.path.insert(0, "/home/runner/work/ai-composer/ai-composer/src")

from ai_composer.markdown_loader import load_markdown_artifacts
from ai_composer.models import TicketState
from ai_composer.orchestrator import Orchestrator
from ai_composer.state_store import StateStore
from ai_composer.telemetry import TraceLogger


class PlatformTests(unittest.TestCase):
    def test_loader_links_ticket_to_epic_and_rules(self) -> None:
        artifacts = load_markdown_artifacts("/home/runner/work/ai-composer/ai-composer/examples/artifacts")
        self.assertIn("T-105", artifacts)
        self.assertEqual(artifacts["T-105"].type, "ticket")
        self.assertIn("E-1", artifacts["T-105"].links)
        self.assertIn("R-1", artifacts["T-105"].links)

    def test_state_store_circuit_breaker_after_three_failures(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            db_path = str(Path(tmp) / "state.db")
            store = StateStore(db_path)
            store.ensure_ticket("T-1")
            store.transition("T-1", TicketState.CODER_ITERATING, increment_attempt=True, last_error="e1")
            store.transition("T-1", TicketState.CODER_ITERATING, increment_attempt=True, last_error="e2")
            record = store.transition("T-1", TicketState.CODER_ITERATING, increment_attempt=True, last_error="e3")
            self.assertEqual(record.state, TicketState.PAUSED_HUMAN_INTERVENTION)
            self.assertEqual(record.attempts, 3)

    def test_orchestrator_generates_context_and_trace(self) -> None:
        with tempfile.TemporaryDirectory() as tmp:
            tmp_path = Path(tmp)
            out_dir = tmp_path / "out"
            db_path = tmp_path / "orchestrator.db"
            trace_path = tmp_path / "traces.jsonl"

            orchestrator = Orchestrator(
                state_store=StateStore(str(db_path)),
                trace_logger=TraceLogger(str(trace_path)),
            )
            tickets = orchestrator.process_tickets(
                "/home/runner/work/ai-composer/ai-composer/examples/artifacts",
                str(out_dir),
            )

            self.assertEqual(tickets, ["T-105"])
            context_path = out_dir / "T-105" / "context.json"
            self.assertTrue(context_path.exists())
            payload = json.loads(context_path.read_text(encoding="utf-8"))
            self.assertEqual(payload["ticket_id"], "T-105")
            self.assertTrue(payload["security_context"]["sandbox_required"])
            self.assertTrue(trace_path.exists())
            self.assertEqual(len(trace_path.read_text(encoding="utf-8").splitlines()), 1)

            with sqlite3.connect(db_path) as conn:
                state = conn.execute("SELECT state FROM ticket_state WHERE ticket_id = 'T-105'").fetchone()[0]
            self.assertEqual(state, TicketState.WAITING_FOR_REVIEW)


if __name__ == "__main__":
    unittest.main()


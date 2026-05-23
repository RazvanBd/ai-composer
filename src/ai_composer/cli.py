from __future__ import annotations

import argparse
from pathlib import Path

from .orchestrator import Orchestrator
from .state_store import StateStore
from .telemetry import TraceLogger


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="A.S.A.D markdown-driven platform CLI")
    parser.add_argument("--artifacts", required=True, help="Absolute path to markdown artifacts directory")
    parser.add_argument("--output", required=True, help="Absolute path to output directory")
    parser.add_argument(
        "--state-db",
        default=".state/orchestrator.db",
        help="Path to SQLite state store (default: .state/orchestrator.db)",
    )
    parser.add_argument(
        "--trace-log",
        default=".state/traces.jsonl",
        help="Path to JSONL trace file (default: .state/traces.jsonl)",
    )
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()

    artifacts = str(Path(args.artifacts).resolve())
    output = str(Path(args.output).resolve())
    state_db = str(Path(args.state_db).resolve())
    trace_log = str(Path(args.trace_log).resolve())

    orchestrator = Orchestrator(state_store=StateStore(state_db), trace_logger=TraceLogger(trace_log))
    tickets = orchestrator.process_tickets(artifacts, output)
    print(f"Processed {len(tickets)} ticket(s): {', '.join(tickets) if tickets else 'none'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())


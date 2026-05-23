from __future__ import annotations

import json
from dataclasses import asdict
from pathlib import Path
from uuid import uuid4

from .markdown_loader import load_markdown_artifacts
from .models import ArtifactNode, TicketContext, TicketState
from .security import inject_mock_environment
from .state_store import StateStore
from .telemetry import TraceEvent, TraceLogger


class Orchestrator:
    """MD-driven orchestrator that builds strongly-typed ticket context."""

    def __init__(self, *, state_store: StateStore, trace_logger: TraceLogger) -> None:
        self.state_store = state_store
        self.trace_logger = trace_logger

    def load_graph(self, artifacts_dir: str) -> dict[str, ArtifactNode]:
        return load_markdown_artifacts(artifacts_dir)

    def build_ticket_context(self, ticket: ArtifactNode, graph: dict[str, ArtifactNode]) -> TicketContext:
        acceptance = ticket.metadata.get("acceptance", [])
        if not isinstance(acceptance, list):
            acceptance = []
        linked = [graph[ref] for ref in ticket.links if ref in graph]
        project_summary = [node.body for node in graph.values() if node.type == "project_summary"]
        adrs = [node.body for node in graph.values() if node.type == "adr" and node.metadata.get("ticket") == ticket.id]
        return TicketContext(
            ticket_id=ticket.id,
            title=ticket.title,
            acceptance_criteria=[str(item) for item in acceptance],
            static_context={
                "project_summary": project_summary,
                "coding_conventions": [
                    "No hardcoded secrets",
                    "Use environment variables for external integrations",
                    "Add concise summaries for changed public methods",
                ],
            },
            dynamic_context={
                "linked_artifacts": [
                    {"id": node.id, "type": node.type, "title": node.title, "path": node.path} for node in linked
                ],
                "adrs": adrs,
            },
            security_context={
                "sandbox_required": True,
                "mock_environment": inject_mock_environment(),
            },
        )

    def process_tickets(self, artifacts_dir: str, output_dir: str) -> list[str]:
        graph = self.load_graph(artifacts_dir)
        ticket_ids: list[str] = []
        out_root = Path(output_dir)
        out_root.mkdir(parents=True, exist_ok=True)
        for node in graph.values():
            if node.type != "ticket":
                continue
            ticket_ids.append(node.id)
            self.state_store.ensure_ticket(node.id)
            self.state_store.transition(node.id, TicketState.CODER_ITERATING)
            context = self.build_ticket_context(node, graph)
            ticket_path = out_root / node.id
            ticket_path.mkdir(parents=True, exist_ok=True)
            (ticket_path / "context.json").write_text(
                json.dumps(asdict(context), ensure_ascii=False, indent=2), encoding="utf-8"
            )
            trace_id = str(uuid4())
            self.trace_logger.log(
                TraceEvent(
                    trace_id=trace_id,
                    ticket_id=node.id,
                    role="tech_lead_orchestrator",
                    tokens_in=1200,
                    tokens_out=450,
                    cost_usd=0.025,
                    status="success",
                    message="Generated strongly-typed context from markdown artifacts",
                )
            )
            self.state_store.transition(node.id, TicketState.WAITING_FOR_REVIEW)
        return ticket_ids

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any


@dataclass(slots=True)
class ArtifactNode:
    """Knowledge-graph node loaded from markdown artifacts."""

    id: str
    type: str
    title: str
    body: str
    metadata: dict[str, Any]
    path: str
    links: list[str] = field(default_factory=list)


@dataclass(slots=True)
class TicketContext:
    """Assembled strongly-typed context for a single ticket."""

    ticket_id: str
    title: str
    acceptance_criteria: list[str]
    static_context: dict[str, Any]
    dynamic_context: dict[str, Any]
    security_context: dict[str, Any]


@dataclass(slots=True)
class SandboxPolicy:
    """Container isolation policy for test/runtime execution."""

    workspace: str
    image: str = "python:3.12-slim"
    network_mode: str = "none"
    read_only_root: bool = True
    cpu_limit: str = "1.0"
    memory_limit: str = "512m"


class TicketState:
    """Finite states for orchestrator ticket lifecycle."""

    WAITING_FOR_CODER = "waiting_for_coder"
    CODER_ITERATING = "coder_iterating"
    WAITING_FOR_REVIEW = "waiting_for_review"
    WAITING_FOR_QA = "waiting_for_qa"
    READY_FOR_HUMAN_REVIEW = "ready_for_human_review"
    PAUSED_HUMAN_INTERVENTION = "paused_human_intervention"


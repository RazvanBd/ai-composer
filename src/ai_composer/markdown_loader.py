from __future__ import annotations

from pathlib import Path
from typing import Any

from .models import ArtifactNode


def _parse_scalar(value: str) -> Any:
    value = value.strip()
    if value.lower() in {"true", "false"}:
        return value.lower() == "true"
    if value.isdigit():
        return int(value)
    if value.startswith("[") and value.endswith("]"):
        inner = value[1:-1].strip()
        if not inner:
            return []
        return [item.strip().strip("'\"") for item in inner.split(",")]
    return value.strip("'\"")


def _parse_frontmatter(frontmatter: str) -> dict[str, Any]:
    meta: dict[str, Any] = {}
    current_list_key: str | None = None
    for raw_line in frontmatter.splitlines():
        line = raw_line.rstrip()
        if not line.strip():
            continue
        stripped = line.strip()
        if stripped.startswith("- ") and current_list_key:
            meta.setdefault(current_list_key, []).append(_parse_scalar(stripped[2:]))
            continue
        if ":" not in line:
            continue
        key, value = line.split(":", 1)
        key = key.strip()
        value = value.strip()
        if not value:
            meta[key] = []
            current_list_key = key
            continue
        meta[key] = _parse_scalar(value)
        current_list_key = None
    return meta


def load_markdown_artifacts(root_dir: str) -> dict[str, ArtifactNode]:
    root = Path(root_dir)
    nodes: dict[str, ArtifactNode] = {}
    for path in sorted(root.rglob("*.md")):
        raw = path.read_text(encoding="utf-8")
        metadata: dict[str, Any] = {}
        body = raw
        if raw.startswith("---\n"):
            _, frontmatter, remainder = raw.split("---\n", 2)
            metadata = _parse_frontmatter(frontmatter)
            body = remainder.strip()
        node_id = str(metadata.get("id") or path.stem)
        node_type = str(metadata.get("type") or "document").lower()
        title = str(metadata.get("title") or path.stem.replace("-", " ").title())
        nodes[node_id] = ArtifactNode(
            id=node_id,
            type=node_type,
            title=title,
            body=body,
            metadata=metadata,
            path=str(path),
        )
    _link_nodes(nodes)
    return nodes


def _link_nodes(nodes: dict[str, ArtifactNode]) -> None:
    for node in nodes.values():
        links: list[str] = []
        if node.type == "ticket":
            epic = node.metadata.get("epic")
            if isinstance(epic, str) and epic in nodes:
                links.append(epic)
            rules = node.metadata.get("rules", [])
            if isinstance(rules, list):
                links.extend(rule for rule in rules if isinstance(rule, str) and rule in nodes)
        if node.type == "adr":
            ticket = node.metadata.get("ticket")
            if isinstance(ticket, str) and ticket in nodes:
                links.append(ticket)
        node.links = list(dict.fromkeys(links))


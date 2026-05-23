from __future__ import annotations

from pathlib import Path

from .models import SandboxPolicy


def sandbox_docker_command(policy: SandboxPolicy, command: list[str]) -> list[str]:
    """Build docker command for restricted, ephemeral execution."""
    workspace = Path(policy.workspace).resolve()
    return [
        "docker",
        "run",
        "--rm",
        "--network",
        policy.network_mode,
        "--cpus",
        policy.cpu_limit,
        "--memory",
        policy.memory_limit,
        "--read-only" if policy.read_only_root else "--read-only=false",
        "-v",
        f"{workspace}:/workspace:rw",
        "-w",
        "/workspace",
        policy.image,
        *command,
    ]


def validate_workspace_path(workspace: str, target_path: str) -> bool:
    """Ensure path write operations remain inside workspace boundaries."""
    root = Path(workspace).resolve()
    target = Path(target_path).resolve()
    return root == target or root in target.parents


def inject_mock_environment(overrides: dict[str, str] | None = None) -> dict[str, str]:
    """Return safe mocked env values for external integrations."""
    env = {
        "PAYMENTS_API_URL": "http://wiremock:8080/payments",
        "S3_ENDPOINT": "http://wiremock:8080/s3",
        "S3_ACCESS_KEY": "mock-access-key",
        "S3_SECRET_KEY": "mock-secret-key",
        "USE_MOCK_SERVICES": "true",
    }
    if overrides:
        env.update(overrides)
    return env


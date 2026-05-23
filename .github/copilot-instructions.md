# Copilot Instructions

## Build and test

- Requires the .NET 9 SDK. The solution root is `AiComposer.slnx`.
- Build everything from the repository root with `dotnet build`.
- Run the full test suite with `dotnet test`.
- Run a single xUnit test with:
  `dotnet test tests\AiComposer.Tests\AiComposer.Tests.csproj --filter FullyQualifiedName~AiComposer.Tests.OrchestratorTests.ProcessTickets_GeneratesContextAndPersistsState`
- Run the orchestrator CLI with:
  `dotnet run --project src\AiComposer.Cli -- --artifacts examples\artifacts --output output --state-db .state\orchestrator.db --trace-log .state\traces.jsonl`
- To spawn a DeepSeek-backed ticket agent, run:
  `dotnet run --project src\AiComposer.Cli -- --artifacts examples\artifacts --output output --provider deepseek --prompt "..." --role coder`
- To use the local GitHub Copilot runtime instead, run:
  `dotnet run --project src\AiComposer.Cli -- --artifacts examples\artifacts --output output --provider copilot --copilot-model gpt-5 --prompt "..." --role coder`
- DeepSeek execution reads the API key from `DEEPSEEK_API_KEY`. Use `--dry-run` to generate request metadata without making a provider call.
- Copilot SDK execution requires a locally authenticated Copilot CLI session (`copilot login`) instead of a provider API key.
- Add `--live-output` when you want streamed provider output in the terminal during execution.
- Add `--apply-files <workspace>` with `--role coder` when you want AI Composer to parse a JSON file plan from the provider response and write the generated text files into a target workspace. Use `--allow-overwrite` only when you want existing files to be replaced.
- There is no separate repository lint command today; the practical verification loop is `dotnet build` plus `dotnet test`.

## High-level architecture

- The codebase is split into `src\AiComposer.Core` and `src\AiComposer.Cli`, with xUnit coverage in `tests\AiComposer.Tests`.
- `AiComposer.Core\Markdown\MarkdownLoader` turns Markdown files with YAML frontmatter into an in-memory artifact graph. Ticket nodes link to their epic and business rules, and ADR nodes link back to their ticket.
- `AiComposer.Core\Orchestration\Orchestrator` is the main pipeline. It loads the artifact graph, advances ticket lifecycle state, builds a strongly typed `TicketContext`, writes `output\<ticket-id>\context.json`, and appends trace telemetry.
- When `--provider deepseek` is enabled, the orchestrator also builds a sanitized provider request from the ticket context plus linked Markdown artifacts, stores per-run audit files under `output\<ticket-id>\runs\...`, and writes the provider response to `agent-response.md`.
- Provider execution currently supports both DeepSeek (HTTP API) and Copilot SDK (local Copilot runtime). Both share the same ticket-context request shape and audit outputs.
- When `--apply-files` is enabled, coder requests switch to JSON output, the orchestrator validates every generated relative path against the target workspace, writes the files, and records the applied file list in `applied-files.json`.
- `AiComposer.Core\State\StateStore` persists ticket state in SQLite (`ticket_state`). The state machine is intentionally disk-backed, and `Transition(..., incrementAttempt: true)` trips a circuit breaker after 3 failed attempts by moving the ticket to `paused_human_intervention`.
- `AiComposer.Core\Security\SecurityHelper` centralizes sandbox and mock-environment behavior. Generated contexts always set `sandboxRequired = true` and include mock external-service values for local/test execution.
- Output serialization is split by consumer: `context.json` is camelCase JSON, while `.state\traces.jsonl` uses snake_case JSONL records.

## Key conventions

- Keep source changes in the existing .NET 9 / C# projects. The repository docs and skill files assume this is a C#-only codebase.
- Prefer the artifact-driven workflow described in the repo docs and skill file: Project Summary -> Epic -> Business Rule -> Ticket -> ADR -> generated ticket context. When changing orchestration behavior, check whether the corresponding Markdown artifact shape or linking rules also need to change.
- Artifact Markdown is schema-sensitive, not freeform. Keep stable IDs and preserve the keys the loader understands: `id`, `type`, `title`, `epic`, `rules`, `ticket`, and `acceptance`.
- Frontmatter parsing is intentionally minimal. `MarkdownLoader` supports simple scalars, bracket lists, and `-` list items under the previous key; avoid relying on richer YAML features unless the loader is extended first.
- Public methods are expected to keep concise XML `<summary>` documentation. This is both a documented project rule and a code-level convention already followed throughout the core library.
- Preserve the three-part `TicketContext` split: `staticContext` for always-on guidance, `dynamicContext` for linked artifacts and ADR content, and `securityContext` for sandbox and mock-environment constraints.
- Provider execution is opt-in. Do not assume a DeepSeek call should happen just because `DEEPSEEK_API_KEY` exists in the environment; the CLI requires `--provider deepseek` explicitly.
- Audit files for provider runs should stay redacted. Do not persist API keys or full prompt payloads to disk.
- Automatic file application supports text files only and refuses path traversal or rooted paths. Keep output and apply-workspace directories separate.
- The example artifact set in `examples\artifacts` is the canonical fixture for tests and CLI examples. If you change artifact semantics, update those fixtures and the corresponding tests together.
- Repository docs and example artifact content are partly Romanian, but YAML keys, state constants, and serialized field names stay in English. Keep machine-readable identifiers in English even when prose content is Romanian.

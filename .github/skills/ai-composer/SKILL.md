---
name: ai-composer
description: Use when the user wants to use AI Composer to bootstrap or evolve a software project through orchestrated AI agents, Markdown artifacts, tickets, epics, business rules, ADRs, or typed ticket context. Trigger even when the request is phrased in Romanian, such as "orchestrare agenti", "artefacte markdown", "epic", "ticket", "ADR", "context tipizat", or "construieste proiectul cu AI Composer".
compatibility: Requires filesystem access in this repository and benefits from dotnet CLI availability for build, test, and orchestrator execution.
---

# AI Composer

Use this skill to drive the **AI Composer** workflow in this repository. The goal is to turn vague product requests into deterministic, disk-backed artifacts and then use those artifacts to orchestrate specialized agents.

## When this skill is active

Treat AI Composer as the default workflow when the user wants any of the following:

- build a project or feature from scratch with orchestrated agents
- define or refine project artifacts such as Project Summary, Epic, Ticket, Business Rule, or ADR
- generate typed execution context for a ticket
- run or extend the AI Composer orchestrator
- inspect ticket state, traces, or cost telemetry

Prefer the repository's artifact-driven process over ad-hoc freeform planning.

## Core operating model

AI Composer is based on four pillars:

1. **Shallow context**: include only the code and artifact context needed for the current ticket.
2. **Knowledge graph on disk**: persist business and technical decisions as Markdown files with YAML frontmatter.
3. **Dynamic prompt assembly**: combine static project guidance with ticket-specific linked artifacts.
4. **Role hierarchy**: split work across PO, PM, BA, Architect, Tech Lead, Coder, Reviewer, and QA responsibilities.

## Default workflow

Follow this sequence unless the user explicitly asks for only one part:

1. Capture the request and convert it into persistent Markdown artifacts.
2. Make sure the repository has a clear **Project Summary**.
3. Model the feature as an **Epic** plus reusable **Business Rules**.
4. Break the work into small **Tickets** with explicit acceptance criteria in Given/When/Then form.
5. Add **ADR** files for non-trivial technical decisions.
6. Run the orchestrator to generate typed ticket contexts.
7. Use the generated context to guide implementation, review, QA, and iteration.

## Artifact rules

Store artifacts under `artifacts\`.

Every artifact must use YAML frontmatter and a stable ID.

### Project Summary

Create `artifacts\project-summary.md` first when missing.

Use this shape:

```markdown
---
id: PS-1
type: project_summary
title: Project Summary
---
Arhitectură: [descriere]
Stack: [tehnologii principale]
Anti-pattern: [ce trebuie evitat]

[detalii arhitecturale concise]
```

### Epic

Use `artifacts\epic-{feature}.md`.

```markdown
---
id: E-1
type: epic
title: Nume Feature
---
[descrierea viziunii si scopului]
```

### Business Rule

Use `artifacts\rule-{name}.md`.

```markdown
---
id: R-1
type: rule
title: Nume Regula
---
[regula reutilizabila de business]
```

### Ticket

Use `artifacts\ticket-{feature}.md`.

Tickets should stay granular: ideally 1 main file, at most a few touched files, with 2-5 clear acceptance criteria.

```markdown
---
id: T-101
type: ticket
title: Nume Task
epic: E-1
rules:
  - R-1
acceptance:
  - Given [precondition], When [action], Then [result]
---
[detalii de implementare]
```

### ADR

Use `artifacts\adr-{decision}.md` whenever you choose between meaningful technical options, add a dependency, or alter a core pattern.

```markdown
---
id: ADR-1
type: adr
title: Decizie Arhitecturala
ticket: T-101
---
## Context
[de ce este necesara decizia]

## Decizie
[ce se alege]

## Consecinte
[trade-offs si impact]
```

## Repository and stack guidance

- Source code is expected to stay in **.NET / C#**
- Persist orchestrator state in **SQLite**
- Persist traces in **JSONL**
- Keep artifacts and design knowledge in **Markdown + YAML**

When editing code or proposing implementation work, stay aligned with the repository architecture and avoid introducing unrelated platform stacks.

## Orchestrator execution

Use this command to generate ticket context:

```powershell
dotnet run --project src\AiComposer.Cli -- --artifacts artifacts --output output --state-db .state\orchestrator.db --trace-log .state\traces.jsonl
```

If the user asks to verify the platform first, the common commands are:

```powershell
dotnet --version
dotnet build
dotnet test
```

## Expected outputs

For each ticket, expect generated context at:

- `output\{ticket-id}\context.json`

The context should carry:

- ticket ID and title
- acceptance criteria
- static context from the project summary and coding conventions
- dynamic context from linked artifacts and ADRs
- security context for sandboxed execution

Also expect:

- `.state\orchestrator.db` for ticket lifecycle state
- `.state\traces.jsonl` for token, cost, and execution telemetry

## Agent responsibilities

Use these responsibilities when turning artifacts into execution:

- **Product Owner**: turns the raw request into feature intent and outcomes
- **Project Manager**: decomposes the epic into tickets
- **Business Analyst**: sharpens acceptance criteria
- **Software Architect**: records architectural decisions as ADRs
- **Tech Lead / Orchestrator**: assembles minimal, typed context per ticket
- **Coder**: implements code and tests
- **Reviewer**: validates architecture, security, and code quality
- **QA**: validates Given/When/Then criteria in an isolated environment

## Built-in conventions to preserve

When generating or reviewing implementation work for AI Composer, preserve these conventions:

1. No hardcoded secrets; use environment variables.
2. Add concise XML `<summary>` tags to public methods you create or change.
3. Prefer sandboxed or isolated execution for generated code paths.
4. Use mock environment values for development and testing flows.
5. Keep prompts and contexts minimal, typed, and ticket-specific.

## Working style

When using this skill:

- prefer creating or updating the artifact graph before coding
- keep ticket scopes small and deterministic
- make links between business rules, epics, tickets, and ADRs explicit
- use acceptance criteria as the source of truth for implementation and QA
- use telemetry and state output as first-class signals when reporting progress

## Example triggers

- "Construieste feature-ul asta prin AI Composer"
- "Genereaza artefactele markdown pentru proiect"
- "Descompune cerinta in epic, tickets si business rules"
- "Fa un ADR pentru decizia asta si ruleaza orchestratorul"
- "Use AI Composer to turn this request into tickets and execution context"

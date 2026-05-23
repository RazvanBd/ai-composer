# ai-composer

Documentația arhitecturală extinsă este disponibilă în
[`ARCHITECTURE.md`](./ARCHITECTURE.md).

## Platformă MVP (.NET 9, C#)

Codul sursă implementează orchestratorul A.S.A.D. descris în arhitectură,
exclusiv în .NET 9 / C#.

### Structură soluție

```
AiComposer.slnx
src/
  AiComposer.Core/          # Logică de domeniu (clasă librărie)
    Models/                 # ArtifactNode, TicketContext, SandboxPolicy, TicketState, ...
    Markdown/               # MarkdownLoader — parsare frontmatter YAML + linking noduri
    State/                  # StateStore — state machine persistentă (SQLite)
    Telemetry/              # TraceLogger — jurnalizare JSONL (role / tokens / cost)
    Security/               # SecurityHelper — policy sandbox Docker + mock env
    Orchestration/          # Orchestrator — asamblare context tipizat per ticket
  AiComposer.Cli/           # Aplicație consolă (CLI)
tests/
  AiComposer.Tests/         # Suite xUnit (14 teste)
examples/artifacts/         # Artefacte Markdown exemplu (Epic, Ticket, ADR, Rules, PS)
```

### Cerințe

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9)

### Build

```bash
dotnet build
```

### Teste

```bash
dotnet test
```

### Rulare CLI

```bash
dotnet run --project src/AiComposer.Cli -- \
  --artifacts examples/artifacts \
  --output output \
  --state-db .state/orchestrator.db \
  --trace-log .state/traces.jsonl
```

**Output generat:**

| Fișier | Conținut |
|---|---|
| `output/<ticket-id>/context.json` | Context strongly-typed (static + dynamic + security) |
| `.state/orchestrator.db` | State machine persistentă (SQLite) |
| `.state/traces.jsonl` | Telemetrie JSONL (trace_id / role / tokens / cost) |

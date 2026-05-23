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

### Execuție agent DeepSeek

Setează cheia API în environment:

```powershell
$env:DEEPSEEK_API_KEY = "your-api-key"
```

Rulează orchestratorul și trimite promptul curent către DeepSeek pentru fiecare ticket:

```powershell
dotnet run --project src\AiComposer.Cli -- `
  --artifacts examples\artifacts `
  --output output `
  --provider deepseek `
  --prompt "Propune implementarea pentru acest ticket și pașii de validare." `
  --role coder `
  --live-output
```

Fiecare rulare generează și fișiere versionate sub `output\<ticket-id>\runs\<timestamp>-<role>\`:

- `agent-request.json` — metadata auditabile despre request (fără API key și fără promptul complet)
- `agent-response.md` — răspunsul generat de provider

Folosește `--live-output` dacă vrei să vezi textul generat de DeepSeek direct în terminal, pe măsură ce răspunsul este produs.

### Execuție agent prin Copilot CLI local

Autentifică mai întâi runtime-ul local:

```powershell
copilot login
```

Rulează același flux de orchestrare folosind providerul Copilot:

```powershell
dotnet run --project src\AiComposer.Cli -- `
  --artifacts artifacts `
  --output output `
  --provider copilot `
  --copilot-model gpt-5 `
  --prompt "Propune implementarea completă pentru ticket." `
  --role coder `
  --live-output
```

Providerul `copilot` folosește CLI-ul local `copilot` în mod non-interactiv, deci nu cere `DEEPSEEK_API_KEY`.

### Aplicare fișiere generate în workspace

Pentru rolul `coder`, AI Composer poate cere răspuns JSON și poate scrie automat fișierele generate într-un workspace țintă:

```powershell
dotnet run --project src\AiComposer.Cli -- `
  --artifacts artifacts `
  --output output `
  --provider deepseek `
  --role coder `
  --prompt "Implementează fișierele jocului Snake pentru acest ticket." `
  --apply-files .\generated\snake-game `
  --live-output
```

Detalii:
- `--apply-files <path>` scrie doar fișiere text în workspace-ul indicat
- path-urile generate trebuie să fie relative și sunt validate să rămână în workspace
- implicit, fișierele existente nu sunt suprascrise; adaugă `--allow-overwrite` dacă vrei update in-place
- fiecare run păstrează audit în `output\<ticket-id>\runs\<timestamp>-coder\applied-files.json`

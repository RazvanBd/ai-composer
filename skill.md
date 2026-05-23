# AI Composer Skill - Orchestrare Agenți pentru Construcție Proiecte

## Descriere

Acest skill permite unui AI LLM să utilizeze sistemul **AI Composer** pentru a construi proiecte software de la zero prin orchestrarea agenților AI și rafinarea dinamică a prompturilor. Sistemul implementează arhitectura A.S.A.D. (Autonomous Software Agentic Deterministic) - o abordare deterministă la dezvoltarea software autonomă.

## Capabilități

- **Construire proiecte de la zero**: Orchestrare completă a ciclului de dezvoltare software
- **Orchestrare multi-agent**: Echipă virtuală de agenți specializați (PO, PM, BA, Architect, Tech Lead, Coder, Reviewer, QA)
- **Context tipizat puternic**: Eliminarea halucinațiilor prin context determinist
- **Rafinare dinamică a prompturilor**: Asamblare just-in-time a contextului pentru fiecare agent
- **Persistență stare**: State machine persistent cu SQLite pentru reziliență
- **Telemetrie și cost tracking**: Monitorizare completă per feature/ticket

## Arhitectură Sistem

### Piloni Fundamentali

1. **Shallow Context (Analiza Statică)**
   - Eliminarea "depth explosion" prin context minimal și relevant
   - Utilizarea semnăturilor de metode în loc de implementări complete
   - Auto-documentare prin XML summary tags

2. **Knowledge Graph (Graf Markdown)**
   - Noduri business: Epic, Ticket, Business Rules
   - Noduri tehnice: Project Summary, ADR, Design Docs
   - Linking explicit între artefacte prin YAML metadata

3. **Prompt Dinamic (Orchestrare)**
   - Context static: rol agent, project summary, coding conventions
   - Context dinamic: graf Roslyn, schema DB, stacktrace-uri
   - Asamblare just-in-time per ticket

4. **Ierarhie Roluri (Echipa Virtuală)**
   - Management: PO, PM, BA
   - Arhitectură: Software Architect, Tech Lead
   - Execuție: Coder, Reviewer, QA

## Structură Tehnică

### Stack
- **.NET 9 / C#** - Platformă exclusivă pentru cod sursă
- **SQLite** - Persistență state machine
- **JSONL** - Format telemetrie și trace logging
- **Markdown + YAML** - Format artefacte și knowledge graph

### Componente Principale

```
src/
  AiComposer.Core/
    Models/              - ArtifactNode, TicketContext, TicketState, TraceEvent
    Markdown/            - MarkdownLoader (parsare YAML frontmatter + linking)
    State/               - StateStore (state machine persistentă)
    Telemetry/           - TraceLogger (JSONL tracing)
    Security/            - SecurityHelper (sandbox policy + mock env)
    Orchestration/       - Orchestrator (asamblare context tipizat)
  AiComposer.Cli/        - CLI pentru rulare orchestrator
```

## Ghid Utilizare pentru AI LLM

### 1. Pregătire Environment

```bash
# Verificare .NET 9 SDK
dotnet --version

# Build sistem
dotnet build

# Rulare teste
dotnet test
```

### 2. Crearea Artefactelor Markdown

#### Project Summary (Constituția Proiectului)
Fișier: `artifacts/project-summary.md`

```markdown
---
id: PS-1
type: project_summary
title: Project Summary
---
Arhitectură: [descriere arhitectură]
Stack: [tehnologii principale]
Anti-pattern: [ce să eviți]

[Detalii arhitecturale - max 200 rânduri]
```

#### Epic (Viziune Macro)
Fișier: `artifacts/epic-{feature}.md`

```markdown
---
id: E-1
type: epic
title: Nume Feature
---
[Descriere viziune și scop feature]
```

#### Business Rules (Constrângeri Reutilizabile)
Fișier: `artifacts/rule-{name}.md`

```markdown
---
id: R-1
type: rule
title: Nume Regulă
---
[Descriere regulă de business reutilizabilă]
```

#### Ticket (Task Granular)
Fișier: `artifacts/ticket-{feature}.md`

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
  - Given [another scenario]
---
[Descriere implementare]
```

#### ADR (Architecture Decision Record)
Fișier: `artifacts/adr-{decision}.md`

```markdown
---
id: ADR-1
type: adr
title: Decizie Arhitecturală
ticket: T-101
---
## Context
[De ce e necesară decizia]

## Decizie
[Ce s-a ales]

## Consecințe
[Implicații și trade-offs]
```

### 3. Rularea Orchestratorului

```bash
dotnet run --project src/AiComposer.Cli -- \
  --artifacts artifacts \
  --output output \
  --state-db .state/orchestrator.db \
  --trace-log .state/traces.jsonl
```

### 4. Output Generat

#### Context per Ticket
Fișier: `output/{ticket-id}/context.json`

Conține obiect `TicketContext` cu:
- `ticketId`: Identificator ticket
- `title`: Titlu task
- `acceptanceCriteria`: Lista criterii Given/When/Then
- `staticContext`: Project summary + coding conventions
- `dynamicContext`: Linked artifacts + ADR-uri
- `securityContext`: Sandbox policy + mock environment

#### State Machine
Fișier: `.state/orchestrator.db` (SQLite)

Stări disponibile:
- `Created` - Ticket nou creat
- `CoderIterating` - Agent Coder lucrează
- `WaitingForReview` - Așteaptă code review
- `QaTesting` - QA rulează teste
- `Done` - Finalizat cu succes
- `Blocked` - Blocat, necesită intervenție

#### Telemetrie
Fișier: `.state/traces.jsonl` (JSONL format)

Câmpuri per trace event:
```json
{
  "traceId": "guid",
  "ticketId": "T-101",
  "role": "tech_lead_orchestrator",
  "tokensIn": 1200,
  "tokensOut": 450,
  "costUsd": 0.025,
  "status": "success",
  "message": "Generated context"
}
```

## Workflow Complet: De la Cerință la Cod

### Faza 1: Construirea Contextului (Planning)

1. **User Request** → Cerință inițială de la utilizator
2. **Product Owner** → Transformă în Epic + Business Rules
3. **Project Manager** → Descompune Epic în Tickets
4. **Business Analyst** → Adaugă Acceptance Criteria (Given/When/Then)
5. **Software Architect** → Generează ADR pentru decizii tehnice
6. **Tech Lead (Orchestrator)** → Asamblează TicketContext tipizat

Toate artefactele rămân pe disk ca fișiere Markdown editabile.

### Faza 2: Bucla Interioară (Implementation)

Pe branch dedicat `feature/task-name`:

1. **Coder Agent** → Implementează cod + teste
2. **Local Build/Test** → `dotnet build && dotnet test`
3. **Feedback Loop** → Dacă erori → înapoi la Coder
4. **Reviewer Agent** → Validare stil, arhitectură, securitate
5. **Repeat** → Până la cod compilabil și acceptat

### Faza 3: Bucla Exterioară (Integration)

1. **Pull Request** → Deschidere automată PR
2. **Environment Setup** → Mediu izolat (Docker sandbox)
3. **QA Agent** → Teste integrare/E2E după Acceptance Criteria
4. **Success** → PR → Ready for Human Review

### Faza 4: Mecanisme de Reziliență

- **Circuit Breaker**: 3 respingeri consecutive → stop
- **Escaladare**: Problema urcă la Tech Lead/Architect
- **Human Intervention**: După eșecuri repetate → etichetă de review manual

## Coding Conventions (Built-in)

Sistemul impune automat următoarele convenții:

1. **No hardcoded secrets** - Toate secretele din variabile de mediu
2. **Environment variables** - Pentru toate integrările externe
3. **XML Summary tags** - Pentru orice metodă publică creată/modificată
4. **Sandbox execution** - Tot codul rulează în containere izolate
5. **Mock environment** - Variabile .env false pentru dezvoltare

## Exemple Practică

### Exemplu: Sistem de Facturare

#### Epic
```markdown
---
id: E-1
type: epic
title: Sistem de facturare
---
Epic pentru emitere și gestionare facturi.
```

#### Regulă Business
```markdown
---
id: R-1
type: rule
title: Sold negativ interzis
---
Clientul nu poate avea sold negativ la emitere factură.
```

#### Ticket
```markdown
---
id: T-105
type: ticket
title: Generare PDF factură
epic: E-1
rules:
  - R-1
acceptance:
  - Given client valid, When request invoice pdf, Then returnează fișier PDF
  - Given sold negativ, When request invoice pdf, Then răspunsul este respins
---
Implementare endpoint pentru export PDF.
```

### Context Generat (JSON)

```json
{
  "ticketId": "T-105",
  "title": "Generare PDF factură",
  "acceptanceCriteria": [
    "Given client valid, When request invoice pdf, Then returnează fișier PDF",
    "Given sold negativ, When request invoice pdf, Then răspunsul este respins"
  ],
  "staticContext": {
    "projectSummary": ["..."],
    "codingConventions": [
      "No hardcoded secrets — always read from environment variables",
      "Use environment variables for all external service integrations",
      "Add concise XML <summary> tags to every public method created or modified"
    ]
  },
  "dynamicContext": {
    "linkedArtifacts": [
      {"id": "E-1", "type": "epic", "title": "Sistem de facturare"},
      {"id": "R-1", "type": "rule", "title": "Sold negativ interzis"}
    ],
    "adrs": []
  },
  "securityContext": {
    "sandboxRequired": true,
    "mockEnvironment": {
      "DATABASE_URL": "mock://localhost/testdb",
      "API_KEY": "mock-key-xxx"
    }
  }
}
```

## Prompturi Recomandate pentru Agenți

### Tech Lead / Orchestrator Prompt

```
Rol: Tech Lead responsabil cu asamblarea contextului pentru execuție.

Input: TicketContext JSON
Task:
1. Analizează acceptance criteria
2. Identifică fișierele implicate (prin analiza statică)
3. Extrage semnături de metode relevante (Shallow Context)
4. Asamblează prompt dinamic pentru Coder Agent
5. Include DOAR informații strict necesare pentru acest ticket

Output: Prompt optimizat pentru Coder cu context minimal și relevant.
```

### Coder Agent Prompt

```
Rol: Software Engineer implementând modificări de cod.

Context Static:
{projectSummary}
{codingConventions}

Context Dinamic:
{linkedArtifacts}
{relevantCodeSignatures}

Task: {ticketTitle}
Acceptance Criteria:
{acceptanceCriteria}

Security Constraints:
- Execută cod DOAR în sandbox
- Folosește mock environment pentru testing
- NO hardcoded secrets

Output:
1. Modificări de cod (diffs aplicabile)
2. Teste unitare/integrare
3. XML summary tags pentru metode noi/modificate
```

### Reviewer Agent Prompt

```
Rol: Code Reviewer validând calitate și conformitate.

Input: Git diff + TicketContext

Verifică:
1. Stil cod conform cu coding conventions
2. Arhitectură aliniată cu Project Summary
3. Securitate: NO hardcoded secrets, NO SQL injection, NO XSS
4. Performance: NO N+1 queries, NO memory leaks
5. Testing: Coverage acceptabil pentru acceptance criteria

Output:
- APPROVE / REQUEST_CHANGES
- Lista sugestii concrete
```

### QA Agent Prompt

```
Rol: Quality Assurance - testare funcțională.

Input: TicketContext + deployed environment

Task:
Pentru fiecare acceptance criterion (Given/When/Then):
1. Configurează precondition (Given)
2. Execută action (When)
3. Validează result (Then)
4. Raportează PASS/FAIL cu detalii

Environment: Sandbox izolat cu mock dependencies

Output: Test report cu status per criterion
```

## Securitate și Izolare

### Sandbox Policy (Built-in)

Sistemul generează automat:
- **Docker isolation** - Execuție în containere efemere
- **Network restrictions** - Acces extern limitat
- **Filesystem constraints** - Write permissions doar în workspace
- **Mock environment** - .env false pentru development/testing

### Mock Environment Example

```json
{
  "DATABASE_URL": "mock://localhost/testdb",
  "API_KEY": "mock-key-xxx",
  "SMTP_HOST": "mock://mailhog:1025",
  "REDIS_URL": "mock://localhost:6379"
}
```

## Telemetrie și Cost Tracking

### Trace Event Structure

Fiecare apel LLM este logat cu:

```json
{
  "traceId": "unique-guid",
  "ticketId": "T-105",
  "role": "coder_agent",
  "tokensIn": 2500,
  "tokensOut": 800,
  "costUsd": 0.042,
  "status": "success",
  "message": "Implementation completed",
  "timestamp": "2026-05-23T06:42:00Z"
}
```

### Cost Analysis Queries

```bash
# Cost per ticket
cat .state/traces.jsonl | jq -r 'select(.ticketId=="T-105") | .costUsd' | awk '{s+=$1} END {print s}'

# Cost per epic (agregare tickets)
# Cost per role
cat .state/traces.jsonl | jq -r 'group_by(.role) | map({role: .[0].role, cost: map(.costUsd) | add})'
```

## State Machine Lifecycle

### Stări Ticket

```
Created → CoderIterating ↔ WaitingForReview → QaTesting → Done
                ↓                                  ↓
              Blocked ←---------------------------┘
```

### Tranziții API

```csharp
// StateStore usage
stateStore.EnsureTicket("T-105");
stateStore.Transition("T-105", TicketState.CoderIterating);
var state = stateStore.GetState("T-105");
```

## Integrare cu AI LLM Extern

### Folosirea ca Tool/Skill

Un AI LLM poate invoca acest sistem ca un tool specializat:

```json
{
  "tool": "ai-composer",
  "action": "build_project",
  "parameters": {
    "projectName": "Invoice System",
    "requirements": "Need a REST API for invoice generation with PDF export",
    "stack": ".NET 9, PostgreSQL, Docker"
  }
}
```

### Workflow AI LLM Agent

1. **Parse cerință** → Înțelege cerința utilizatorului
2. **Generate artifacts** → Creează Epic, Tickets, Rules în Markdown
3. **Run orchestrator** → Execută CLI pentru generare contexte
4. **Read contexts** → Citește fișierele context.json generate
5. **Execute agents** → Invocă agenți specializați cu contextele respective
6. **Iterate** → Urmărește state machine și iterează până la Done
7. **Report** → Analizează telemetria și raportează cost/status

### API Programatic (Viitor)

Pentru integrare directă, sistemul poate expune API:

```csharp
var composer = new AiComposer();

// Creare Epic programatic
var epic = composer.CreateEpic("Invoice System", "...");

// Creare Ticket
var ticket = composer.CreateTicket("PDF Export", epic.Id,
    acceptanceCriteria: new[] {
        "Given valid client, When request PDF, Then return file"
    });

// Execuție orchestrare
var context = composer.BuildContext(ticket.Id);

// Invocare agent
var result = await composer.InvokeAgent("coder", context);
```

## Best Practices pentru Construcție Proiecte

### 1. Începe cu Project Summary

Definește ÎNTOTDEAUNA constituția proiectului înainte de primul ticket:
- Arhitectură macro
- Stack principal
- Anti-patterns de evitat
- Structură folderelor

### 2. Descompune în Epic-uri Mici

Preferă 10 Epic-uri mici vs 1 Epic uriaș:
- Mai ușor de tracked
- Context mai clar
- Paralelizare posibilă

### 3. Tickets Granulare (1-2 ore de lucru)

Un ticket bun:
- 1 fișier principal modificat (max 2-3 fișiere)
- 2-5 acceptance criteria clare
- Referințe explicite la Business Rules

### 4. ADR pentru Decizii Majore

Creează ADR când:
- Alegi între 2+ soluții tehnice
- Schimbi arhitectură existentă
- Adaugi dependență nouă
- Modifici pattern-uri fundamentale

### 5. Iterare și Rafinare

Nu te aștepta la perfecțiune first-time:
- Lasă agenții să itereze
- Folosește feedback loop-urile
- Trustează circuit breaker-ul pentru anti-loop infinit

## Limitări Actuale și Viitor

### MVP Current

- ✅ Orchestrator funcțional cu state machine
- ✅ Markdown artifacts cu YAML frontmatter
- ✅ Context tipizat puternic (TicketContext)
- ✅ Telemetrie JSONL cu cost tracking
- ✅ Security helpers pentru mock environment

### Roadmap Viitor

- ⏳ **Roslyn integration** - Extracție automată semnături metode
- ⏳ **Docker sandbox** - Execuție izolată automată
- ⏳ **Agent implementations** - Agenți Coder/Reviewer/QA funcționali
- ⏳ **Git automation** - Branch creation, PR automation
- ⏳ **Web UI** - Dashboard pentru monitorizare stare și costuri
- ⏳ **LLM provider abstraction** - Support multiple providers (OpenAI, Anthropic, etc.)

## Troubleshooting

### Eroare: "No tickets found"
Verifică că fișierele Markdown au `type: ticket` în frontmatter.

### Eroare: "Failed to parse YAML"
Asigură-te că frontmatter-ul este valid YAML între `---` delimiters.

### Context prea mare
Reduce numărul de linked artifacts sau limitează adâncimea grafului.

### State machine blocat
Resetează manual: `DELETE FROM tickets WHERE ticket_id = 'T-XXX'` în SQLite.

## Concluzie

**AI Composer** oferă o platformă completă pentru construcția deterministă de proiecte software prin orchestrarea agenților AI. Prin eliminarea probabilismului din proces și utilizarea contextului tipizat puternic, sistemul permite:

- **Precizie ridicată** - Context relevant, nu zgomot
- **Transparență totală** - Stare auditabilă pe disk
- **Cost optimizat** - Fiecare agent primește doar ce are nevoie
- **Control uman** - Omul se mută de la execuție la guvernanță

Folosește acest skill pentru a construi proiecte complexe de la zero prin rafinarea incrementală a artefactelor Markdown și orchestrarea dinamică a agenților specializați.

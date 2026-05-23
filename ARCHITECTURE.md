# Arhitectura Sistemului Agentic Determinist (A.S.A.D.)

## Trecerea de la "Vibe Coding" la Inginerie Software Autonomă

---

## 1. Problema fundamentală: de ce eșuează agenții actuali?

În prezent, sistemele de inteligență artificială aplicate în programare (cum ar fi agenții bazați pe Retrieval-Augmented Generation - RAG) suferă de o problemă critică: **diluarea contextului prin zgomot informațional**.

Când unui agent i se cere să modifice o funcționalitate, abordarea clasică este să i se ofere acces semantic la întregul repository. Rezultatul:

1. **Explozia adâncimii (Depth Explosion):** modelul citește zeci de fișiere irelevante doar pentru că au cuvinte cheie similare.
2. **Efectul "Lost in the Middle":** din cauza numărului masiv de tokeni, modelele uită instrucțiunile esențiale plasate la mijlocul promptului.
3. **Halucinații structurale:** neavând vizibilitate perfectă asupra tipurilor de date, modelul inventează proprietăți sau metode care nu există în baza de cod.
4. **Codificare generică ("Comportament implicit"):** modelul revine la "media internetului" pe care a fost antrenat, ignorând arhitectura specifică a proiectului curent.

**Soluție propusă:** înlocuirea căutării semantice (probabilistice) cu **determinismul analizei statice de cod** (ex: Roslyn pentru C#) și fragmentarea execuției într-o ierarhie strictă de agenți care comunică exclusiv prin documente puternic tipizate (Strongly Typed Context), salvate pe disk.

---

## 2. Pilonul 1: indexarea codului și contextul superficial (Shallow Context)

Un agent care trebuie să modifice o metodă nu are nevoie să citească implementarea tuturor claselor cu care interacționează. Are nevoie doar de un "contract".

### 2.1. Analiza statică (rolul "Roslyn")

Sistemul utilizează un analizator de cod (fără intervenție AI) care citește repository-ul și extrage o hartă de navigare. Această hartă conține:

- locația fizică a fișierelor;
- numele claselor și interfețelor;
- semnăturile exacte ale metodelor publice (parametri și tipuri de returnare);
- comentariile XML (`/// <summary>`), singurul limbaj natural pe care agentul îl vede despre restul sistemului.

### 2.2. Buclele de auto-documentare (Self-Healing)

Pentru ca acest Shallow Context să funcționeze pe termen lung, sistemul impune o regulă: **orice metodă creată sau modificată trebuie să aibă un tag `summary` concis și actualizat**.

Astfel, pe măsură ce agenții scriu cod, ei își curăță și ascut singuri uneltele de navigare pentru sarcinile viitoare.

---

## 3. Pilonul 2: graful hibrid de cunoaștere (Knowledge Graph)

Sistemul nu funcționează pe baza unor conversații lungi de chat, ci pe baza unor fișiere Markdown (cu metadate YAML) stocate pe disk, care formează un graf relațional.

### 3.1. Nodurile non-tehnice (business)

Aceste noduri descriu *ce* trebuie făcut:

- **Epic-ul:** descrie viziunea macro.
- **Ticket-ul:** porțiune granulară din Epic, cu scenarii Given / When / Then.
- **Business Rules:** constrângeri absolute reutilizabile în mai multe tichete.

### 3.2. Nodurile tehnice (arhitectură)

Aceste noduri descriu *cum* se implementează:

- **Project Summary (Constituția Proiectului):** document strict de maxim 200 de rânduri, cu arhitectură, stack și anti-pattern-uri.
- **ADR / Design Docs:** decizii tehnice specifice unui tichet.
- **Graful de dependențe (extracție Roslyn):** structură deterministă a fișierelor implicate.

---

## 4. Pilonul 3: arhitectura promptului dinamic (orchestrare)

Orchestratorul (Tech Lead) asamblează contextul Just-in-Time pentru agentul de execuție.

### 4.1. Structura promptului generat

Promptul final către modelul de execuție este format din:

1. **Partea statică (mereu prezentă):**
   - rolul agentului;
   - Project Summary;
   - Coding Conventions;
   - descriere strictă a task-ului curent.
2. **Partea dinamică (injecție contextuală):**
   - graful ușor (semnături Roslyn relevante);
   - schema bazei de date doar când task-ul o cere;
   - stacktrace-ul doar în buclele de bug fixing.

---

## 5. Pilonul 4: ierarhia rolurilor (echipa virtuală)

Fiecare agent este un apel izolat către LLM, cu rol și context clar delimitate.

### Nivel management & business

1. **Product Owner (PO):** transformă cerința umană vagă în Epic + Business Rules.
2. **Project Manager (PM):** sparge Epic-ul în Tickets și monitorizează blocajele.
3. **Business Analyst (BA):** completează ticket-ul cu Acceptance Criteria verificabile.

### Nivel arhitectură

4. **Software Architect:** generează ADR și actualizează Project Summary când e necesar.
5. **Tech Lead (Orchestrator):** produce promptul dinamic și delimitează zona de impact.

### Nivel execuție (factory)

6. **Coder:** generează modificări de cod structurate și aplicabile pe disk.
7. **Reviewer:** validează static diff-ul (stil, arhitectură, performanță, securitate).
8. **QA:** validează funcțional criteriile de acceptanță în mediu izolat.

---

## 6. Fluxul de execuție și rezoluția problemelor (SDLC Pipeline)

### Faza 1: construirea contextului

Flux liniar: User request -> PO -> PM -> BA -> Architect -> Tech Lead. Toate artefactele rămân pe disk, editabile de om în orice moment.

### Faza 2: bucla interioară (Fail Fast)

Pe branch dedicat (`feature/task-name`):

1. Coder implementează + adaugă teste locale.
2. Se rulează build/test local.
3. Dacă apar erori: feedback-ul merge înapoi la Coder.
4. Dacă trece: Reviewer validează.
5. Bucla continuă până la cod compilabil și acceptat.

### Faza 3: bucla exterioară (Pull Request)

1. Se deschide PR automat.
2. Se ridică aplicația în mediu izolat.
3. QA rulează teste de integrare/E2E după Acceptance Criteria.
4. Dacă totul trece, PR devine *Ready for Human Review*.

### Faza 4: mecanism de escaladare (anti loop infinit)

- **Circuit Breaker:** 3 respingeri consecutive pe același fișier opresc bucla.
- **Escaladare:** problema urcă la Tech Lead/Architect pentru redesign sau few-shot.
- **Escaladare supremă:** după eșecuri repetate, tichetul primește etichetă de intervenție umană.

---

## 7. Avantajele competitive

1. **Precizie ridicată:** context tipizat, redus și relevant.
2. **Transparență totală:** starea este salvată și auditabilă.
3. **Cost optimizat:** fiecare rol primește doar contextul necesar.
4. **Control uman integrat:** omul se mută de la execuție la guvernanță arhitecturală.

---

## 8. Completări esențiale pentru producție (DevOps & SecOps)

### 8.1. Securitate și izolare (sandboxing strict)

Execuția codului și testelor trebuie făcută exclusiv în **containere efemere (Docker)** cu:

- acces de rețea externă restricționat;
- permisiuni de scriere limitate la workspace-ul proiectului.

Această izolare reduce riscul de ștergere accidentală de date, exfiltrare și instalare de dependențe neaprobate.

### 8.2. Telemetrie și cost per feature (observability)

Sistemul trebuie să implementeze **tracing (ex: OpenTelemetry)**.

Fiecare Epic/Ticket primește un `TraceId`, iar fiecare apel LLM este logat cu:

- rolul agentului;
- numărul de iterații;
- consumul de tokeni;
- costul estimat per feature.

### 8.3. Gestionarea secretelor și dependențelor externe

Orchestratorul trebuie să includă un modul de **mocking injectabil**:

- injectează `.env` false în task-uri de dezvoltare/test;
- redirecționează integrarea spre servicii mock (ex: WireMock);
- impune reguli de coding conventions: consum din variabile de mediu, **fără hardcodare de secrete**.

### 8.4. Resiliența orchestratorului (state machine)

Orchestratorul nu trebuie să țină starea doar în RAM. Trebuie să ruleze ca **state machine** (Saga/Actor Model), persistând starea ticket-ului în:

- fișier local `.state`, sau
- SQLite minimalist.

La restart, fluxul este reluat exact din starea anterioară, fără dublarea apelurilor LLM.

---

## 9. Concluzie și următorul pas

Această arhitectură definește o trecere de la agenți probabilistici la execuție deterministă, auditabilă și controlabilă operațional.

**Următorul pas recomandat (MVP):**
1. extractorul Roslyn pentru shallow graph;
2. orchestratorul de bază cu state machine;
3. instrumentarea OTel și metrica de cost per ticket;
4. runtime izolat în containere efemere cu mocking injectabil.

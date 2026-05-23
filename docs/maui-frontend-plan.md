# Plan issue-uri GitHub — frontend .NET MAUI pentru ai-composer

## 1) Viziune de produs
Frontend-ul .NET MAUI transformă ai-composer într-un desktop orchestration studio, fără a schimba motorul existent.  
CLI-ul rămâne sursa unică de execuție și logică de business, iar MAUI oferă UX pentru explorare artefacte, rulare tickete și inspectare output-uri.  
MVP-ul este intenționat mic, practic și demo-ready: dashboard, artifacts explorer, tickets list, run console cu live logs și viewer pentru fișiere generate.  
Integrarea se face incremental, cu risc redus, prin lansarea CLI-ului ca proces separat.

## 2) Principii de arhitectură
- CLI-ul existent rămâne engine-ul oficial; proiectele `AiComposer.Core` și `AiComposer.Cli` nu se refactorizează pentru acest frontend.
- MAUI este strat de prezentare (shell UX), nu duplică logică de orchestrare, state machine sau security policy.
- Integrarea MAUI ↔ engine se face exclusiv prin execuția CLI-ului ca proces separat, în spatele unor interfețe de servicii.
- Separare strictă pe straturi: `Views` / `ViewModels` / `Services` / `Models`.
- Faza 0 trebuie finalizată integral înainte de Faza 1; issue-urile din Faza 1 pot rula în paralel după Faza 0.
- Faza 3 este opțională pentru MVP.

## 3) Structură recomandată pentru proiectul MAUI
```text
src/
  AiComposer.Maui/
    Platforms/
    Views/
    ViewModels/
    Services/
      Abstractions/
      Implementations/
    Models/
    Resources/
    AppShell.xaml
    MauiProgram.cs
docs/
  maui-frontend-plan.md
```

## 4) Lista completă de issue-uri (în ordinea de implementare)

### 1. [MAUI] Create new MAUI project in solution
**Faza:** 0 — Fundație (nu se atinge engine-ul)  
**Prioritate:** High  
**Dependențe:** N/A

**Scop:**  
Adăugarea proiectului `src/AiComposer.Maui` în soluție, ca proiect separat, fără modificări în proiectele existente ale engine-ului.

**Criterii de acceptare:**
- [ ] Proiectul `AiComposer.Maui` este creat în `src/` și adăugat în `AiComposer.slnx`.
- [ ] Build-ul soluției rămâne funcțional după adăugare.
- [ ] Nu sunt schimbate fișierele din `src/AiComposer.Core` și `src/AiComposer.Cli`.

**Note tehnice:**
- Folosește template-ul standard `.NET MAUI App`.
- Integrarea este strict prin proiect nou; engine-ul CLI nu se modifică.

### 2. [MAUI] Define solution structure and project references
**Faza:** 0 — Fundație (nu se atinge engine-ul)  
**Prioritate:** High  
**Dependențe:** #1

**Scop:**  
Definirea structurii de foldere MAUI și referințelor necesare, cu separare clară UI vs. integrare engine.

**Criterii de acceptare:**
- [ ] Structura de directoare urmează modelul propus (`Views`, `ViewModels`, `Services`, `Models` etc.).
- [ ] Referințele de proiect/pachete sunt minime și justificate.
- [ ] Separarea responsabilităților UI/servicii este documentată succint.

**Note tehnice:**
- Nu se adaugă coupling direct la logica engine-ului.
- Nu se modifică fișierele existente din proiectele engine (`AiComposer.Core`, `AiComposer.Cli`).

### 3. [MAUI] Add service abstractions (interfaces)
**Faza:** 0 — Fundație (nu se atinge engine-ul)  
**Prioritate:** High  
**Dependențe:** #2

**Scop:**  
Definirea interfețelor pentru stratul de aplicație UI: `IWorkspaceService`, `IRunService`, `IArtifactsService`, `ITicketService`, `IOutputService`, `ISettingsService`.

**Criterii de acceptare:**
- [ ] Interfețele enumerate există în `Services/Abstractions`.
- [ ] Semnăturile susțin scenariile MVP (load workspace, run ticket, logs, output inspect).
- [ ] ViewModels consumă interfețe, nu implementări concrete.

**Note tehnice:**
- Contract-first pentru a permite implementări alternative ulterioare.
- Fără duplicare de logică de orchestrare din engine.
- Nu se modifică fișierele existente din engine.

### 4. [MAUI] Implement CLI process integration layer
**Faza:** 0 — Fundație (nu se atinge engine-ul)  
**Prioritate:** High  
**Dependențe:** #3

**Scop:**  
Implementarea `CliRunService` care pornește CLI-ul ai-composer ca proces separat, transmite argumente și citește stdout/stderr în timp real, abstractizat prin `IRunService`.

**Criterii de acceptare:**
- [ ] `CliRunService` execută CLI-ul prin `ProcessStartInfo` cu argumente configurabile.
- [ ] Output-ul stdout/stderr este capturat și expus către UI (stream/event/callback).
- [ ] Există suport pentru anulare/stop run în mod controlat.

**Note tehnice:**
- Integrarea este exclusiv process-based (fără refactoring/embedding engine).
- Nu se modifică fișierele existente din `AiComposer.Core` sau `AiComposer.Cli`.
- Gestionarea erorilor include cod de exit, timeout și output final.

### 5. [MAUI] Add shell navigation and main layout
**Faza:** 0 — Fundație (nu se atinge engine-ul)  
**Prioritate:** High  
**Dependențe:** #1, #2, #3, #4

**Scop:**  
Configurarea `AppShell` cu navigare între secțiunile principale (Dashboard, Artifacts, Tickets, Run Console, Generated Workspace, Settings).

**Criterii de acceptare:**
- [ ] `AppShell.xaml` definește rutele și navigarea de bază.
- [ ] Layout-ul principal permite extindere incrementală pe pagini.
- [ ] Aplicația pornește cu shell funcțional pe platforma de dezvoltare țintă.

**Note tehnice:**
- Navigarea este UI-only; nu adaugă logică de business în code-behind.
- Nu se modifică proiectele existente ale engine-ului.

---

### 6. [MAUI] Dashboard — workspace overview and quick actions
**Faza:** 1 — MVP Core  
**Prioritate:** High  
**Dependențe:** #1, #2, #3, #4, #5

**Scop:**  
Oferă overview pentru workspace și acțiuni rapide: `Open Workspace`, `Validate Artifacts`.

**Criterii de acceptare:**
- [ ] Afișează workspace curent și indicatori de status artifacts/tickets.
- [ ] Include butoane funcționale: `Open Workspace`, `Validate Artifacts`.
- [ ] Actualizează vizual statusul după acțiuni.

**Note tehnice:**
- Datele vin prin interfețele de servicii definite în Faza 0.
- Integrarea cu engine rămâne prin CLI proces separat; nu se modifică fișierele engine.

### 7. [MAUI] Artifacts Explorer — list and preview
**Faza:** 1 — MVP Core  
**Prioritate:** High  
**Dependențe:** #1, #2, #3, #4, #5

**Scop:**  
Afișează lista de artifacts (Project Summary, Epics, Rules, Tickets, ADRs), preview lateral și status de validare.

**Criterii de acceptare:**
- [ ] Lista artifacts poate fi filtrată pe tip.
- [ ] Selectarea unui artifact afișează preview text în panou lateral.
- [ ] Statusul de validare este vizibil per artifact.

**Note tehnice:**
- Citirea datelor se face prin servicii UI dedicate și/sau output CLI.
- Nu se schimbă parserul/loaderul din engine; nu se modifică proiectele existente ale engine-ului.

### 8. [MAUI] Tickets List — status and selection
**Faza:** 1 — MVP Core  
**Prioritate:** High  
**Dependențe:** #1, #2, #3, #4, #5

**Scop:**  
Listă de tickets cu status badges (Draft, Ready, Running, Blocked, Done) și selecție ticket curent.

**Criterii de acceptare:**
- [ ] Lista tickets este afișată cu status badge clar.
- [ ] Există selecție ticket curent pentru acțiunile de run.
- [ ] Schimbarea statusului este reflectată în UI după rulări.

**Note tehnice:**
- Statusurile sunt mapate din datele furnizate de engine/CLI, fără logică duplicată în UI.
- Nu se modifică fișiere din `AiComposer.Core` și `AiComposer.Cli`.

### 9. [MAUI] Run Console — start run and live log streaming
**Faza:** 1 — MVP Core  
**Prioritate:** High  
**Dependențe:** #1, #2, #3, #4, #5, #8

**Scop:**  
Permite pornirea unui run pentru ticketul selectat și afișarea live a output-ului CLI, plus control de execuție (`Run Selected Ticket`, `Stop Current Run`).

**Criterii de acceptare:**
- [ ] Butonul `Run Selected Ticket` pornește execuția pentru ticketul selectat.
- [ ] Console-ul afișează live stdout/stderr din procesul CLI.
- [ ] Butonul `Stop Current Run` întrerupe controlat execuția curentă.

**Note tehnice:**
- Implementarea folosește `CliRunService` din Faza 0.
- Fără apeluri directe în engine intern; integrare doar prin proces CLI separat.
- Nu se modifică fișierele existente ale engine-ului.

### 10. [MAUI] Generated Workspace Viewer — file tree and preview
**Faza:** 1 — MVP Core  
**Prioritate:** High  
**Dependențe:** #1, #2, #3, #4, #5, #9

**Scop:**  
Vizualizare output generat: arbore fișiere, preview text și marcaj pentru fișiere noi/modificate.

**Criterii de acceptare:**
- [ ] File tree afișează fișierele generate pentru run-ul selectat.
- [ ] Selectarea unui fișier arată preview text simplu.
- [ ] Fișierele noi/modificate sunt marcate vizual.

**Note tehnice:**
- Se citesc artefactele de output generate de CLI; UI nu re-implementează generarea.
- Nu se modifică fișierele din proiectele existente ale engine-ului.

---

### 11. [MAUI] Settings page — workspace paths and model configuration
**Faza:** 2 — Completare MVP  
**Prioritate:** Medium  
**Dependențe:** #1, #2, #3, #4, #5

**Scop:**  
Pagină de setări pentru workspace path, output path, provider AI, model și run policies.

**Criterii de acceptare:**
- [ ] Utilizatorul poate seta și salva path-urile principale.
- [ ] Providerul/modelul pot fi configurate din UI.
- [ ] Setările sunt încărcate la startup și aplicate la run.

**Note tehnice:**
- Persistență locală simplă (preferabil settings store nativ MAUI).
- Setările influențează doar argumentele CLI; nu modifică engine-ul.

### 12. [MAUI] Run All Ready Tickets action
**Faza:** 2 — Completare MVP  
**Prioritate:** Medium  
**Dependențe:** #8, #9, #11

**Scop:**  
Acțiune de rulare secvențială pentru toate ticketele cu status `Ready`, cu log live consolidat.

**Criterii de acceptare:**
- [ ] Sunt selectate automat doar ticketele în status `Ready`.
- [ ] Rulările se execută secvențial, cu afișare progres.
- [ ] Erorile sunt raportate per ticket fără blocarea întregii sesiuni.

**Note tehnice:**
- Orchestrarea secvențială se face în UI layer peste `IRunService`.
- Engine-ul rămâne neschimbat și este apelat doar prin CLI proces separat.

### 13. [MAUI] Audit and applied files panel in Run Console
**Faza:** 2 — Completare MVP  
**Prioritate:** Medium  
**Dependențe:** #9, #10, #11

**Scop:**  
Extinderea Run Console pentru afișare audit trail și listă de `applied files` după execuție.

**Criterii de acceptare:**
- [ ] Panelul afișează datele de audit disponibile pentru run-ul selectat.
- [ ] Lista `applied files` este afișată clar și poate fi inspectată.
- [ ] UI evidențiază diferența între run reușit și run eșuat.

**Note tehnice:**
- Sursa datelor: fișierele de audit produse de CLI (`runs/*`).
- Nu se modifică pipeline-ul intern al engine-ului.

---

### 14. [MAUI] Validate Artifacts action with detailed results panel
**Faza:** 3 — Îmbunătățiri post-MVP (opțional)  
**Prioritate:** Low  
**Dependențe:** #6, #7, #9

**Scop:**  
Acțiune dedicată de validare artifacts cu rezultate detaliate per artifact și tip de eroare.

**Criterii de acceptare:**
- [ ] `Validate Artifacts` rulează validarea la cerere.
- [ ] Rezultatele sunt grupate și filtrabile per artifact.
- [ ] Erorile/warning-urile sunt ușor de navigat din UI.

**Note tehnice:**
- Validarea se bazează pe capabilitățile existente ale engine-ului/CLI.
- Fără modificări în proiectele engine.

### 15. [MAUI] Generate Context action
**Faza:** 3 — Îmbunătățiri post-MVP (opțional)  
**Prioritate:** Low  
**Dependențe:** #6, #8, #9

**Scop:**  
Acțiune UI pentru declanșarea explicită a generării contextului pentru ticketul selectat.

**Criterii de acceptare:**
- [ ] Butonul `Generate Context` este disponibil în ecranul relevant.
- [ ] Acțiunea generează contextul prin CLI și raportează rezultatul.
- [ ] Link rapid către `context.json` generat.

**Note tehnice:**
- Execută command-ul CLI corespunzător, ca proces separat.
- Nu se modifică codul existent al engine-ului.

### 16. [MAUI] Open Output button — open generated folder in OS explorer
**Faza:** 3 — Îmbunătățiri post-MVP (opțional)  
**Prioritate:** Low  
**Dependențe:** #10

**Scop:**  
Buton `Open Output` care deschide folderul de output în file manager-ul nativ al sistemului de operare.

**Criterii de acceptare:**
- [ ] Butonul `Open Output` este disponibil în Dashboard/Run Console.
- [ ] Folderul corect este deschis pe platforma curentă.
- [ ] UI tratează elegant cazul în care path-ul nu există.

**Note tehnice:**
- Folosește API-uri cross-platform MAUI pentru lansare path.
- Nicio modificare în engine; funcționalitate strict de UX.

### 17. [MAUI] Compare Outputs — basic diff viewer for generated files
**Faza:** 3 — Îmbunătățiri post-MVP (opțional)  
**Prioritate:** Low  
**Dependențe:** #10, #13

**Scop:**  
Comparare simplă între două versiuni ale unui fișier generat.

**Criterii de acceptare:**
- [ ] Utilizatorul poate selecta două versiuni de fișier.
- [ ] UI afișează un diff textual de bază (line-by-line).
- [ ] Modificările sunt evidențiate vizual minim (add/remove/change).

**Note tehnice:**
- MVP pentru diff rămâne simplu, fără editor avansat.
- Datele provin din output-urile deja generate de CLI.
- Nu se modifică fișierele existente ale engine-ului.

### 18. [MAUI] Ticket Board — kanban-style visualization
**Faza:** 3 — Îmbunătățiri post-MVP (opțional)  
**Prioritate:** Low  
**Dependențe:** #8, #12

**Scop:**  
Vizualizare tip board cu coloane pe status (Draft, Ready, Running, Blocked, Done), opțional cu drag-and-drop.

**Criterii de acceptare:**
- [ ] Tickets sunt grupate vizual pe coloane de status.
- [ ] Refresh-ul board-ului reflectă starea curentă după rulări.
- [ ] Drag-and-drop (dacă este inclus) este marcat clar ca opțional.

**Note tehnice:**
- Implementare orientată pe vizualizare, nu pe rescriere de workflow intern.
- Engine-ul CLI rămâne neschimbat.

### 19. [MAUI] Run history and session list
**Faza:** 3 — Îmbunătățiri post-MVP (opțional)  
**Prioritate:** Low  
**Dependențe:** #9, #10, #13

**Scop:**  
Istoric de rulări/sesiuni cu posibilitate de reinspectare a output-urilor anterioare.

**Criterii de acceptare:**
- [ ] UI listează sesiunile anterioare disponibile în output.
- [ ] O sesiune poate fi selectată pentru inspectare detaliată.
- [ ] Navigarea între sesiuni și fișiere asociate este fluentă.

**Note tehnice:**
- Sursa istoricului este structura de output existentă a CLI-ului.
- Nu se modifică fișierele din proiectele existente ale engine-ului.

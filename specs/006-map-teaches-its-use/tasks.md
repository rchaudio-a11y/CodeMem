---

description: "Task list for CodeMem 006 — the map teaches its own use: server instructions, the usage kit, and rename_candidates"
---

# Tasks: CodeMem 006 — The Map Teaches Its Own Use

**Input**: Design documents from `/specs/006-map-teaches-its-use/`

**Prerequisites**: all present — plan.md (§STOP 1, eleven decisions, **ruled 2026-09-29**; each task names the decision it
rests on), spec.md (clarified 2026-09-29, five rulings, the six ratified hashes in Q3's table), research.md (R74–R84),
data-model.md, contracts/tools.md, contracts/usage-texts.md, quickstart.md.
- Constitution **v1.5.0** (`806b524`).
- Baseline: `main` at `806b524`, Debug, 196 passed / 1 failed / 9 skipped (the failure is the version pin T002
  retires), plus one unnamed failure seen once.
- **Branch**: `006-map-teaches-its-use` from `806b524`; the 006 artefacts are committed there before the first Red.

**Analyze (2026-09-29)**: fourteen findings, ruled by the Architect and applied here, in the spec, the plan and the
quickstart. Finding IDs are cited where a task changed.
- **C1, G1**: four FIREs added, three in T014 (both run refusals, and a `SqliteConnection` in the reader) and one in
  T026 (the skill's phrase).
- **G2**: FR-513 is "the fact, with one FIRE".
- **G3**: B08 (11), unfiltered MemOS, with no timing assert.
- **G4**: B12 (9), both filters.
- **U1**: `ListToolAnnotations` and `ConfigPath` added.
- **U2**: run 3 asserted from the map.
- **G5**: accepted as is.
- **G6**: one line at the close.
- **I1–I5**: wording.

**Tests**: REQUIRED (Article II; spec FR-529). The loop for each: write → run → confirm Red *for the stated reason* →
record it in the test file header → implement → confirm Green → record. Every guard carries a `' FIRE:` line.
- New: `Bridge/B12_RenameCandidatesTests`, `Guards/UsageTextsGateTests`, `Support/RenameScenario`.
- Amended in place, each after its named Red: B01 (5), B05 (18), B08 (+ (9), (10), (11)), B09 (4) and (6).
- Retired: `BridgeStandaloneGateTests` (4).
- **Order rule (CON2)**: no implementation slice lands before its behaviour test exists and is observed Red.
- **Production route (CON3)**: every new refusal and the tool's answers are driven once through `CodeMem.Bridge.exe
  serve` over stdio. `BridgeHost` serves the fast in-process facts and is never the production-route evidence.
- **No second stop**: the work stops only for a Red not named in plan §Test design, a `DIFF` in quickstart §Verbatim,
  or a recurring intermittent failure (T003).

**Organization**:
- Phase 1 setup: the branch; `main` green again; the named baseline.
- Phase 2 foundational: test support both P1 stories' Red batteries need.
- Phases 3–5, the three stories in priority order:
  - US1 `rename_candidates`;
  - US2 the instructions and the `symbol_search` sentence;
  - US3 the kit files, the README, the fragment and the process document.
- Phase 6: polish, the acceptance on a copy, and the close.
- **The MVP is Phases 1–4**: US1 and US2 are both P1 and ship together, because the instructions name
  `rename_candidates`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1–US3 from spec.md
- Every path is repository-relative

## Standing rules for every task

- **Source files**: header block first (`' File:`, `' Project:`, `' Description:`, `' Author: RCH Automation LLC`,
  `' Created: <date>`). An edited file gets a dated line naming the task. XML docs on every public declaration; one
  type per file; Option Strict/Explicit On, Infer Off (project-wide).
- **SQL**: no SQL literal anywhere in `src/CodeMem.Bridge/` or `src/CodeMem.Bridging/`. Every query is a named `Read…`
  method under `src/CodeMem.Core/Repositories/`.
- **Text in the bridge projects**: no literal there may carry a word either SQL gate matches: `select`, `insert`,
  `update`, `delete`, `create table` and `pragma` in any case, and `CREATE`, `DROP`, `ALTER`, `ATTACH` in capitals.
  No case-sensitive `memos` either (005's standalone gate). `New SqliteConnection` stays in `MapDatabase.vb` only.
- **Verbatim texts come from the store, never retyped.** Read doc 191490's `document_sections` read-only and take
  the block by the spec's Q3 rule (LF). A task that lands one runs quickstart §Verbatim for it before it closes; a
  `DIFF` stops the work for a ruling (spec Q3).
- **Map first (constitution v1.5.0)**: before a structure question about CodeMem's own code (who calls, constructs or
  uses X; is it safe to change), call `map_status`, and `extract solutionKey=CodeMem` if it is behind. A "nothing",
  "exactly N" or "only X" claim in a task record names the tool and the count.
- **Red/Green/FIRE lines** go in the test file header; every FIRE injection is reverted before the task closes.
- **Retired test facts** are moved, never deleted, to `_Archive/006-map-teaches-its-use/` with their header intact
  (Article XIV).
- **Fixtures**: the committed ones under `tests/CodeMem.Tests/Fixtures/` are never edited by a test; every mutation
  runs on a `FixtureCopy` or a copied `TempMap`. `C:\_DB\codemem.sqlite` is never opened by a test; T031 works on a
  copy.
- **Untouched**: nothing under `src/CodeMem.Extraction/` or `src/CodeMem.Extractor/`, and
  `src/CodeMem.Core/Schema/SchemaRepository.vb` (Q11). Nothing in `rchaudio-a11y\MemOS`. Nothing under `~/.claude`.

---

## Phase 1: Setup — the branch, `main` green again, the named baseline

**Purpose**: The feature branch carries the artefacts; the red version pin is retired to the archive as ruled; the
suite's baseline is named test by test before any product code changes.

- [X] T001 Create the branch and record the starting state:
  - `git switch -c 006-map-teaches-its-use 806b524`.
  - Commit `specs/006-map-teaches-its-use/` (spec, plan, research, data-model, contracts/, quickstart, checklists/;
    this file included) as the first commit: "docs(006): spec, plan, research, contracts, tasks — STOP 1 ruled".
  - Record `git -C C:\Users\rchau\source\repos\rchaudio-a11y\MemOS status --porcelain` (before) under "What must not
    happen" in `specs/006-map-teaches-its-use/quickstart.md`.
- [X] T002 Retire `BridgeStandaloneGateTests` (4) (STOP 1 decision 1):
  - Create `_Archive/006-map-teaches-its-use/tests/BridgeStandaloneGateTests_RetiredFacts.vb` holding fact (4)'s
    text, `TheConstitutionIsVersionOneFour`, its XML doc and its `<Fact>`, verbatim. Give it a header naming
    `tests/CodeMem.Tests/Guards/BridgeStandaloneGateTests.vb` as its origin, its 005 RED/GREEN lines, and "RED:
    2026-09-29 on `main` at `806b524` (Expected start `**Version**: 1.4.0`; the v1.5.0 amendment moved the line) —
    retired 2026-09-29, STOP 1 decision 1".
  - Create `_Archive/006-map-teaches-its-use/README.md` saying what moved, from where, and why, quoting the
    Architect's ruling verbatim: "fact (3) pins Article IX's text, which is what it protected; (4) now only breaks
    main on every amendment".
  - In `tests/CodeMem.Tests/Guards/BridgeStandaloneGateTests.vb`:
    - remove fact (4);
    - revise the `' Description:` line (drop "the constitution is v1.4.0");
    - revise the class summary to three facts;
    - add a dated header line: "2026-09-29 (006, T002): (4) retired to `_Archive/006-map-teaches-its-use/` — red on
      main since 806b524".

  `ConstitutionPath()` stays: fact (3) still uses it. Run `BridgeStandaloneGateTests` → 3 facts, all green.
- [X] T003 The named baseline (plan §Test design "Red on `main`"):
  - Run `dotnet test CodeMem.sln -c Debug --nologo --logger "trx;LogFileName=baseline-006-<n>.trx"` three times
    (n = 1, 2, 3).
  - From each TRX, record passed / failed / skipped, the duration and every failing test by name. Expected: 196
    passed / 0 failed / 9 skipped, 205 total.
  - Record the three lines under "Build and test" in `specs/006-map-teaches-its-use/quickstart.md`. The TRX files
    stay out of the tree: delete them after recording, or confirm `TestResults/` is ignored.
  - **If any failure appears, record its name and message, stop, and raise it with the Architect as a pre-existing
    intermittent defect.** It is not fixed inside 006 (STOP 1 decision 1).
  - Commit Phase 1: "test(006, T002–T003): retire the constitution version pin; the named baseline".

**Checkpoint**: `main`'s one reproducible Red is gone; the baseline is named test by test; no product code has
changed.

---

## Phase 2: Foundational — test support for the two P1 Red batteries

**Purpose**: The helpers B12 and `UsageTextsGateTests` need exist before either Red is written. Test code only.

**⚠️ CRITICAL**: No user story work until T006 records its check.

- [X] T004 [P] Add `Public Function InitializeResult() As JsonElement` to `tests/CodeMem.Tests/Support/BridgeProcess.vb`
  (research R74, R79):
  - It sends the same `initialize` request and `notifications/initialized` as `Initialize()` and returns the whole
    `result` element, cloned.
  - `Initialize()` keeps returning `serverInfo` and is re-expressed as `InitializeResult().GetProperty("serverInfo")`
    (one door for the handshake).
  - Add `Public Function ListToolAnnotations() As Dictionary(Of String, JsonElement)`: one `tools/list`, each tool's
    `annotations` element cloned, keyed by name. It is needed for B12 (6)'s `readOnlyHint` (analyze U1).
  - Dated header line.
- [X] T005 [P] Test-side mutation helpers (research R78, R79; STOP 1 decision 8):
  - In `tests/CodeMem.Tests/Support/TwinScenario.vb`, `Prepare` goes from `Private Shared` to `Friend Shared`, with a
    dated header line: "a second caller, `RenameScenario`; no third, so no abstraction".
  - In `tests/CodeMem.Tests/Support/MapQueries.vb` (the exempt test SQL file), add
    `Public Sub DeleteCandidate(db As String, candidateId As Long)` running `DELETE FROM rename_candidates WHERE id =
    @id` on a read-write connection. Its XML doc says it exists only to make a completed run disagree with its
    recorded count on a copied map. Add `Public Function ReadCandidateIds(db As String, runId As Long) As List(Of
    Long)` (`SELECT id FROM rename_candidates WHERE run_id = @run_id ORDER BY id`). Dated header lines.
- [X] T006 Create `tests/CodeMem.Tests/Support/RenameScenario.vb` (research R79; STOP 1 decision 8). An
  `IDisposable` class fixture holding `Copy As FixtureCopy`, `Other As FixtureCopy`, `Map As TempMap`, the two
  solution ids, the four run ids (`SampleRun1`, `SampleRun2`, `SampleRun3Failed`, `OtherRun1`), `ConfigPath As String`
  (the file written by `BridgeHost.WriteConfig(Map.Path, Nothing, False, False)`, exposed for `BridgeProcess.Serve`;
  analyze U1) and `Host As BridgeHost` over that config. Its constructor:
  1. `TwinScenario.Prepare(Copy)` and `TwinScenario.Prepare(Other)`.
  2. Extract `Copy` as key `Sample` (run 1), then `Other` as key `Other` (run 1).
  3. On `Copy`: `Replace("Sample.Lib/Widgets.vb", "Sub Describe(", "Sub Explain(")` and `Replace("Shared/Twin.vb",
     "Public Function Name()", "Public Function Label()")`. Extract `Sample` (run 2): exit `Success`; `ReadCandidates`
     for run 2 holds 3 rows, else throw naming the count.
  4. On `Copy`: `Replace("Sample.Lib/Widgets.vb", "Sub Explain(", "Sub Narrate(")`. Extract `Sample` with `New
     RunSeams With {.CorruptStagedCounts = Sub(c As RunCounts) c.SymbolsMatched += 1}`: exit `ResidualMismatch`, else
     throw. That is run 3: failed, recording 1 candidate, publishing none. Assert it from the map (analyze U2): run
     3's outcome is `failed`, its recorded `rename_candidates` is 1, and `ReadCandidates` for run 3 is empty. Throw
     naming the figures otherwise, so B12 (1)'s failed-run case cannot be vacuous.

  Helpers:
  - `SymbolIdByDocId(solutionId, docCommentId, active As Boolean)`.
  - `CopyMap() As TempMap`, a byte copy of `Map` for the mismatch fact.

  Build, then a throwaway check: construct it once in an ad-hoc fact, confirm the counts, and delete the fact. Record
  in the header: "run 2: 3 candidates (Describe→Explain; Name→Label in `Sample.App.Shared.Twin` and in
  `Sample.Lib.Shared.Twin`)".

**Checkpoint**: `InitializeResult`, the mutation helpers and the rename scenario exist; no product code has changed.

---

## Phase 3: User Story 1 — The bridge answers "what became of this id?" (Priority: P1) 🎯 MVP (with Phase 4)

**Goal**: A ninth tool, `rename_candidates`, reads the map's rename evidence for one solution. It gives bare ids with
each side's project, runs newest first with the count check stated, and under `retiredSymbolId` only the runs that
matter. It has three new refusals, and `SymbolRetired` names it.

**Independent Test**: B12 (1)–(9) on `RenameScenario`, the refusals through the executable, the map byte-identical;
B08 (9)–(11) armed for T031.

- [X] T007 [US1] Create `tests/CodeMem.Tests/Bridge/B12_RenameCandidatesTests.vb` (FR-501–FR-509, FR-515; spec US1;
  `<Collection("Fixture")>`, `IClassFixture(Of RenameScenario)`) with nine facts. Assertions are on the parsed JSON of
  data-model §2–§4, and every id comes from the scenario, never a literal.
  - (1) `UnfilteredListsEveryRunAndTheTwinsAsSeparateCandidates`: `rename_candidates(solutionKey "Sample")`.
    - `runs` is `[run3, run2, run1]` by id descending.
    - Run 2: `countChecked` true, `renameCandidatesRecorded` 3, `candidatesReturned` 3. Run 1: checked, 0 = 0.
      Run 3: `outcome` "failed", `renameCandidatesRecorded` 1, `candidatesReturned` 0, `countChecked` false,
      `countNotCheckedReason` "failed run".
    - `total` 3.
    - The two `Label` candidates have distinct `retired.id`, `new.id`, `retired.project.name` ("Sample.App",
      "Sample.Lib") and `container.id`, and equal `name`, `path` and `line`: bare ids, never folded.
    - `samePath` true and `offsetDistance` a number on each; `note` null.
  - (2) `RetiredSymbolIdListsOnlyTheRunsThatMatter`: `retiredSymbolId` = the retired `Describe` id.
    - One candidate, whose `new` is `Explain`.
    - `runs` is exactly `[run2]`; `symbol.isActive` false, `symbol.lastSeenRunId` run 1, `symbol.retiredInRunId`
      run 2.
    - The run carries `countChecked` false, reason "filtered by retiredSymbolId".
  - (3) `TheRetiredRefusalNamesTheToolAndItAnswers` (CON3; FR-515): through `BridgeProcess.Serve`.
    - `symbol_detail` with the retired `Describe` id → `IsError`, and the text contains "call rename_candidates with
      retiredSymbolId " & id.
    - `rename_candidates` with that id → not an error, one candidate.
  - (4) `ACompletedRunThatDisagreesWithItsCountIsRefusedByName`: on `CopyMap()`, `DeleteCandidate` removes one of run
    2's rows. `rename_candidates(runId run2)` → `IsError`, and the text contains "recorded 3 rename candidates but the
    map holds 2" and the run id.
  - (5) `RunAndSymbolRefusalsAreByName` (CON3), through the executable:
    - a run id past the largest → "holds no extract run with id";
    - `OtherRun1` under key `Sample` → "belongs to solution 'Other'" and "not to 'Sample'";
    - a symbol id past the largest → `SymbolNotFound`'s phrase;
    - an `Other` symbol's id under `Sample` → `SymbolOutOfScope`'s phrase.
  - (6) `TheToolIsReadOnly`:
    - the map's SHA-256 before and after (1)–(3)'s calls is equal;
    - `BridgeProcess` `tools/list` shows `rename_candidates` with `annotations.readOnlyHint` true.
  - (7) `AnActiveSymbolIsAnsweredNotRefused`: `retiredSymbolId` = the active `Label` id in `Sample.Lib`.
    - Not an error; `symbol.isActive` true; `retiredInRunId` null.
    - `candidates` is empty (it is only ever a new side); `note` is the retired-symbol sentence of data-model §2.
  - (8) `ASolutionWithNoCandidatesSaysSo`: `rename_candidates(solutionKey "Other")` → `total` 0, one run checked 0 = 0,
    and `note` the unfiltered sentence of data-model §2.
  - (9) `BothFiltersNarrowToOneRunAndNeverCheck` (spec Q5 "both"; analyze G4):
    - `runId` run 2 with `retiredSymbolId` = the retired `Describe` id → `runs` exactly `[run2]`, one candidate
      (`Describe`→`Explain`), `countChecked` false, reason "filtered by retiredSymbolId", `symbol` present;
    - `runId` run 1 with the same id → `runs` exactly `[run1]`, `total` 0, not checked, and `note` the retired-symbol
      sentence (the edge case "the run wrote nothing naming the symbol").

  Run → every fact red for its stated reason (the tool is not registered: "unknown tool" / not found; record the
  message). Header "RED: <date> (T007)".
- [X] T008 [US1] Core reads (research R76, R77; STOP 1 decisions 3–4; data-model §1).
  - `src/CodeMem.Core/Records/RenameCandidateRecord.vb` (new): `Id`, `SolutionId`, `RunId`, `RetiredSymbolId`,
    `NewSymbolId` As Long; `SamePath` As Boolean; `OffsetDistance` As Integer? ("null when same_path = 0");
    `Rank` As Integer.
  - `src/CodeMem.Core/Repositories/RenameCandidatesRepository.vb` + `Public Function ReadCandidates(db As MapDatabase,
    solutionId As Long, runId As Long?, retiredSymbolId As Long?) As List(Of RenameCandidateRecord)`. It runs `SELECT
    id, solution_id, run_id, retired_symbol_id, new_symbol_id, same_path, offset_distance, rank FROM rename_candidates
    WHERE solution_id = @solution_id AND (@run_id IS NULL OR run_id = @run_id) AND (@retired_symbol_id IS NULL OR
    retired_symbol_id = @retired_symbol_id) ORDER BY run_id DESC, retired_symbol_id, rank, new_symbol_id`, with each
    null parameter bound as `DBNull.Value`. The module summary becomes "Append-only rename candidate proposals, and
    their read".
  - `src/CodeMem.Core/Repositories/ExtractRunsRepository.vb` + `ReadBySolution(db, solutionId) As List(Of RunRecord)`
    (`SELECT <ReadColumns> FROM extract_runs WHERE solution_id = @solution_id ORDER BY id DESC`).
  - The same file + `ReadRetiringRun(db, solutionId, lastSeenRunId) As RunRecord` (`SELECT <ReadColumns> FROM
    extract_runs WHERE solution_id = @solution_id AND outcome = 'completed' AND id > @last_seen_run_id ORDER BY id
    LIMIT 1`; `Nothing` when none). Its XML doc states Q2's rule: "the first completed run of the solution after the
    symbol's last sighting is the run that retired it (Article VI step 4)".
  - Both reads reuse the existing column list and row mapping; dated header lines.

  Run `Guards` → `BridgeSqlGateTests` (3) (the three are `Read…`, SELECT-only), the tripwire and `SqlLocationGateTests`
  green.
- [X] T009 [US1] `src/CodeMem.Bridging/Reading/SymbolResolver.vb` (research R76; Article XII):
  - Add `Public Function RequireInScope(map, scope, config, symbolId As Long) As SymbolRecord`. It reads the row, then
    raises `SymbolNotFound`, then `SymbolOutOfScope`, with the same facts as today, and returns the row active or not.
  - `RequireActive` becomes `RequireInScope` followed by the `SymbolRetired` check, the same order and the same facts.
    The two refusals now live in one place.
  - Dated header line. Run `Bridge` → the existing refusal facts (B02, B03) green, unchanged.
- [X] T010 [US1] Refusal vocabulary (FR-508, FR-515; contracts/tools.md §6; STOP 1 decision 7).
  - `src/CodeMem.Bridging/Refusals/BridgeRefusalKind.vb`: add `RunNotFound`, `RunOutOfScope` and
    `CandidateCountMismatch` after `NotAType`, each with its summary.
  - `src/CodeMem.Bridging/Refusals/BridgeRefusal.vb`: the three texts of §6 verbatim, keyed on the facts `mapPath`,
    `runId`, `runKey`, `solutionKey`, `recorded`, `found`. `SymbolRetired`'s last sentence becomes "Search again for
    the current symbol, or call rename_candidates with retiredSymbolId " & id & "."
  - Dated header lines.
  - Amend `tests/CodeMem.Tests/Bridge/B09_StandaloneTests.vb` (6): run first → **red, expected 29, actual 32** (the
    named Red); record it; change the literal to 32; dated header line. Run → green.
- [X] T011 [P] [US1] Envelopes (data-model §2–§4, field names and nulls verbatim; nulls are written, never omitted —
  `BridgeJson`'s `DefaultIgnoreCondition = Never`), each file new under `src/CodeMem.Bridging/Reading/Envelopes/`:
  - `RenameCandidatesEnvelope`: `ReadAtUtc`, `Scope As ScopeEnvelope`, `Filters As RenameCandidatesFiltersEnvelope`,
    `Symbol As CandidateSideEnvelope`, `Runs As List(Of ExaminedRunEnvelope)`, `Total As Integer`,
    `Candidates As List(Of CandidateEnvelope)`, `Note As String`.
  - `RenameCandidatesFiltersEnvelope` (its own file, one type per file; analyze I3): `RunId As Long?`,
    `RetiredSymbolId As Long?`, echoing the arguments as given.
  - `CandidateEnvelope`: `RunId`; `Retired As CandidateSideEnvelope`; `[New] As CandidateSideEnvelope`, serialised
    as `new`; `SamePath`; `OffsetDistance As Integer?` ("null when samePath is false"); `Rank`.
  - `CandidateSideEnvelope`: `Id`, `DocCommentId`, `Kind`, `Name`, `Container As NamedRefEnvelope`,
    `Project As NamedRefEnvelope`, `Path`, `Line`, `IsActive`; and `LastSeenRunId As Long?`, `RetiredInRunId As Long?`,
    which are null on a candidate's sides and set only on the `symbol` block.
  - `ExaminedRunEnvelope`: `RunId`, `Outcome`, `FinishedUtc`, `SymbolsRetired`, `RenameCandidatesRecorded`,
    `CandidatesReturned`, `CountChecked As Boolean`, `CountNotCheckedReason As String` ("null when checked; otherwise
    `failed run` or `filtered by retiredSymbolId`").
- [X] T012 [US1] `src/CodeMem.Bridging/Reading/Readers/RenameCandidatesReader.vb` (new; research R76–R78; STOP 1
  decisions 3–5). `Public Function Read(map, scope As ResolvedScope, config, runId As Long?, retiredSymbolId As Long?)
  As RenameCandidatesEnvelope`, in this order:
  1. The one solution of the scope.
  2. When `runId` is given: `ExtractRunsRepository.ReadById`. `Nothing` → `RunNotFound` (`mapPath`, `runId`). A
     different `SolutionId` → `RunOutOfScope`, with the run's own key from `SolutionsRepository.ReadById`.
  3. When `retiredSymbolId` is given: `SymbolResolver.RequireInScope`.
  4. `RenameCandidatesRepository.ReadCandidates`.
  5. The runs, per spec Q5:
     - neither filter → `ReadBySolution`;
     - `runId` → that run;
     - `retiredSymbolId` only → the distinct candidate `RunId`s read with `ReadById`, plus, when the symbol row is not
       active, `ReadRetiringRun(solutionId, row.LastSeenRunId)`; no duplicates, id descending.
  6. The sides, through `CodeSymbolsRepository.ReadById`, memoised in a `Dictionary(Of Long, SymbolRecord)` per call.
     Container and project become `NamedRefEnvelope`s via `TwinFolder.NamedRef`.
  7. The count check (R78): when `retiredSymbolId` is absent, for each run with `Outcome = "completed"` in listed
     order, `candidatesReturned` ≠ `run.Counts.RenameCandidates` → `CandidateCountMismatch` (`runId`, `solutionKey`,
     `recorded`, `found`). Otherwise record `countChecked` and the reason.
  8. `note`: data-model §2's two sentences when `total` = 0, else null.

  The file carries no SQL and no gate word. XML docs name the Q2 derivation's one door (`ReadRetiringRun`).
- [X] T013 [US1] Register the tool (FR-501, FR-510; contracts/tools.md §1–§2, §4; STOP 1 decision 6 as ruled).
  - `src/CodeMem.Bridging/Mcp/BridgeToolDescriptions.vb`: add `Public Const RenameCandidates As String`, the ruled
    text of contracts/tools.md §2 verbatim, both STOP 1 edits included. Revise the header description ("The nine
    registered tool descriptions").
  - `src/CodeMem.Bridging/Mcp/BridgeTools.vb`:
    - `"rename_candidates"` in `Names` after `"map_status"` and before `"extract"`, giving the order `solutions`,
      `symbol_search`, `symbol_detail`, `references`, `orphans`, `type_usages`, `map_status`, `rename_candidates`,
      `extract` (the read tools before the one that runs a child), and in `Descriptions`;
    - `Public Function RenameCandidates(rawArguments, Optional solutionKey As String = Nothing, Optional runId As
      Long? = Nothing, Optional retiredSymbolId As Long? = Nothing) As CallToolResult`, through `RunRead`:
      `ValidateArguments(rawArguments, key)`, then `ScopeResolver.Resolve`, the seam, `RenameCandidatesReader.Read`.
  - `src/CodeMem.Bridging/Mcp/BridgeToolBindings.vb`: `RenameCandidates(context, solutionKey, runId,
    retiredSymbolId)`.
  - `src/CodeMem.Bridge/Mcp/BridgeServer.vb` `ToolDelegate`: the case `"rename_candidates"` → `New Func(Of
    RequestContext(Of CallToolRequestParams), String, Long?, Long?, CallToolResult)(AddressOf
    bindings.RenameCandidates)`. `ReadOnly = (name <> "extract")` already annotates it.
  - Dated header lines.
  - Amend `tests/CodeMem.Tests/Bridge/B09_StandaloneTests.vb` (4): run first → **red, expected 8, actual 9**;
    record; change the literal to 9 (the new description passes the forbidden-phrase scan unchanged); dated header
    line.
- [X] T014 [US1] `dotnet build` 0/0; run B12 → (1)–(9) green; run `Bridge` and `Guards` → green. Record Green in B12's
  header. Then the FIREs (research R79; plan §Test design; the Review Gate "every new guard carries its fire
  demonstration"; analyze C1, G1), each reverted from a byte copy:
  - (1): in the reader, fold candidates whose sides share `name`, `path` and `line` → two candidates, not three → red;
  - (2): under `retiredSymbolId` list every run of the solution → three runs, not `[run2]` → red;
  - (4): skip the count check → answered, not refused → red;
  - (5) `RunOutOfScope`: drop the reader's solution comparison → `OtherRun1` under `Sample` is answered, not refused
    → red;
  - (5) `RunNotFound`: answer an unknown `runId` with an empty `runs` list instead of refusing → answered, not
    refused → red;
  - (6): register `rename_candidates` with `ReadOnly = False` → `readOnlyHint` false → red;
  - FR-509: add `Dim probe As SqliteConnection = New SqliteConnection("Data Source=x")` to
    `src/CodeMem.Bridging/Reading/Readers/RenameCandidatesReader.vb` → `BridgeStandaloneGateTests` (1) red naming
    `RenameCandidatesReader.vb`, and the connection-site gate (`SqlLocationGateTests`) red naming a second site.

  Record the seven FIRE lines: B12's in B12's header, and FR-509's in B12's header naming the two gates.
- [X] T015 [US1] Add three facts to `tests/CodeMem.Tests/Bridge/B08_LiveMapTests.vb`, armed by `CODEMEM_LIVE_MAP` like
  the other eight (research R84; STOP 1 decision 9; analyze G3). No timing is asserted; each fact writes its call's
  measured duration to the test output, for T031 to record.
  - (9) `LiveRenameCandidatesRunThirteenHasOne`: `rename_candidates(solutionKey "MemOS", runId 13)` → one candidate:
    `retired.id` 5339, `new.id` 23219, `samePath` true, `offsetDistance` 284, rank 1; run 13 `countChecked` true.
  - (10) `LiveRetiredConstructorListsOnlyItsRetiringRun`: `retiredSymbolId 6754` → `total` 0; `runs` exactly `[53]`;
    `symbol.lastSeenRunId` 52; `symbol.retiredInRunId` 53.
  - (11) `LiveUnfilteredMemOsReconcilesEveryCompletedRun`: `rename_candidates(solutionKey "MemOS")` → answered, not
    refused. Every run with `outcome` "completed" has `countChecked` true. `total` equals the sum of
    `renameCandidatesRecorded` over the completed runs (1 on 2026-09-29, run 13's, checked live through the MemOS-side
    reader).

  Run with the variable unset → all three skipped (the acceptance runs at T031). Dated header line.

**Checkpoint**: `rename_candidates` answers, refuses by name and writes nothing. `SymbolRetired` hands its id over.
The count check fires. Thirty-two kinds, nine descriptions.

---

## Phase 4: User Story 2 — A connected assistant is told how to use the map (Priority: P1) 🎯 MVP (with Phase 3)

**Goal**: `initialize` advertises 191490 §1 verbatim, LF only; every registered tool is named in it; `symbol_search`
carries the constructor sentence; the bridge is 0.3.0.

**Independent Test**: `UsageTextsGateTests` (1)–(3) and B01 (5) through the executable; B09 (4)'s caveat table.

- [X] T016 [US2] Create `tests/CodeMem.Tests/Guards/UsageTextsGateTests.vb` (FR-511–FR-513, the v1.5.0 gate;
  contracts/usage-texts.md §5) with three facts. A helper `Collapse(text)` replaces every run of `\s+` with one space
  (spec Q4). A helper `HasWord(text, name)` matches `(?<![A-Za-z0-9_])` & `Regex.Escape(name)` &
  `(?![A-Za-z0-9_])` (research R79).
  - (1) `TheInstructionsAreAdvertisedExactlyWithoutCarriageReturns`: `BridgeProcess.Serve` over a fixture map's
    config. `InitializeResult().GetProperty("instructions").GetString()` equals `BridgeServerInstructions.Text` and
    contains no `vbCr`.
  - (2) `EveryRegisteredToolIsNamedInTheInstructions`: for each of `BridgeTools.RegisteredToolNames`,
    `HasWord(BridgeServerInstructions.Text, name)`. Assert the list's length is 9 first, so the scan cannot pass
    vacuously.
  - (3) `TheInstructionsCarryTheKitsPhrases`: `Collapse(Text)` contains "VB constructors are all named New", "Prove,
    don't grep", "Use text search for what the map does not hold" and "runtime behaviour".

  Amend `tests/CodeMem.Tests/Bridge/B01_ReadOnlyContractTests.vb` (5):
  - expected names become the literal `{"solutions", "symbol_search", "symbol_detail", "references", "orphans",
    "type_usages", "map_status", "rename_candidates", "extract"}` (sorted set comparison, as today), not
    `RegisteredToolNames`;
  - version `"0.3.0"`;
  - `InitializeResult()`'s `instructions` equals `BridgeServerInstructions.Text`.

  Run → UsageTexts (1)–(3) red (compile: no `BridgeServerInstructions`); record as the Red. After a stub
  `Public Const Text As String = ""` lands: (1) red (no `instructions` property, or empty); (2) red; (3) red. B01 (5)
  red: version 0.2.0 and no instructions. Record every Red line.
- [X] T017 [US2] The instructions (FR-511; research R75; STOP 1 decision 2):
  - `src/CodeMem.Bridging/Mcp/BridgeServerInstructions.vb` (new module): `Public Const Text As String` = a multi-line
    VB string literal whose value is 191490 §1's block. Take it from the store by the Q3 rule (25 lines), doubling
    only the two `"` quotations for VB. XML doc: "The server's instructions, 191490 §1 verbatim; LF only (the file is
    pinned eol=lf); read by RunAsync and the usage-texts facts."
  - `.gitattributes`: add `src/CodeMem.Bridging/Mcp/BridgeServerInstructions.vb text eol=lf` beside
    `SchemaRepository.vb`'s line, with a comment naming 006 R75. Then `git add --renormalize .gitattributes
    src/CodeMem.Bridging/Mcp/BridgeServerInstructions.vb`.
  - `src/CodeMem.Bridge/Mcp/BridgeServer.vb` `RunAsync`: `options.ServerInstructions = BridgeServerInstructions.Text`
    directly after the `ServerInfo` line; dated header line.
  - Run quickstart §Verbatim's `instructions` line → `OK` (`d8a173d3…`); a `DIFF` stops the work.
- [X] T018 [US2] The `symbol_search` sentence (FR-514; contracts/tools.md §2):
  - First add "VB constructors are named New; to find a class's constructions, use type_usages on the class." to the
    `SymbolSearch` row of the caveat table in `tests/CodeMem.Tests/Bridge/B09_StandaloneTests.vb` (4). Run → **red**
    (the phrase is absent); record.
  - Then insert the sentence in `src/CodeMem.Bridging/Mcp/BridgeToolDescriptions.vb` `SymbolSearch`, directly after
    "…event, project." as contracts/tools.md §2 shows. Every other sentence is unchanged (B07's "compiled into N
    projects" included).
  - Dated header lines.
- [X] T019 [P] [US2] `<Version>0.3.0</Version>` in `src/CodeMem.Bridging/CodeMem.Bridging.vbproj` and
  `src/CodeMem.Bridge/CodeMem.Bridge.vbproj` (FR-525; STOP 1 decision 10). `ProjectFileGateTests` green.
- [X] T020 [US2] `dotnet build` 0/0; run `UsageTextsGateTests` (1)–(3), B01 (5), B09 (4) and B07 → green; record Green
  lines. FIREs, each reverted:
  - UsageTexts (1): concatenate `& vbCrLf` into the constant → red;
  - B01 (5) and UsageTexts (1): remove the `ServerInstructions` line from `RunAsync` → red (the spec's FIRE);
  - UsageTexts (2): delete the line "- Was it renamed; what became of a retired id → rename_candidates (proposals,
    never applied)." from the constant → red;
  - UsageTexts (3): replace "Prove, don't grep" with "Prove it" → red.

  Record the FIRE lines.

**Checkpoint (MVP)**: a Claude Code session connected to the Debug bridge receives the instructions at `initialize`,
and `rename_candidates` is listed among nine tools. That is 191497 #1 and #7 delivered on the surface every session
loads.

---

## Phase 5: User Story 3 — Someone who downloads CodeMem gets the same kit (Priority: P2)

**Goal**: The skill and the public snippet under `docs/claude-code/`, verbatim; the README at nine tools with the row,
step 5's sentence, step 6 and v1.5.0; the fragment and the process document free of machine paths; nothing private in
the repository.

**Independent Test**: `UsageTextsGateTests` (4)–(7), B05 (18), quickstart §Verbatim (six `OK`).

- [X] T021 [US3] Add four facts to `tests/CodeMem.Tests/Guards/UsageTextsGateTests.vb` (FR-518–FR-521; contracts
  usage-texts §5; ruled FR-521 B):
  - (4) `TheReadmeToolTableIsExactlyTheRegisteredTools`: read `README.md` from the line `## The tools your assistant
    gets` to the first blank line after the table's last `|` row. Take each row's tool as the text between ``**` `` and
    `` `**`` in its first cell (skip the header and separator rows). Assert set equality with `RegisteredToolNames`,
    reporting the missing and the extra.
  - (5) `TheSkillAndTheSnippetSayTheMapProvesStructure`: `Collapse` of `docs/claude-code/skills/codemem/SKILL.md` and
    of `docs/claude-code/CLAUDE.snippet.md` each contains "The map proves structure; runtime behaviour needs its own
    proof."
  - (6) `NothingPrivateIsUnderDocs`: every file under `docs/`, at least one, contains neither "Rick" nor "153204"
    (ordinal), naming the file.
  - (7) `TheReadmeSaysNineTools`: `README.md` contains `MCP-9%20tools`, `9 MCP tools`, `Nine, over stdio.` and
    `the bridge and its nine tools`, and contains none of `MCP-8%20tools`, `8 MCP tools`, `Eight, over stdio` and
    `its eight tools`.

  Amend `tests/CodeMem.Tests/Bridge/B05_ExtractGateTests.vb` (18) (FR-522): the hook command equals
  `"""C:/path/to/CodeMem.Bridge.exe"" hook"` and does not contain `C:/Users/`.

  Run → (4) red (no `rename_candidates` row), (5) red (no files), (6) green first (a guard on absence; trusted through
  T026's FIRE), (7) red ("8 tools"), B05 (18) red (the `rchau` path). Record every Red line.
- [X] T022 [P] [US3] The kit files, from the store (FR-516, FR-517; research R82):
  - `docs/claude-code/skills/codemem/SKILL.md` = 191490 §3's block + one final LF (70 lines);
  - `docs/claude-code/CLAUDE.snippet.md` = 191490 §2's **first** block + one final LF (13 lines).

  Both are taken by the Q3 rule from `document_sections`, read-only. Neither the second block of §2 nor its heading is
  written anywhere. Run quickstart §Verbatim's `skill` and `snippet` lines → `OK`.
- [X] T023 [US3] `README.md`, exactly contracts/usage-texts.md §1, every row but the suite counts and the Status line
  (T030):
  - badge `MCP-9%20tools`; the sketch's `9 MCP tools`; "Nine, over stdio.";
  - the `rename_candidates` row after `orphans`;
  - step 5's last line, 191490 §4's quoted sentence, from the store;
  - step 6, 191490 §4's block from the store, after step 5 with one blank line before and after;
  - `v1.5.0, fifteen articles`; "the bridge and its nine tools".

  No pointer line. No other line changes (`git diff README.md` shows only these hunks). Run quickstart §Verbatim's
  `step 6`, `step 5 line` and `table row` → `OK`.
- [X] T024 [P] [US3] `src/CodeMem.Bridge/hooks/settings.fragment.json` line 9 → `"command": "\"C:/path/to/CodeMem.Bridge.exe\"
  hook"` (contracts/usage-texts.md §3). Nothing else in the file changes.
- [X] T025 [P] [US3] `src/CodeMem.Bridge/README.md`, exactly contracts/usage-texts.md §4 (FR-523; Q9; STOP 1
  decision 11 as ruled):
  - nine tools named in §"What the bridge is"; its sentence "Every tool is scoped by `solutionKey`, the key of a
    `solutions` row in the map; `solutions` lists them." deleted;
  - install step 4's two machine paths → `C:/path/to/CodeMem.Bridge.exe`;
  - the `rename_candidates` entry under §"When each tool is called";
  - "Thirty-two kinds", `SymbolRetired`'s remedy, and the three rows after `NotAType`;
  - the §"What the bridge never does" sentence amended.

  `git grep -n "C:/Users/" -- src/CodeMem.Bridge/README.md` prints nothing.
- [X] T026 [US3] `dotnet build` 0/0; run `UsageTextsGateTests` (1)–(7) and B05 → green; record Green lines. FIREs, each
  reverted:
  - (4): delete the `rename_candidates` row → red naming it missing; add the row ``| **`ghost`** | x |`` → red naming
    it extra;
  - (5): delete "The map proves structure; runtime behaviour needs its own proof." from
    `docs/claude-code/skills/codemem/SKILL.md` → red naming the skill (its first Red was "file missing", which does
    not prove the phrase check; analyze C1);
  - (6): append §2's private block (from the store) to `docs/claude-code/CLAUDE.snippet.md` → red naming the file;
  - (7): restore `8 MCP tools` in the sketch → red.

  Record the FIRE lines. Run the whole quickstart §Verbatim → **six `OK`**, and record them.

**Checkpoint**: every text the v1.5.0 gate names agrees with the nine tools; the six verbatim texts hash to the
ratified values; nothing private and no machine path ships.

---

## Phase 6: Polish, the acceptance, the close

**Purpose**: Both builds green, the live figures proven on a copy, the README's counts current, the record written.

- [ ] T027 Full suite on the Debug build: `dotnet test CodeMem.sln -c Debug --nologo --logger
  "trx;LogFileName=close-006-debug.trx"` → green, under 5 minutes. Record passed / failed / skipped, the duration and
  the delta against T003's baseline: + B12's 9, + UsageTexts' 7, + B08's 3 skipped, − BridgeStandaloneGateTests (4).
- [ ] T028 Full suite on the Release build. Stop any Release `CodeMem.Bridge.exe serve` first (the F10 caveat), then
  `dotnet build CodeMem.sln -c Release --nologo -v q` 0/0 and `dotnet test CodeMem.sln -c Release --nologo` → green,
  under 5 minutes. Record it.
- [ ] T029 Every new guard carries its FIRE: `git grep -n "' FIRE:" -- tests/CodeMem.Tests/Bridge/B12_RenameCandidatesTests.vb
  tests/CodeMem.Tests/Guards/UsageTextsGateTests.vb` shows the FIREs of T014, T020 and T026. `git diff --stat
  806b524 -- src/CodeMem.Extraction src/CodeMem.Extractor src/CodeMem.Core/Schema/SchemaRepository.vb` prints nothing
  (FR-526; SC-508). The extractor still reports version 0.3.0 and the fixture's I02 fact is green. Record.
- [ ] T030 README counts at the close (contracts/usage-texts.md §1; Q8 (d)): the badge's `tests-<n>%20passing`, the
  Layout row's "<n> passing, 9 skipped" and the Status line's figures set to T027's passed and skipped counts. The
  Status line's "Features 001–005 merged" stays until the merge (T033). Re-run `UsageTextsGateTests` (4) and (7) →
  green.
- [ ] T031 Acceptance on a copy of the live map (quickstart §"Acceptance on a copy"; FR-527; STOP 1 decision 9):
  1. With no build running in a mapped repository, record the live map's SHA-256.
  2. `Copy-Item` it to `$env:TEMP\codemem-006-acceptance.sqlite`.
  3. In one command, run B08 with `CODEMEM_LIVE_MAP` set to the copy, the live hash read before and after.
  4. Record B08 (9)'s, (10)'s and (11)'s figures and each one's measured duration (no timing is asserted; analyze
     G3), every other B08 fact's result, and the line "live map unchanged by the suite: True".
  5. Delete the copy.

  A moved figure in an older B08 fact is diagnosed as 005 T042 did, never re-pinned blind.
- [ ] T032 Record the untouched: `git -C C:\Users\rchau\source\repos\rchaudio-a11y\MemOS status --porcelain`, the same
  as T001's; `git grep -n "C:/Users/" -- README.md src/CodeMem.Bridge/README.md src/CodeMem.Bridge/hooks/settings.fragment.json`
  prints nothing; `git grep -n "153204" -- docs` prints nothing. Record under "What must not happen" in the quickstart.
- [ ] T033 Close-out:
  - Append "Implementation record (<date>)" to `specs/006-map-teaches-its-use/plan.md`: every Red, Green and FIRE by
    task; T003's baseline and any intermittent failure; the acceptance figures; deviations.
  - Update the spec's Status line. `checklists/requirements.md` is left alone (its markers are the reviewer's).
  - State in the implementation record, in one line, that no task of 006 writes to `~/.claude` (FR-528; analyze G6).
  - Revise the 006 memory note.
  - `git status` clean on the feature branch.
  - The merge to `main` is at the Architect's direction only. At the merge, the README Status line's "Features
    001–005 merged" becomes "001–006", in the merge-preparation commit.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: T001 → T002 → T003. T003's baseline must be green, or raised, before Phase 2.
- **Foundational (Phase 2)**: after Phase 1. T004 ∥ T005; T006 after T005 (it calls `TwinScenario.Prepare`). **Blocks
  US1 and US2** (B12 needs `RenameScenario`; UsageTexts (1) and B01 (5) need `InitializeResult`).
- **US1 (Phase 3)**: after Phase 2.
  - **T007 (the B12 Red) before T008–T013.**
  - T008 → T009 → T010 → T011 → T012 → T013 → T014 → T015.
  - T011 is [P] with T008–T010 (new files only); T012 needs T008–T011; T013 needs T012.
- **US2 (Phase 4)**: after US1. The instructions name `rename_candidates`, and UsageTexts (2) asserts nine registered
  names.
  - **T016 (the Red battery) before T017–T018.**
  - T017 → T018; T019 ∥ T017; T020 last.
  - Phases 3 + 4 are the MVP.
- **US3 (Phase 5)**: after US2, because UsageTexts (4) compares the README with the nine registered names.
  - **T021 (the Red battery) before T022–T025.**
  - T022 ∥ T024 ∥ T025 (three files); T023 after T021 (README only).
  - T026 last.
- **Polish (Phase 6)**: after Phase 5. T027 → T028 → T029 → T030 → T031 → T032 → T033, each recording before the
  next.

### Within Each User Story

- The Red battery, written, run and recorded → the slices in the order given → Green recorded → FIREs recorded → the
  production-route check through the executable → the verbatim check where a text landed → the suite.

### Parallel Opportunities

- Phase 2: T004 ∥ T005.
- Phase 3: T011 (the envelopes) beside T008–T010.
- Phase 4: T019 beside T017.
- Phase 5: T022 ∥ T024 ∥ T025 after T021.

---

## Parallel Example: Phase 5 after T021

```text
Task: "docs/claude-code/skills/codemem/SKILL.md and CLAUDE.snippet.md from the store; verbatim OK"   (T022)
Task: "hooks/settings.fragment.json: the placeholder path"                                          (T024)
Task: "src/CodeMem.Bridge/README.md: contracts/usage-texts.md §4"                                   (T025)
```

## Parallel Example: Phase 3 beside T008

```text
Task: "Core reads: RenameCandidateRecord, ReadCandidates, ReadBySolution, ReadRetiringRun"   (T008)
Task: "The five envelopes of data-model §2–§4"                                                (T011)
```

---

## Implementation Strategy

### MVP First (User Stories 1 and 2, both P1)

1. Phase 1: the branch, the version pin retired, the baseline named.
2. Phase 2: `InitializeResult`, the helpers, `RenameScenario`.
3. Phase 3 (`rename_candidates` with its refusals and the count check; `SymbolRetired` hands over its id), then
   Phase 4 (the instructions at `initialize`, the `symbol_search` sentence, 0.3.0).
4. **STOP and VALIDATE**:
   - B12, `UsageTextsGateTests` (1)–(3), B01 (5), B09 green.
   - A Claude Code session connected to the Debug bridge shows the instructions and nine tools.
   - `rename_candidates` answers `SymbolRetired`'s id.

   That is 191497 #1, #3 and #7 on the surface every connected session loads.

### Incremental Delivery

1. US3: the skill, the snippet, the README and the process document, which is what someone who downloads CodeMem
   reads.
2. Phase 6: both builds, the acceptance on a copy, the counts, the record; the merge at the Architect's word.

---

## Notes

- [P] tasks = different files, no dependencies. A task that edits a file another task also edits is never [P].
- Verify every Red for its stated reason before implementing; a Red for another reason is a finding, not a Red.
- Commit after each phase at least. The retirement (T002) and the instructions with their `.gitattributes` line
  (T017) each go on their own commit, so the history reads cleanly.
- The plan-time scratch extraction of 191490 (`scratchpad/191490/*.txt`, `hash191490.py`) is evidence, not source.
  Every text is re-taken from the store at its task and hashed back.
- Every task that names a STOP 1 decision is revised if a later ruling changes it; the decisions are numbered in
  plan.md §STOP 1.

# Implementation Plan: CodeMem 006 — The Map Teaches Its Own Use: Server Instructions, the Usage Kit, and `rename_candidates`

**Branch**: `006-map-teaches-its-use` (from `main` at `806b524`; not yet created, since no hook creates it) | **Date**: 2026-09-29 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/006-map-teaches-its-use/spec.md`, clarified 2026-09-29 with five rulings:
- Q1: twins are bare ids plus a project on each side.
- Q8 (b): "Nine, over stdio."
- Q8 (c) and (f): 191490 revised, the step 5 sentence added, no pointer line, and six ratified hashes taken from the
  store.
- FR-521: the README table is checked in both directions.
- Q8 (e): "v1.5.0".

Q2–Q7 and Q9–Q13 stood as proposed; STOP 1 is ruled below. **Research**: [research.md](research.md), R74–R84.

**Analyze**: 2026-09-29. Fourteen findings, ruled by the Architect and applied here, in the spec, the tasks and the
quickstart. The finding IDs are cited where a passage changed.
- **C1, G1**: four FIREs added.
- **G2**: FR-513 is "the fact, with one FIRE".
- **G3**: B08 (11), no timing assert, "under 3 s" dropped.
- **G4**: B12 (9).
- **U1, U2**: the helpers and run 3's assertion.
- **G5**: accepted.
- **G6**: a line at the close.
- **I1–I5**: wording.

**Governing document**: `.specify/memory/constitution.md` **v1.5.0** (`806b524`). This is the first plan checked
against its Map-first rule and its usage-texts gate. **Found at plan: `main` is red since `806b524`.**
`BridgeStandaloneGateTests` (4) pins the version line `**Version**: 1.4.0` (005 FR-431), which the v1.5.0 amendment
moved. The amendment's migration path said no existing code was put out of compliance, and this test was. The fix is
this feature's first task. As ruled at STOP 1 (decision 1), the fact is **retired to `_Archive/006-map-teaches-its-use/`**,
not moved to 1.5.0: fact (3) still pins Article IX's text, and (4) would break `main` on every amendment. Its Red,
already observed, is recorded.

**Ceremony**: STANDARD, no spike. **One stop, after this plan, before tasks** (§STOP 1): eleven decisions.
**Ruled 2026-09-29**: eight as proposed; 1 amended (retire, don't move); 6 approved with two edits; 11 overruled on
both exclusions.

## Summary

Give the bridge a ninth tool, `rename_candidates`, that reads the map's rename evidence for one solution. Each
candidate carries its bare ids and each side's project, never folded. Runs are listed newest first with the count
check stated per run. Under `retiredSymbolId`, only the runs that wrote a candidate for that id plus the derived run
that retired it are listed. Three new refusals are added, and the existing symbol refusals are reused through one door.
`SymbolRetired`'s text names the tool.

Advertise 191490 §1 as the server's instructions at `initialize`: one LF-pinned constant, set through the SDK's
`McpServerOptions.ServerInstructions` (ModelContextProtocol.Core 1.4.0, verified). Add the constructor sentence to
`symbol_search`. Ship the skill and the public snippet under `docs/claude-code/`, verbatim. Edit the README (nine
tools, the table row, step 5's sentence, step 6, v1.5.0) and the process document. Put the placeholder path in the
hook fragment.

Pin the v1.5.0 gate as facts: every registered tool in the instructions, the README table equal to the registered
tools, the kit's phrases, nothing private under `docs/`. Verify the six texts against the spec's hashes in the
quickstart. The extractor and the schema do not change; Core gains three SELECT-only reads and one record.

## Technical Context

**Language/Version**: VB.NET on .NET 8 (`net8.0`), Option Strict/Explicit On, Infer Off, `GenerateDocumentationFile`
on. No project changes its settings. `CodeMem.Bridge` and `CodeMem.Bridging` `<Version>` move 0.2.0 → **0.3.0**. The
extractor stays at 0.3.0.

**Primary Dependencies**: unchanged. `ModelContextProtocol.Core` 1.4.0 already carries
`McpServerOptions.ServerInstructions` (R74); `Microsoft.Data.Sqlite` 8.0.31; `LibGit2Sharp` 0.32.0; the Roslyn and
MSBuild pins of 002/005. No package moves. No reference to MemOS.

**Storage**: the map, read-only from the bridge, **schema version 3 unchanged**. No table, column or index is added.
Reads added: `rename_candidates` by solution with two optional filters; `extract_runs` by solution; the retiring run.

**Testing**: xUnit, real SQLite, the real compiled fixture and its linked-file variant, the real executable over
stdio.
- New: `Bridge/B12_RenameCandidatesTests`, `Guards/UsageTextsGateTests`, `Support/RenameScenario`.
- Amended: B01 (5), B05 (18), B08 (+ (9), (10), (11)), B09 (4) and (6), `Support/BridgeProcess` (+ `InitializeResult`,
  `ListToolAnnotations`),
  `Support/TwinScenario` (`Prepare` → `Friend Shared`), `Support/MapQueries` (+ `DeleteCandidate`).
- Retired: `BridgeStandaloneGateTests` (4), to `_Archive/006-map-teaches-its-use/tests/` (STOP 1 decision 1).
- Live facts are Skip-armed by `CODEMEM_LIVE_MAP`, pointed at a copy (R84).

**Baseline (re-measured at plan, FR-529)**: `main` at `806b524`, Debug, two runs:
- With build: **195 passed / 2 failed / 9 skipped (206), 4 m 51 s**.
- `--no-build`: **196 passed / 1 failed / 9 skipped, 4 m 45 s**.

The one reproducible failure is `BridgeStandaloneGateTests.TheConstitutionIsVersionOneFour`: expected start
`**Version**: 1.4.0`, the line `806b524` moved. The first run's second failure **did not reproduce, and its name was
not captured**: that run's output was cut to its last lines. It is therefore an unnamed intermittent failure on
`main`, not a known one. The README's "197 passing" is the 005 close's figure. Both durations are near SC-509's
5-minute bound; the 005 close recorded 3 m 01 s on Release.

**Target Platform**: Windows 11 developer workstation; .NET SDK 10.0.401 hosting `net8.0`; Claude Code as the MCP
client.

**Project Type**: CLI/stdio server over class libraries. The six existing projects: **no new project**, none removed.

**Performance Goals**: no timing is asserted for `rename_candidates` (analyze G3, ruled). B08 (9)–(11) each record
their call's measured duration on the copy at T031, including MemOS unfiltered (97 runs, 1 candidate). The
instructions add ~1.9 KB to one `initialize` response per session.

**Constraints**:
- Article IX (one database, one connection site).
- Article XI: SQL only in Core's named `Read…` methods; no literal with a SQL word in either bridge project.
- Article XII, one door each:
  - the symbol refusals (`SymbolResolver.RequireInScope`, which `RequireActive` now calls);
  - the retiring run (`ExtractRunsRepository.ReadRetiringRun`);
  - the count check (the reader);
  - the instructions (`BridgeServerInstructions.Text`);
  - refusal wording (`BridgeRefusal`).
- Verbatim texts (spec Q3).
- LF instructions on a CRLF checkout (R75).
- The extractor and the schema untouched (Q11).

**Scale/Scope**: nine tools; thirty-two refusal kinds; five mapped solutions (MemOS 97 runs, one candidate). About
20 production and test files touched or added, two docs files added, the README and the process document edited.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Article / Gate | Status | How this plan satisfies it |
|----------------|--------|----------------------------|
| I. Library-First | PASS | No new project. `CodeMem.Bridge` stays wiring: one assignment in `RunAsync`, one delegate case. Every rule is in `CodeMem.Bridging` or `CodeMem.Core`. One module or class per file: `BridgeServerInstructions`, `RenameCandidatesReader`, `RenameCandidateRecord`, five envelopes, `RenameScenario`, `B12_…`, `UsageTextsGateTests`. |
| II. Test-First | PASS | Every behaviour is a fact observed Red first (§Test design). Every new guard carries its FIRE. The moved pins (B01 (5), B05 (18), B09 (4), (6)) are named Reds; `BridgeStandaloneGateTests` (4) is red already, and its retirement records that Red. |
| III. Integration-First | PASS | Real extractions of the real fixture: the I06 rename, the linked-file twin rename, a real failed run via `RunSeams`. The real executable over stdio for `initialize`, `tools/list` and every refusal. A real copy of the live map for acceptance. No mock. |
| IV. Compiler Fact Only | PASS | The tool presents rows the extractor wrote. The retiring run is a query over published runs, not an inference: Article VI step 4 makes the first completed run after the last sighting the retiring one. No ranking or folding of the tool's own. |
| V. Green Only, Stamped | PASS / unchanged | No write path changes. A failed run is shown as failed and never checked. |
| VI. Reconcile, Never Truncate | PASS / unchanged | The extractor is untouched. Candidates are read, never applied (the Review Gate): the tool is read-only-annotated, opens the map read-only, and B12 (6) asserts the map byte-identical. |
| VII. Evidence on Every Row | PASS | Each side carries its path and line; each candidate its run. |
| VIII. Counts That Reconcile | PASS (extended on the read side) | The count check refuses a completed run whose rows disagree with its recorded `rename_candidates` (R78). |
| IX. One File, Many Solutions, One Writer | PASS | The reader opens the map through `MapAccess`/`MapDatabase`. Zero `SqliteConnection` in the bridge projects (005's standalone gate) and exactly one site under `src/` (the connection-site gate). FIRE: a `New SqliteConnection` in `RenameCandidatesReader.vb`. Neither texts nor code name another database. |
| X. Absence Must Be Representable | PASS | `offsetDistance`, `container`, `project`, `symbol`, `retiredInRunId`, `countNotCheckedReason` and `note` are JSON `null` when absent (the envelopes write nulls). `total` 0 and `candidates: []` are the empty answer, never a missing field. |
| XI. Anti-Abstraction | PASS | Microsoft.Data.Sqlite direct. Three `Read…` methods in two existing repositories. `RenameCandidateRecord` is the stored row's shape (R76 says why `RenameCandidate` cannot serve). `TwinScenario.Prepare` gains a second caller, not an abstraction. `RequireInScope` is an Article XII split, not a Rule-of-Three abstraction. |
| XII. One Door for Every Rule | PASS | Listed under Constraints. `RequireActive` calls `RequireInScope` rather than restating its two refusals. |
| XIII. Production-Route Reachability | PASS | Every new refusal and the tool's answers are driven through `CodeMem.Bridge.exe serve` over stdio. `initialize`'s instructions come from the real server. The `SymbolRetired` → `rename_candidates` hand-off is exercised end to end (B12 (3)). |
| XIV. Archive, Never Delete | PASS | One test fact retires: `BridgeStandaloneGateTests` (4) moves to `_Archive/006-map-teaches-its-use/tests/` with a README (STOP 1 decision 1). Its history stays in the live file's header. Nothing is deleted. |
| XV. Compiled .NET, No Foreign Runtime | PASS | The quickstart's verbatim check is PowerShell (a dev-time artefact). The plan-time hash extraction was scratch Python, not shipped (R82). |
| **Map first (v1.5.0 hard rule)** | PASS | The map was refreshed before planning (run 111). Every structure fact in research came from it or from source read for behaviour or text. §Test design's "nothing else pins X" claims name their search: text search over test sources, which is right for strings and literals. |
| **Usage texts agree with tool behaviour (v1.5.0 gate)** | PASS (by design; proven at the close) | This feature adds a tool, so it updates the instructions, every description (the new one, and `symbol_search`'s), `docs/claude-code/`, the README's tool list and usage section, and the process document. `UsageTextsGateTests` fires on a registered tool missing from the instructions or the README table, and on an extra README row. |
| Gate: Option settings | PASS | No project file changes its settings; only the two `<Version>` lines move. |
| Gate: no SQL outside a named repository method | PASS | The three reads live in `ExtractRunsRepository` and `RenameCandidatesRepository`. The new texts avoid every SQL word (R80, R81). |
| Gate: `New SqliteConnection` in exactly one file | PASS | Unchanged: `MapDatabase.vb`. |
| Gate: header block + XML docs | PASS | Every new and touched `.vb` file. The docs markdown is not source. |
| Gate: tripwire; residuals block; candidates never applied | PASS / unchanged | No write added anywhere. Core's new methods are SELECTs, which `BridgeSqlGateTests` (3) checks by prefix. |
| Gate: three call sites for a new abstraction | PASS | None introduced. |

**Post-design re-check**: unchanged after Phase 1. No row depends on a STOP 1 choice except the tool description
(decision 6), which is the one shipped text with no ratified source.

## Project Structure

### Documentation (this feature)

```text
specs/006-map-teaches-its-use/
├── spec.md                    # clarified 2026-09-29 (five rulings; the six ratified hashes)
├── plan.md                    # this file
├── research.md                # R74–R84
├── data-model.md              # what is read; the result; side; examined run; kinds; the shipped texts
├── contracts/
│   ├── tools.md               # parameters, the two descriptions, the result, refusal order, instructions wiring, the four texts
│   └── usage-texts.md         # README, docs/claude-code/, the fragment, the process document, what the gate's facts read
├── quickstart.md              # build and test, fixture validation, the verbatim hashes, acceptance on a copy
├── checklists/requirements.md
└── tasks.md                   # /speckit-tasks output, after STOP 1
```

### Source Code (repository root): files that appear or change

```text
.gitattributes                                        # + src/CodeMem.Bridging/Mcp/BridgeServerInstructions.vb text eol=lf (R75)
README.md                                             # contracts/usage-texts.md §1
docs/claude-code/skills/codemem/SKILL.md              # NEW: 191490 §3 verbatim
docs/claude-code/CLAUDE.snippet.md                    # NEW: 191490 §2's public block verbatim
src/CodeMem.Core/
├── Records/RenameCandidateRecord.vb                  # NEW: the stored row (R76)
├── Repositories/RenameCandidatesRepository.vb        # + ReadCandidates(db, solutionId, runId?, retiredSymbolId?)
└── Repositories/ExtractRunsRepository.vb             # + ReadBySolution(db, solutionId), ReadRetiringRun(db, solutionId, lastSeenRunId)
src/CodeMem.Bridging/
├── Mcp/BridgeServerInstructions.vb                   # NEW: Public Const Text (191490 §1; LF-pinned)
├── Mcp/BridgeToolDescriptions.vb                     # SymbolSearch + one sentence; + RenameCandidates (contracts/tools.md §2)
├── Mcp/BridgeTools.vb                                # + "rename_candidates" in Names/Descriptions; + RenameCandidates(rawArguments, solutionKey, runId, retiredSymbolId)
├── Mcp/BridgeToolBindings.vb                         # + RenameCandidates(context, solutionKey, runId, retiredSymbolId)
├── Reading/SymbolResolver.vb                         # + RequireInScope; RequireActive = RequireInScope + the active check
├── Reading/Readers/RenameCandidatesReader.vb         # NEW (R76–R78)
├── Reading/Envelopes/RenameCandidatesEnvelope.vb     # NEW (data-model §2)
├── Reading/Envelopes/RenameCandidatesFiltersEnvelope.vb   # NEW: runId, retiredSymbolId as given (analyze I3)
├── Reading/Envelopes/CandidateEnvelope.vb            # NEW
├── Reading/Envelopes/CandidateSideEnvelope.vb        # NEW (data-model §3; also the `symbol` block, + lastSeenRunId, retiredInRunId)
├── Reading/Envelopes/ExaminedRunEnvelope.vb          # NEW (data-model §4)
├── Refusals/BridgeRefusalKind.vb                     # + RunNotFound, RunOutOfScope, CandidateCountMismatch (after NotAType): 32
├── Refusals/BridgeRefusal.vb                         # SymbolRetired revised; three texts (contracts/tools.md §6)
└── CodeMem.Bridging.vbproj                           # Version 0.3.0
src/CodeMem.Bridge/
├── Mcp/BridgeServer.vb                               # options.ServerInstructions = BridgeServerInstructions.Text; + the rename_candidates delegate
├── hooks/settings.fragment.json                      # the placeholder path
├── README.md                                         # contracts/usage-texts.md §4
└── CodeMem.Bridge.vbproj                             # Version 0.3.0
tests/CodeMem.Tests/
├── Bridge/B12_RenameCandidatesTests.vb               # NEW
├── Guards/UsageTextsGateTests.vb                     # NEW
├── Support/RenameScenario.vb                         # NEW (R79)
├── Support/TwinScenario.vb                           # Prepare: Private Shared -> Friend Shared
├── Support/BridgeProcess.vb                          # + InitializeResult()
├── Support/MapQueries.vb                             # + DeleteCandidate(db, candidateId) (the count-mismatch fixture, R78)
├── Bridge/B01, B05, B08, B09                         # amended in place (§Test design)
└── Guards/BridgeStandaloneGateTests.vb               # (4) retired (the Red already on main); header and summary at three facts
_Archive/006-map-teaches-its-use/                     # NEW folder (Article XIV; STOP 1 decision 1)
├── README.md                                         # what moved, from where, why (the ruling, verbatim), the Red observed at plan
└── tests/BridgeStandaloneGateTests_RetiredFacts.vb   # fact (4)'s text, header intact, as 005's retired facts are kept
```

`CandidateSideEnvelope` serves both the two sides and the `symbol` block: the block is a side plus `lastSeenRunId` and
`retiredInRunId`, which are `null` on a candidate's sides. One envelope, not two, and the data-model shows both uses.

**Structure Decision**: six projects stay six. The bridge gains one reader, five envelopes and one text module. Core
gains three reads and one record. The docs gain one folder. One test fact is archived.

## Design

### The tool's pipeline (R76–R78)

`BridgeTools.RenameCandidates(raw, solutionKey, runId, retiredSymbolId)` → `RunRead`:
- **Arguments**: `ScopeResolver.ValidateArguments` gives `ProjectIdRemoved` and `ScopeMissing`.
- **Configuration**, then **map**: `MapAccess.OpenRead`, one read transaction.
- **Question**: `RenameCandidatesReader.Read`, in five steps:
  1. The scope (`SolutionKeyUnknown`).
  2. The run check (`RunNotFound`, `RunOutOfScope`).
  3. `RequireInScope` for `retiredSymbolId` (`SymbolNotFound`, `SymbolOutOfScope`).
  4. `ReadCandidates`, then the runs of Q5 (`ReadById`, `ReadBySolution`, or the candidate runs ∪ `ReadRetiringRun`).
  5. The sides through `ReadById` (memoised), then the count check (`CandidateCountMismatch`), then the envelope.
- **Serialise → `EndRead` → answer**, exactly as the other seven read tools.

`BridgeToolBindings.RenameCandidates` takes the SDK's request context and hands `context.Params.Arguments` over, as
005 R68 set up. `BridgeServer.ToolDelegate` gains the `rename_candidates` case with
`Func(Of RequestContext(Of CallToolRequestParams), String, Long?, Long?, CallToolResult)`.

### The instructions (R74, R75)

`BridgeServerInstructions.Text` is a multi-line literal whose value is 191490 §1's block, with the file pinned
`eol=lf`. `RunAsync` sets `options.ServerInstructions` next to `options.ServerInfo`. Nothing else reads the constant
but the facts.

### The texts (R80–R83; contracts/usage-texts.md)

The six verbatim texts are written from the store (`render_document` 191490 or `document_sections`), block by the Q3
rule, and hashed back with the quickstart's §Verbatim before the task closes. The process document's edits are
Implementor text, reviewed with the diff. `settings.fragment.json` changes one string.

## Test design

| File | Facts (Red-first; every guard with a FIRE line) | Red before code | FIRE after Green |
|------|------------------------------------------------|-----------------|------------------|
| `B12_RenameCandidatesTests` (on `RenameScenario`: Sample runs 1–3, Other run 1) | (1) unfiltered: run 2's three candidates (Describe→Explain; Name→Label ×2, one per twin, each side's project named, bare ids, not folded); run 2 `countChecked` true; run 3 (failed, 1 recorded, 0 rows) listed, not checked, reason "failed run"; runs `id DESC`; `total` 3. (2) `retiredSymbolId` = retired Describe: one candidate; runs `[2]`; `symbol.retiredInRunId` 2; not checked, reason "filtered by retiredSymbolId". (3) `symbol_detail` on the retired id → `SymbolRetired` text contains "call rename_candidates with retiredSymbolId {id}"; the same id to `rename_candidates` answers. (4) `DeleteCandidate` on a copy → `runId` 2 refused `CandidateCountMismatch` "recorded 3 rename candidates but the map holds 2". (5) `RunNotFound`; `RunOutOfScope` (the Other solution's run, both keys named); `SymbolNotFound`; `SymbolOutOfScope`, each through the executable. (6) map SHA-256 unchanged across every call; `tools/list` annotates it read-only. (7) an active symbol's id → answered, `symbol.isActive` true, `retiredInRunId` null. (8) a solution with no candidates (the Other copy) → `total` 0, every run checked 0 = 0, the `note` sentence. (9) both filters: run 2 + Describe → `[run2]`, one candidate, not checked; run 1 + Describe → `[run1]`, 0, the note (analyze G4) | no tool registered: every fact red ("unknown tool") | (1): fold twins in the reader (group by path and line) → two candidates, not three → red. (2): list every run of the solution → three runs, not `[2]` → red. (4): skip the check → answered, not refused → red. (5) `RunOutOfScope`: drop the solution comparison → red; (5) `RunNotFound`: answer an unknown run with empty `runs` → red (analyze C1). (6): register `ReadOnly = False` → red. FR-509: a `New SqliteConnection` in `RenameCandidatesReader.vb` → the standalone gate (1) and the connection-site gate red (analyze G1). Revert each |
| `UsageTextsGateTests` | (1) `initialize`'s `instructions` equals `BridgeServerInstructions.Text` and contains no CR. (2) every registered name, as an identifier-bounded word, in the constant. (3) the four Q4 phrases, whitespace collapsed. (4) README table rows = registered names as a set. (5) skill and snippet carry the sentence, whitespace collapsed. (6) no file under `docs/` contains "Rick" or "153204". (7) the four tool-count places read nine | (1) red: no instructions; (2) red: no constant; (4) red: `rename_candidates` not in the table; (5) red: no files; (7) red: "8 tools"; (6) is a guard on absence, green first, trusted through its fire | (1): concatenate one `vbCrLf` into the constant → red; drop the assignment in `RunAsync` → red. (2): delete the `rename_candidates` line → red. (4): delete its row → red; add a row `**`ghost`**` → red. (5): delete the sentence from `SKILL.md` → red (analyze C1: its first Red was "file missing"). (6): paste §2's private block into the snippet → red. Revert each |
| `B01` (5) amended | the nine names as a literal list (not derived from `RegisteredToolNames`); `InitializeResult().instructions` equals the constant; version `0.3.0` | red: eight names listed, no instructions, 0.2.0 | drop the assignment in `RunAsync` → red (the spec's FIRE); revert |
| `B05` (18) amended | the fragment's command equals `"C:/path/to/CodeMem.Bridge.exe" hook`; contains no `C:/Users/` | red against today's fragment | — (the Red is the check) |
| `B08` (9), (10), (11) new, armed | (9) MemOS run 13: one candidate 5339 → 23219, `samePath`, 284, rank 1, `countChecked`. (10) 6754: 0 candidates, runs `[53]`, `retiredInRunId` 53. (11) MemOS unfiltered: answered, every completed run `countChecked`, `total` = the sum of recorded counts over completed runs (1 on 2026-09-29; checked live through the MemOS-side reader). No timing asserted; each duration recorded (analyze G3) | skipped unless `CODEMEM_LIVE_MAP` | — (acceptance; quickstart) |
| `B09` (4), (6) amended | (4) 9 descriptions; the `symbol_search` sentence among the caveat phrases; the new description free of the forbidden phrases. (6) 32 kinds | (4) red: 8; (6) red: 29 | — |
| `BridgeStandaloneGateTests` (4) **retired** (STOP 1 decision 1) | none: the fact leaves the suite. Its text goes to `_Archive/006-map-teaches-its-use/tests/BridgeStandaloneGateTests_RetiredFacts.vb` beside a README naming what moved, why, and the ruling. (1)–(3) stay; (3) still pins Article IX's text | **already red on `main` since `806b524`**: the Red is recorded in the file's header and in the archive README | — (a retirement is not a guard; (3)'s FIRE of 2026-09-17 stands) |

**Red on `main` before this feature (baseline, 2026-09-29)**:
- `BridgeStandaloneGateTests` (4), `TheConstitutionIsVersionOneFour`, the version line. Reproduced in both runs.
- One unnamed failure in the first run only, not reproduced in the second. Task 1 runs the suite with a TRX logger
  (`--logger "trx;LogFileName=baseline-006.trx"`), three times, and records every failure by name before anything is
  changed. A failure that appears there is recorded as a pre-existing intermittent defect and raised with the
  Architect. It is not fixed silently inside this feature.

**Expected Reds in existing tests, named before they happen**:
- B01 (5): the names, the version, the instructions.
- B09 (4): the count of 8, and later the new caveat phrase.
- B09 (6): 29 kinds.
- B05 (18): the path.
- Any fact asserting `SymbolRetired`'s old wording: none, per a text search over `tests/` for "consult the rename"
  (0 hits on 2026-09-29).
- `ProjectFileGateTests`, `FileHeaderGateTests`, `SqlLocationGateTests`, `BridgeSqlGateTests`, the tripwire, I02 and
  the 001–005 suites stay green. Any other Red is unexpected and stops the work.

**Order** (CON2 as in 004 and 005):
1. `BridgeStandaloneGateTests` (4) retired to the archive (its header and summary revised to three facts); the three
   TRX baseline runs: `main` green again, and any intermittent failure named, before anything else.
2. B12's Red (the scenario and the facts), then Core's three reads and the record, `RequireInScope`, the reader, the
   envelopes, the three refusal kinds and texts, the registration and the delegate, then B12 green and its fires.
3. `SymbolRetired`'s text with B12 (3).
4. `UsageTextsGateTests`' Red, then the instructions constant and the `.gitattributes` line, the assignment, the
   `symbol_search` sentence, and the tool description; B01 (5), B09 (4), (6) moved.
5. The kit files from the store; the README; the fragment with B05 (18); the process document.
6. The quickstart's §Verbatim: six `OK`.
7. Both builds, both suites.
8. The acceptance on a copy.

## Operator steps (after implementation; outside this feature's code)

Quickstart §"Acceptance on a copy". Then, by hand and optional:
- copy the skill to `~/.claude/skills/codemem/` and append the snippet to `~/.claude/CLAUDE.md`, per step 6;
- place the private lines of 191490 §2 in the Architect's own `~/.claude/CLAUDE.md`.

The registration-scope check (task 152685) is the Operator's and does not gate this feature.

## Known limits (recorded)

- **The instructions are checked one way.** Every registered tool must appear in them, but a removed tool still named
  in them is caught by the review gate, not a test (FR-521, as ruled: a snake_case scan over prose would misfire).
- **The retiring run of an active symbol is not derivable.** A symbol retired and later reactivated shows the runs
  that wrote candidates for it and no retiring run (Q2, Q6). The map keeps no retirement history beyond candidates.
- **The README sketch** keeps its column alignment only because "8" → "9" is one character.
- **An unfiltered call on a long-lived solution lists every run**: 97 for MemOS today, uncapped by the description.
  It is the caller's choice. Every completed MemOS run reconciles today (checked live 2026-09-29), so the call is
  answered, not refused. B08 (11) holds it on the copy.

## STOP 1 — RULED 2026-09-29 (Architect)

Decisions 2–5 and 7–10 are approved as written. The rest:

- **Decision 1, amended: fix first, and retire (4) rather than move it.** `BridgeStandaloneGateTests` (4)
  (`TheConstitutionIsVersionOneFour`) goes to `_Archive/006-map-teaches-its-use/` as a retired fact. It is not moved
  to 1.5.0. The Architect's reason: "fact (3) pins Article IX's text, which is what it protected; (4) now only breaks
  main on every amendment." The intermittent failure is raised if it recurs.
- **Decision 6, approved with two edits to the description**, now in contracts/tools.md §2:
  - after "runId narrows to one run." add "Without runId or retiredSymbolId, every run of the solution is listed,
    newest first.";
  - "the candidates must equal the run's recorded count" → "the number of candidates returned must equal the run's
    recorded count".
- **Decision 11, overruled on both exclusions.** In `src/CodeMem.Bridge/README.md`:
  - drop "Every tool is scoped by `solutionKey`", the same ruling as Q8 (b);
  - replace install step 4's machine paths with `C:/path/to/CodeMem.Bridge.exe`, the same fix as the fragment.

  Deletions and a substitution only; no new wording. contracts/usage-texts.md §4 carries both.

The decisions as they stood for the ruling:

1. **Main is red since `806b524`.** `BridgeStandaloneGateTests` (4) moves to `1.5.0` as task 1, its Red recorded as
   already observed. The fact's name, `TheConstitutionIsVersionOneFour`, is renamed `TheConstitutionIsVersionOneFive`
   in the same edit. Task 1 also runs the suite three times with a TRX logger to name the first plan-time run's
   unreproduced failure. If it recurs, it is raised, not fixed inside 006. *(Amended at the ruling: retired to the
   archive, not moved.)*
2. **The instructions constant** (R75): `BridgeServerInstructions` beside the descriptions, a multi-line literal, the
   file pinned `eol=lf`, a no-CR fact.
3. **The reader** (R76): three `Read…` methods, the one new record `RenameCandidateRecord`, sides through
   `ReadById` memoised, `RequireInScope` split out of `RequireActive`.
4. **The retiring run** (R77): the first completed run of the solution after `last_seen_run_id`, one repository
   method.
5. **The count check** (R78): refuse on the first mismatching completed run, newest first; failed runs listed, never
   checked; the mismatch fixture by one test-side delete in `MapQueries`.
6. **The tool description** (R81; contracts/tools.md §2), the one shipped text with no ratified source. Rule it,
   edit it, or send it to the Documenter.
7. **The refusal texts** (R80; contracts/tools.md §6), and the three kinds placed after `NotAType`.
8. **The fixture** (R79): `RenameScenario` with the I06 rename and the twin rename (run 2); a further rename under
   corrupted counts, a real failed run recording one candidate (run 3); an unchanged `Other` solution.
   `TwinScenario.Prepare` made `Friend`.
9. **Acceptance** (R84): B08 (9) and (10) against a copy through `CODEMEM_LIVE_MAP`, the live map's hash taken in the
   same command.
10. **Versions**: both bridge projects 0.3.0; the extractor 0.3.0 and schema 3 untouched.
11. **The process document** (contracts/usage-texts.md §4), including the two recorded exclusions. *(Overruled at
    the ruling: both are changed.)*

## Complexity Tracking

No Constitution Check violation to justify.

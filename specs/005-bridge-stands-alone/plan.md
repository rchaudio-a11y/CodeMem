# Implementation Plan: CodeMem 005 — The Bridge Stands Alone: One Database, `.slnx` as an Equal Input, Warnings as Warnings

**Branch**: `005-bridge-stands-alone` (from `main` at `90d62ca`) | **Date**: 2026-09-16 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/005-bridge-stands-alone/spec.md` (clarified 2026-09-16: Q1, Q2, Q3, Q6, Q8 ruled; Q4, Q5,
Q7, Q9, Q10 stand as proposed until STOP 1). **Research**: [research.md](research.md) R60–R73 — the `.slnx` route and the warning
mechanism proven with a scratch harness before this plan was written, as 152688 required.

**Governing document**: `.specify/memory/constitution.md` v1.3.0 today; **v1.4.0 at T003** (R70's wording, FR-431), after which
Article IX reads as v1.2.1 and the connection-site gate names one file. The store code stays until T024, the close of US2, when
nothing references it any more — the migration path the amendment itself names — and the gate test's Red is recorded and its
assertion amended in that task (analyze C3, 2026-09-17).

**Analyze**: 2026-09-17 — nineteen findings, all accepted by the Architect and applied here, in the spec, the tasks, research R63/R69
and the quickstart; the finding IDs are cited where a passage changed.

**Ceremony**: STANDARD, no spike (spec). **One stop, after this plan, before tasks** (§STOP 1): ten decisions the Architect rules or
strikes; nothing in Phase 2 starts before.

## Summary

Cut the bridge's second database and make the map the only source of every answer: scope by `solutionKey` alone; resolve a build
directory to a key through `solutions.repo_root` (or the solution file's directory when there is none) with `SolutionScope`, the one
containment door; answer a directory the map does not hold with one sentence naming the exact `extract --solution-key <key>
--solution <file>` that would add it, and let the Architect run it through the same gate, door and log; list every such directory
under `map_status` as `not_in_map`, read from the extract log the hook already writes. Archive `StoreDatabase`, the registry module and
their readers; retire seven refusal kinds and add four; revert Article IX to v1.2.1 and the connection gate to one file (v1.4.0).

In the extractor: the loader parses a `.slnx` itself and opens its projects into one workspace (route (b), proven: two projects,
references intact, the same fact set as the `.sln`); a workspace Failure that NuGet's own assets file records as a **warning** is
recorded on the run at **schema version 3** (one typed table, in-place upgrade as 002 did) and the load continues, while an assets-file
**error** still stops it by name; `warnings=N` joins the summary line. No package moves; no environment variable is read.

## Technical Context

**Language/Version**: VB.NET on .NET 8 (`net8.0`), Option Strict/Explicit On, Infer Off, `GenerateDocumentationFile` on; every touched
project keeps its four settings. `CodeMem.Extraction` `<Version>` moves to 0.3.0 (the fact set for one digest is unchanged, but the
summary line and the inputs are not — a consumer parsing the line should see a new version); `CodeMem.Bridge`/`Bridging` to 0.2.0.

**Primary Dependencies**: unchanged — `Microsoft.CodeAnalysis.*.Workspaces` 4.14.0, `Microsoft.Build.Tasks.Core` 17.8.43 (002's pins
stand: R60, R61 move nothing), `LibGit2Sharp` 0.32.0, `ModelContextProtocol.Core` 1.4.0, `Microsoft.Data.Sqlite` 8.0.31. **No
`Microsoft.Build.Locator`, no package bump.** No reference of any kind to anything under `rchaudio-a11y\MemOS` (004 FR-302's gate stays).

**Storage**: the map, read-only from the bridge, read-write from the extractor, **schema version 3** (R63: `extract_run_warnings`; the
version-1 and version-2 DDL texts frozen; migration 2 → 3 one added text; the abort seam covers it). One append-only text log, now also
read by `map_status` (R64). No store, no second database, no sidecar file.

**Testing**: xUnit, real SQLite, the real compiled fixture (`Sample.sln` and its committed `Sample.slnx` twin), two new warning fixtures
restored offline from a 3.6 KB hand-packed package (R71), a real repository through LibGit2Sharp, the real executables over stdio and
as child processes. New: `Bridge/B09`–`B11`, `Extraction/X01`–`X02`, `Fixpack/S03`, `Guards/BridgeStandaloneGateTests`; amended:
B01, B02, B04, B05, B06, B08, F06, S02, US1, `SqlLocationGateTests`, `BridgeSqlGateTests`; archived: `RegistryFixture`, two facts of
`BridgeSqlGateTests`. Live facts Skip-armed by `CODEMEM_LIVE_MAP` only (`CODEMEM_LIVE_STORE` retired).

**Target Platform**: Windows 11 developer workstation; .NET SDK 10.0.401 hosting `net8.0` builds; Claude Code as the MCP client.

**Project Type**: CLI/stdio server over class libraries; the six existing projects; **no new project** and no removed project.
`_Archive/004-store/` at the repository root is a folder, not a project.

**Performance Goals**: SC-301 inherited — each read tool under 3 s on the live map; `map_status` with five repositories and a 200-line
log under 3 s (asserted with a Stopwatch in B11); the `.slnx` open time relative to the `.sln` open is measured and recorded in the
quickstart, not asserted (2.4 s vs 1.9 s in the harness — the per-project opens cost a build-host round trip each; analyze A1); the
hook entry's own cost unchanged (< 100 ms).

**Constraints**: Article IX at v1.4.0 (one database, one connection site); Article XI (SQL in Core; the new table's SQL in one
repository; neither bridge project holds a literal); Article XII (one door each for: the map open, scope, the extract verb, target
resolution, the suggestion, the `.slnx` parse, the warning rule, refusal wording, configuration); Article XIV (archive, never delete);
Article XV (no script, no foreign runtime — the package is packed by `dotnet pack`, the fixtures restored by `dotnet restore`);
nothing writes to stdout in `serve` but the transport; the extractor reads no environment variable except its test seams.

**Scale/Scope**: five mapped solutions (15 792 + 2 811 + 2 170 + 1 644 + 725 active symbols); eight tools; one hook; one log; two
extractor changes; one schema version; one constitution amendment; ~30 production files touched, 8 archived.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Article / Gate | Status | How this plan satisfies it |
|----------------|--------|----------------------------|
| I. Library-First | PASS | No new project. `CodeMem.Bridge` stays wiring only (its command line gains one option); every rule lands in `CodeMem.Bridging`, `CodeMem.Extraction` or `CodeMem.Core`. One class per file: `SlnxReader`, `AssetsLog`, `RunWarning`, `SolutionFileSuggestion`, `NotInMapReader`, `ExtractRunWarningsRepository`, `RunWarningRecord`, `NotInMapEntryEnvelope`, `RunWarningEnvelope` are each their own file. |
| II. Test-First | PASS | Every new behaviour is a fact observed Red first (§Test design); the two new guards carry their FIRE; the schema bump's Reds in 002's tests are named before they happen; the connection-site gate's Red is the named Red of v1.4.0. |
| III. Integration-First | PASS | Real SQLite (fixture maps, a version-1 and a version-2 map, a hand-bumped version 4), the real compiled fixture in both forms, real restore warnings from real NuGet on real projects (R71), a real repository, the real executables. No mock of Roslyn, NuGet, SQLite or the log. |
| IV. Compiler Fact Only | PASS | Nothing derived: warnings are NuGet's own rows copied verbatim (code, level, message) and excluded from the canonical fact set; `not_in_map` is what the log recorded, filtered by the map; the suggestion is a file name. Determinism (I02) unchanged and re-asserted across formats. |
| V. Green Only, Stamped | PASS | Unchanged: a restore *error* still refuses before the compiler; a compile error still exits 2; a warning never hides either (R62: the compilation is produced and judged as always). The run row is still the first fact-table write; warning rows follow it inside the same transaction. |
| VI. Reconcile, Never Truncate | PASS | No write path changes; the tripwire scans Core's repositories, where one INSERT (warnings) and SELECTs are added; nothing touches `code_symbols`. |
| VII. Evidence on Every Row | PASS | A warning row names its run, its code, the project file and NuGet's message; an occurrence is unchanged. |
| VIII. Counts That Reconcile | PASS | Unchanged; the summary line's `warnings=N` is an addition after the ten counts and the residuals. |
| IX. One File, Many Solutions, One Writer | PASS under v1.4.0 (T003; the archive at T024 — STOP 1 decision 1) | The bridge opens the map and nothing else; the store code is archived; a gate asserts zero `SqliteConnection` constructions and zero path-shaped, case-sensitive `memos` in the bridge projects (analyze I1) and exactly one site under `src/`. The extractor stays the sole writer; the bridge's only writes are the log and the child it launches. |
| X. Absence Must Be Representable | PASS | `project_path` NULL when the failure names no project; `warnings` an empty list, never absent; `notInMapError` null when the log read; `suggestedKey`/`command` null when none; no sentinel anywhere. Typed rows, never a blob. |
| XI. Anti-Abstraction | PASS | Microsoft.Data.Sqlite direct; the new SQL in one named Core module; no new seam (the launcher seam and `ReadSeams` are reused); `SolutionFileSuggestion` has two call sites (the refusal, the status list) and exists because Article XII forbids the rule at two doors, not as a Rule-of-Three abstraction — stated here so the review sees it. |
| XII. One Door for Every Rule | PASS | Map open + pin: `MapAccess`; scope: `ScopeResolver`; extract: `ExtractDoor`; resolution: `TargetResolver` over `SolutionScope.Resolve` + `ContainsDirectory` (the extractor's root rule, one door — Q6); the suggestion: `SolutionFileSuggestion`; the `.slnx` parse: `SlnxReader`; the warning rule: `AssetsLog` + the loader; wording: `BridgeRefusal`; configuration: `BridgeConfigFile.Load`; the log's line shape: `ExtractLog` (written) and `NotInMapReader` (read) share one column contract in `contracts/cli-config-hook.md` §5. |
| XIII. Production-Route Reachability | PASS | Every refusal and every new shape is driven once through `CodeMem.Bridge.exe` over stdio or through the `extract`/`hook` entries; the extractor's new refusals through `CodeMem.Extractor.dll` as a child (exit code + stderr); the `projectId` refusal through the real server (R68's first Red). |
| XIV. Archive, Never Delete | PASS | Eight files move to `_Archive/004-store/` with a README (R69); no file is deleted; the seven retired refusal texts are recorded there. `DSP_Processor.sln` is the Operator's, in another repository. |
| XV. Compiled .NET, No Foreign Runtime | PASS | No script; the research harness and its Python comparison are scratch and are not shipped; the fixture package is built by `dotnet pack`. |
| Gate: Option settings in every project file | PASS | No project file changes its settings; `ProjectFileGateTests` green. |
| Gate: no SQL outside a named repository method | PASS | The new table's DDL lives in `SchemaRepository` (Core/Schema), its INSERT and SELECT in `ExtractRunWarningsRepository` (Core/Repositories); neither bridge project gains a literal with a SQL keyword (the descriptions were written around the tree-wide scan). |
| Gate: `New SqliteConnection` in exactly one named file (v1.4.0) | PASS (after T024) | `MapDatabase.vb`; `SqlLocationGateTests.OnlyMapDatabaseOpensAConnection` goes red when `StoreDatabase.vb` leaves `src/`, is amended to the one file in that task, and re-fired. |
| Gate: header block + XML docs | PASS | `FileHeaderGateTests` scans `src/` and `tests/`; every new and touched file complies; archived files keep their headers. |
| Gate: tripwire (I13) | PASS | Core gains one INSERT in a new repository whose name the tripwire's write scan sees; the destructive-form absence holds. |
| Gate: every guard fires; residuals block; candidates never applied | PASS / unchanged | New guards: the standalone gate (FIRE: one `New SqliteConnection` in Bridging; one literal `memos.sqlite`), the Article IX text fact (FIRE: one word changed in the transcription), the archived-names gate (FIRE: `Dim probe As StoreDatabase` in a test); the amended connection gate re-fired. |
| Gate: three call sites for a new abstraction | PASS | None introduced; see XI for the one two-site module and why. |

**Post-design re-check**: unchanged. Article IX passes only under v1.4.0, which is why the amendment is T003 and STOP 1's first
decision, and why the archive closes US2 at T024 (analyze C3); every other row passes under either version.

## Project Structure

### Documentation (this feature)

```text
specs/005-bridge-stands-alone/
├── spec.md                     # clarified 2026-09-16 (five rulings)
├── plan.md                     # this file
├── research.md                 # R60–R73 (the harness runs, the assets-file finding, the v1.4.0 wording, the fixtures)
├── data-model.md               # the deltas: scope, observed directory, map_status, extract, config, run warning, kinds, .slnx grammar, the warning rule
├── contracts/
│   ├── tools.md                # parameters, the eight descriptions, shapes 3.1/3.2/3.7/3.8, order of refusal, the new and revised texts
│   ├── cli-config-hook.md      # the command line (+ --solution), the hook's .slnx token, the config without storePath, the log, the README's sections
│   └── extractor.md            # inputs, the .slnx grammar, the warning rule, the summary line and usage, MIGRATION 2 -> 3, the Core API
├── quickstart.md               # fixture validation per story, the live sequence, the record table
├── checklists/requirements.md
└── tasks.md                    # /speckit-tasks output — after STOP 1
```

### Source Code (repository root) — files that appear, change or leave

```text
.specify/memory/constitution.md                    # v1.4.0 (R70): Article IX paragraph removed; the gate line at one file; the amendment entry
_Archive/004-store/                                # NEW folder (R69): README.md, the eight files below (headers intact) and tests/ — the retired facts of B02, B04, B05 and BridgeSqlGateTests as .vb fragments (analyze C2)
src/CodeMem.Core/
├── Schema/SchemaVersion.vb                        # Current = 3
├── Schema/SchemaState.vb                          # + Version2
├── Schema/SchemaRepository.vb                     # + MigrationToVersion3 (frozen text), UpgradeToVersion3
├── Repositories/MapDatabase.vb                    # InspectSchema: 2 -> Version2, 3 -> Current
├── Repositories/ExtractRunWarningsRepository.vb   # NEW: InsertAll, ReadByRun (the only SQL naming extract_run_warnings)
├── Records/RunWarningRecord.vb                    # NEW
├── Repositories/StoreDatabase.vb                  # -> _Archive/004-store/
├── Repositories/Registry/CodeMapSolutionsRepository.vb, RegistryTableMissingException.vb   # -> _Archive/004-store/
└── Records/RegistryRecord.vb                      # -> _Archive/004-store/
src/CodeMem.Extraction/
├── Workspace/SolutionLoader.vb                    # the three-way extension door; .slnx via SlnxReader + OpenProjectAsync per project; the warning rule; Warnings property
├── Workspace/SlnxReader.vb                        # NEW: the parse of data-model §10; its four refusals
├── Workspace/AssetsLog.vb                         # NEW: reads <project dir>/obj/project.assets.json logs[]; the match by message
├── Workspace/AssetsLogMatch.vb                        # NEW: the record AssetsLog.Classify returns (code, level, project, message); the loader's Warnings list holds Core's RunWarningRecord (analyze I2)
├── Workspace/WorkspaceLoadException.vb            # unchanged; the restore-error line is composed by the loader
├── Workspace/SolutionScope.vb                     # + NormalizeDirectory (additive): the one directory normalisation the resolver and the log reader share (T028; analyze I2)
├── Run/ExtractionRun.vb                           # step 4: the three upgrade paths; after InsertCompleted/InsertFailed: InsertAll(warnings); summary with warnings
├── Run/SummaryLine.vb                             # + warnings=N
└── CodeMem.Extraction.vbproj                      # Version 0.3.0
src/CodeMem.Extractor/CommandLine.vb               # usage: the three inputs; the failure classes
src/CodeMem.Bridging/
├── Configuration/BridgeConfig.vb                  # - StorePath
├── Configuration/BridgeConfigFile.vb              # storePath present -> ConfigKeyRetired
├── Refusals/BridgeRefusalKind.vb                  # - 7 + 4 = 29
├── Refusals/BridgeRefusal.vb                      # the revised and new texts (contracts/tools.md §6)
├── Reading/BridgeSchemaPin.vb                     # Required = 3
├── Reading/ScopeResolver.vb                       # solutionKey only; ValidateArguments(rawArguments, solutionKey): ProjectIdRemoved, ScopeMissing
├── Reading/StoreAccess.vb                         # -> _Archive/004-store/
├── Reading/Envelopes/ScopeEnvelope.vb             # by, solutionKey, solutions[] only
├── Reading/Envelopes/LatestRunEnvelope.vb         # + Warnings (count)
├── Reading/Envelopes/MapStatusEnvelope.vb         # - StorePath, Bound, Unbound, Inactive; + NotInMap[], NotInMapError
├── Reading/Envelopes/MapStatusEntryEnvelope.vb    # - ProjectId, CodememSolutionId; + SolutionId
├── Reading/Envelopes/NotInMapEntryEnvelope.vb     # NEW
├── Reading/Envelopes/RunWarningEnvelope.vb        # NEW
├── Reading/Envelopes/UnboundEntryEnvelope.vb, InactiveEntryEnvelope.vb   # -> _Archive/004-store/
├── Reading/Readers/SolutionsReader.vb             # latestRun.warnings via ReadByRun
├── Status/MapStatusReader.vb                      # entries from solutions rows; verdict order minus map_missing_solution; notInMap via NotInMapReader
├── Status/NotInMapReader.vb                       # NEW (R64): the log -> entries, deduplicated, filtered by the map, suggestion recomputed
├── Extract/ExtractRequest.vb                      # + SolutionPath
├── Extract/ExtractDoor.vb                         # no store stage; cardinality with the optional path; run.warnings read back
├── Extract/TargetResolver.vb                      # map rows; root = SolutionScope.Resolve(repoRoot, dir(lastSeenPath)); by key with an explicit path
├── Extract/SolutionFileSuggestion.vb              # NEW (R66): Inspect(directory) -> files, key, command, ambiguity
├── Extract/SolutionFileInspection.vb              # NEW: the record Inspect returns (readable, files, suggestedKey, command, ambiguous) (analyze I2)
├── Extract/ExtractRunEnvelope.vb                  # + Warnings[]
├── Extract/StaleResult.vb                         # + NotInMap[]
├── Extract/HookRequest.vb                         # .slnx token
├── Mcp/BridgeTools.vb                             # no projectId; RequestContext(Of CallToolRequestParams) per tool (R68); Extract(solutionKey, solutionPath, repoPath, stale)
├── Mcp/BridgeToolDescriptions.vb                  # the eight texts of contracts/tools.md §2
└── CodeMem.Bridging.vbproj                        # Version 0.2.0
src/CodeMem.Bridge/
├── BridgeCommandLine.vb                           # + --solution (extract only, beside --solution-key); usage text
├── Mcp/BridgeServer.vb                            # the delegates' new signatures
├── bridge.config.sample.json                      # - storePath
├── README.md                                      # contracts/cli-config-hook.md §6
└── CodeMem.Bridge.vbproj                          # Version 0.2.0
tests/CodeMem.Tests/
├── Fixtures/Sample/Sample.slnx                    # NEW: the two-line twin
├── Fixtures/Warnings/NuGet.config, packages/CodeMem.NetFxOnly.1.0.0.nupkg, NetFxOnly/ (source), Nu1701/, Nu1101/   # NEW (R71)
├── Bridge/B09_StandaloneTests.vb                  # NEW: US1
├── Bridge/B10_MapResolutionTests.vb               # NEW: US2 resolution + the not-in-map texts; US3 the by-key-with-path shape
├── Bridge/B11_NotInMapTests.vb                    # NEW: US3 scenario 6 (the log, dedup, restart, disappearance, old kinds, unreadable log, the Stopwatch)
├── Extraction/X01_SlnxTests.vb                    # NEW: US4
├── Extraction/X02_WarningTests.vb                 # NEW: US5
├── Fixpack/S03_SchemaVersion3Tests.vb             # NEW
├── Guards/BridgeStandaloneGateTests.vb            # NEW: the memos-free gate, the archived-names gate, Article IX's text, the version line
├── Guards/SqlLocationGateTests.vb                 # amended: one file (after its Red)
├── Guards/BridgeSqlGateTests.vb                   # (2) and (4) archived; (1) and (3) stay
├── Bridge/B01, B02, B04, B05, B06, B08            # amended in place (§Test design)
├── Fixpack/F06, S02; Invariants/US1               # amended: 2 -> 3 (after their Reds)
├── Support/FixtureSolution.vb                     # + SolutionXPath
├── Support/WarningsFixture.vb                     # NEW: restores the two warning projects once, offline
├── Support/WarningsCollection.vb                  # NEW: the Warnings collection definition (analyze I2)
├── Fixtures/Maps/version2.sqlite                  # NEW: a version-2 map committed before T009, beside sample-v1.sqlite (S03 (3); B02's version-2 VersionBelow fact) (analyze I2)
├── Support/MapQueries.vb                          # + ReadWarnings(db, runId); ReadSchemaVersion unchanged
├── Support/MapStatusScenario.vb, ExtractScenario.vb, FixtureMapScenario.vb, LiveBridge.vb, BridgeHost.vb   # revised: no store, no registry
└── Support/RegistryFixture.vb                     # -> _Archive/004-store/
```

**Structure Decision**: six projects stay six. The bridge loses a door (the store) and gains three modules (the suggestion, the log
reader, two envelopes); the extractor's loader gains two doors (the `.slnx` parse, the assets-file match) and Core gains one table's
repository. Retired code goes to `_Archive/004-store/` at the root, outside every project and every gate's scan.

## Design

### Resolution and the not-in-map answer (R65, R66)

`ExtractDoor.Run`: cardinality (`TargetMissing` names the three shapes and the optional path) → configuration (`ConfigKeyRetired` before
`Unconfigured`'s key checks) → gates → the map, opened once → `TargetResolver`: by key without a path → `ReadByKey` or
`SolutionKeyUnknown` (with the add remedy); by key with a path → no lookup, the target is the pair; by path → every `solutions` row's root
through `SolutionScope.Resolve(repoRoot, dir(lastSeenPath)).ContainsDirectory`, longest wins, tie `AmbiguousRoot`, none →
`SolutionFileSuggestion.Inspect(repoPath)` → `PathNotInMap` (with key and command, or placeholders) or `AmbiguousSolutionFile` → the map
closed → the semaphore → the launch (`--solution-key <key> --solution <path> --db <map>`) → the run read back with its warning rows →
the log line (target as given: `solutionKey=<key> solution=<path>` for the new shape). The hook entry hands `repoPath` exactly as
before; its answer line carries the refusal's full text.

### `map_status` (R64)

`MapStatusReader.Read(config, map)`: every `solutions` row → its latest completed run → `RepositoryFacts.Read(root, sha)` where root is
the same `SolutionScope` answer → verdict in the ruled order (`no_git` → `dirty` → `diverged` → `behind` → `current`). Then
`NotInMapReader.Read(mappedRoots)`: parse the log, keep the two kinds, one entry per normalised directory, drop those a root contains,
`Inspect` the rest. The map facts are read first and the repositories asked after (004's rule), the log last.

### `projectId` refused by name (R68)

Each tool method takes `context As RequestContext(Of CallToolRequestParams)` (injected, excluded from the schema); `BridgeTools` passes
`context.Params.Arguments` to `ScopeResolver.ValidateArguments`, which refuses `ProjectIdRemoved` when the dictionary holds `projectId`
(ordinal or ignoring case), before `ScopeMissing`. `BridgeHost` builds the same context for the in-process facts. `solutions` and
`map_status` take the context too: a `projectId` sent to them is refused the same way (FR-406 says every tool).

### The loader (R61, R62)

`SolutionLoader.Open`: extension → `.sln`: as today; `.vbproj`: as today; `.slnx`: `SlnxReader.Read(path)` (refuses by name before
the workspace exists) then, for each project path in order, `OpenProjectAsync` unless `workspace.CurrentSolution` already holds it;
`Solution = workspace.CurrentSolution`; other extensions refused. Failures collected during the opens are then classified:
`AssetsLog.Match(failureMessage)` → Warning → `Warnings.Add(RunWarning)`; Error → `WorkspaceLoadException("restore error <code> in
<project>: <text>")`; unmatched → `WorkspaceLoadException(text)` as today. `CompileAll` is called only after `Open` returns, so a
restore error never reaches the compiler — the observable X02 (3) asserts (no `errors=` line on stderr).

### Schema version 3 (R63)

`ExtractionRun` step 4 branches on `SchemaState`: Fresh (1 → 2 → 3), Version1 (2 → 3), Version2 (3), Current, Foreign, Newer;
`DuringUpgrade` after the last migration statement; `SetSchemaVersion(3)`. After `InsertCompleted`/`InsertFailed`:
`ExtractRunWarningsRepository.InsertAll(db, runId, loader.Warnings)` with `project_path` made solution-relative by `SolutionPaths.Relative`
(the record, the repository and the calls land at T034–T036 behind X02's Red, not with the schema — analyze C1).
`SummaryLine.Format` gains `warnings`. The bridge's `ReadRun` reads `ReadByRun` into `run.warnings`; `SolutionsReader` counts it.

### The archive and the amendment (R69, R70)

T003 makes the constitution's three edits (R70's text verbatim); the archive move is T024, at the close of US2, once US1 and US2 have
taken every store reference out of the code — with `SqlLocationGateTests`'s Red recorded in its header before the assertion is
amended to `{"MapDatabase.vb"}` and re-fired; `BridgeSqlGateTests` (2) and (4) and the retired facts of B02, B04 and B05 move to
`_Archive/004-store/tests/` as `.vb` fragments in the same task (Article XIV; analyze C2). Between T003 and T024 the tree is the
amendment's migration path in progress (analyze C3).

## Test design

| File | Facts (Red-first; every guard with a FIRE line) | Red before code | FIRE after Green |
|------|------------------------------------------------|-----------------|------------------|
| `B09_StandaloneTests` | (1) every tool answers on a fixture map with no store file and no `storePath` — through the executable over stdio; (2) `projectId` on each of the eight → `ProjectIdRemoved`, nothing opened (the config path is absent and still no `Unconfigured`); (3) `storePath` in the config → `ConfigKeyRetired` naming it, on a read tool and on `extract`; (4) the eight descriptions carry every 004 caveat phrase and none of the four forbidden phrases; (5) `map_status` envelope has no `bound`/`unbound`/`inactive`/`storePath`; `scope` envelope has no registry counts | (2) red: `projectId` accepted (or ignored — the assertion is on the refusal text); (3) red: `storePath` tolerated; (4) red: "projectId" present; (5) red: fields present | (2): drop the raw-argument check → silently ignored → red; revert |
| `B10_MapResolutionTests` | (1)–(4) resolution over map rows (root and subdirectory, nested, shared, NULL-root fallback — Q6); (5) one `.slnx` → `PathNotInMap` with `extract --solution-key <key> --solution <file>` in the text; (6) none → placeholders; (7) two files → `AmbiguousSolutionFile` naming both; (8) the suggestion never looks up or down; each refusal with 0 launches (the seam) and the hash unchanged; (9) the not-in-map answer through the executable; (10) `--solution-key Fresh --solution <copy>` → one real launch (shared by (11) and B11 (4) through a class fixture — analyze I3), exit 0, a new row, ten balanced counts; (11) repeated → matched > 0, new = 0, one row; (12) key alone, unknown → `SolutionKeyUnknown` with the add remedy; (13) path alone → `TargetMissing`; (14) the five gate cells for the new shape; (15) one log line per call with the new target form; (16) adding through the command line reaches the door (the `.slnx` hook token lives in B06; the numbering is the tasks' — analyze I5) | no path shape, no suggestion: every fact red for its stated reason | (8): walk up one directory in `Inspect` → the parent's `.sln` suggested → red (Q4); (4): skip NULL-root rows → red; revert |
| `B11_NotInMapTests` | (1) after one refusal `map_status` lists the directory once with key and command; (2) two refusals → one entry, the later time; (3) a new `BridgeTools` instance over the same log lists it (restart equivalence); (4) after `--solution-key … --solution …` maps it → gone, the log line intact; (5) a 004-era `PathNotRegistered` line → ignored; (6) an unreadable log → `notInMapError`, entries intact; (7) Stopwatch: `map_status` over a 200-line log under 3 s; (8) `extract(stale)` lists an observed directory under `notInMap` and launches nothing for it (FR-420; analyze G1); every fact asserts only on entries whose path is its own temp directory (analyze A2) | no reader: (1) red (no `notInMap`) | (1): filter lines by process start → after a restart red; revert |
| `X01_SlnxTests` | (1) parity: two fresh maps, identical fact sets, ≥ 1 S/P/E line; (2) key `Sample` for both; (3) `last_seen_path` `.sln` / `.slnx`; (4) a missing project path → exit 1 naming it, 0 rows, before any open (asserted: the refusal arrives in under 5 s — SC-406's bound, analyze I4 — and no build-host process was started; the line is the reader's sentence); (5)–(7) malformed XML, no `Solution` root, no project → their sentences; (8) a `<Folder>`-nested project and forward slashes → opens; (9) `.txt` → `unsupported solution file`; (10) usage names the three inputs and the failure classes | (1) red: exit 1 "No file format header found" | (1): open only the first project → the App's symbols missing → red; revert |
| `X02_WarningTests` | (1) NU1701 fixture → exit 0, ≥ 1 row `NU1701` naming the project, `warnings=1` on the line; (2) + a compile error → exit 2, 0 rows; (3) NU1101 fixture → exit 1, stderr names `NU1101` and the project, no `errors=` line, 0 rows; (4) the child's environment carries no `NoWarn` (asserted on the launch) and the outcome equals a run with `NoWarn=NU1701` set (the variable is inert); (5) `solutions` latestRun.warnings = 1, `extract` run.warnings lists the row (through the bridge); (6) an unmatched Failure (a fixture with a broken project reference) still aborts with its text | (1) red: exit 1 "workspace load failed: Msbuild failed …" | (3): treat every matched entry as a warning → NU1101 continues → red; revert |
| `S03_SchemaVersion3Tests` | (1) fresh → 3, the table present; (2) a version-1 map → 3, rows kept; (3) a version-2 map (a copy of today's fixture map) → 3; (4) abort `DuringUpgrade` on a version-2 map → still 2, the table present, the next run completes; (5) fresh vs upgraded (from 1 and from 2) `sqlite_master` text byte-identical; (6) an exit-4 run writes its warning rows beside the failed run row | (1) red: 2 | (4): move `AbortIf` after `SetSchemaVersion` → the map reads 3 after the abort → red; revert |
| `BridgeStandaloneGateTests` | (1) 0 `SqliteConnection` and 0 case-sensitive, path-shaped `memos` under the two bridge projects (`MemOS` in prose passes — analyze I1; a positive count of `.vb` files first); (2) the eight archived names absent under `src/` and `tests/`; (3) Article IX's body equals the v1.2.1 transcription; (4) the version line reads `1.4.0` | guards on absence: (1) and (2) red until the archive task; (3) and (4) red until the amendment | (1): `Dim probe As SqliteConnection = New SqliteConnection("x")` in `MapAccess.vb` → red; a literal `"memos.sqlite"` → red; (2): `Dim r As StoreDatabase` in a test → red; (3): one word changed → red; revert each |
| `SqlLocationGateTests` (amended) | exactly `{"MapDatabase.vb"}` | red the moment `StoreDatabase.vb` leaves `src/` (the named Red of v1.4.0) | `New SqliteConnection` in `NotInMapReader.vb` → red; revert |
| `B01`, `B02`, `B04`, `B05`, `B06`, `B08` (amended) | B01: `projectId 132040` calls removed, `solutionKey` calls kept, the hash facts unchanged; B02: `ScopeConflict`/`RegistryAbsent` facts archived, `ScopeMissing`'s new text, `ProjectIdRemoved`; B04: entries from map rows, `map_missing_solution` fact archived, no unbound/inactive; B05: the registry refusal facts replaced by map-based ones (`PathNotInMap` for the old `PathNotRegistered` case, `SolutionKeyUnknown` for `KeyNotRegistered`), `KeyUnbound`/`KeyInactive`/`MapMissingSolution` facts archived; B06: the `.slnx` token; B08: five entries, `CODEMEM_LIVE_STORE` gone, `map_status` entries = `solutions` count | each amended fact red for its stated reason when the parameter or field goes | — |
| `F06`, `S02`, `US1` (amended) | the version asserts 2 → 3; S02 (3)'s "newer" fixture 3 → 4 | red when `Current` becomes 3 | — |

**Expected Reds in existing tests, named before they happen**: `SqlLocationGateTests.OnlyMapDatabaseOpensAConnection` (the archive
task); `F06_FreshMapTests` ×2, `S02_SchemaUpgradeTests` (1), (3), (4), `US1_FirstRunTests` (the version-3 task);
`tests/CodeMem.Tests/Guards/RefusalTests.vb:78` (the anchored summary-line regex) and `tests/CodeMem.Tests/Bridge/B05_ExtractGateTests.vb:98`
(the exact expected summary string) when `warnings=N` joins the line (T035; analyze G2); `BridgeSqlGateTests`
(2) — vacuous once the registry folder is gone — and (4) (the archive task; both archived as text); every B01/B02/B04/B05/B08 fact that
names `projectId`, a registry field or a retired kind (the scope task; each revised in place with its Red recorded). `ProjectFileGateTests`,
`MemOsReferenceGateTests`, `FileHeaderGateTests`, `SchemaConstraintTests`, I02 and the 003 suites stay green. Any other Red is
unexpected and stops the work.

**Order** (CON2 as in 004): the amendment and the archive with the gate's Red (task 1); the standalone gate's Red and Green; the
schema-3 Reds (S03, F06, S02, US1) then Core's version 3 (the warnings repository waits for X02's Red — analyze C1); the X01 Red then `SlnxReader` and the loader's
door; the X02 Red then `AssetsLog` and the rule, the fixtures first; the B09 Red then the scope and configuration changes and the
`projectId` context; the B10 Red then the resolver, the suggestion, the request's path and the command line; the B11 Red then the
log reader and `map_status`; B06/B08 amendments; the descriptions, the sample config, the README; the full suite; the quickstart's
live steps on a copy; the live map only at the Architect's request.

## Operator steps (after implementation; outside this feature's code)

Exactly as [quickstart.md](quickstart.md) "Live steps" 1–7: backup; the config without `storePath`; the copy; the live re-points at the
Architect's request; the from-the-map checks with `memos.sqlite` renamed; then the Operator deletes `DSP_Processor.sln` in that
repository and re-points the two registry rows in MemOS; MemOS's `git status` recorded before and after.

## Known limits (recorded)

- **A project whose assets file is not at `obj/project.assets.json`** (a custom `BaseIntermediateOutputPath`) has its restore warnings
  unmatched: the Failure aborts with its text, as today (R62). None of the five live solutions does this.
- **A `.slnx` `<Configurations>` section is not read** (R61): every project builds under the run's `Configuration`; the same is true of
  the `.sln` path through the same global properties, so parity holds by construction.
- **The suggestion inspects the given directory only** (Q4): a build run from a subdirectory of an unmapped repository is answered with
  placeholders; the hook usually hands the solution file's directory, so the usual case holds the file.
- **`map_status` lists a `not_in_map` directory for as long as the log holds it and the map does not** (Q1 as ruled): a directory the
  Architect decides never to map stays listed; the remedy is the Architect's (map it, or accept the line). The log is never edited by
  the bridge.
- **The tree-wide dirty check, the per-process extraction guard, the hook firing for Claude Code's builds only, two sessions racing** —
  004's limits, unchanged.
- **The MemOS Shell's reader and a version-3 map**: not this feature's concern (Q2 as ruled); not carried here.

## STOP 1 — RULED 2026-09-17 (Architect): all ten as proposed

Ruled after `/speckit-analyze`'s nineteen findings were accepted and applied; the wording below is what was ruled, with the analyze
amendments (C1: the warnings repository lands behind X02's Red; C2: retired facts archived as `.vb` fragments) folded in.

1. **Constitution v1.4.0** with R70's wording verbatim, as the feature's first task; the archive move and the connection-gate amendment
   in that task, after the gate's Red.
2. **`.slnx` route (b)** as proven (R61): the loader's own parse, `OpenProjectAsync` per project in document order, no package moves.
3. **The warning rule through NuGet's assets file** (R62): match by message, `level` decides, no `NoWarn` anywhere; a custom `obj/` path is
   a recorded limit.
4. **Schema version 3** as drafted (R63, contracts/extractor.md §5): one table, one index, no trigger; the `DuringUpgrade` seam reused;
   the named Reds in 002's tests.
5. **`projectId` refused through the SDK's request context** (R68); the fallback named; the first Red decides.
6. **The fixture package**: a 3.6 KB hand-packed `CodeMem.NetFxOnly.1.0.0.nupkg` committed under `Fixtures/Warnings/packages/` with its
   source beside it (R71).
7. **The archive layout** `_Archive/004-store/` with a README carrying the seven retired refusal texts and the two archived gate facts (R69).
8. **`not_in_map` from the whole log**, one entry per normalised directory, old kinds ignored, an unreadable log reported (R64).
9. **The tool's `solutionPath`** as the surface of `--solution` (R67); the log's `solutionKey=<key> solution=<path>` form.
10. **The live sequence** (R73): backup; the `VersionBelow` line recorded first; the copy; the two re-points on the live map at the
    Architect's request; the from-the-map checks with `memos.sqlite` renamed; the Operator's two steps outside this repository.

## Complexity Tracking

No Constitution Check violation to justify. The one two-call-site module (`SolutionFileSuggestion`) is recorded under Article XI's row
as an Article XII consequence, not an abstraction.

## Phase 0 / Phase 1 outputs

- [research.md](research.md) — R60–R73, every unknown of the Technical Context resolved; the harness runs recorded.
- [data-model.md](data-model.md) — eleven sections of deltas.
- [contracts/tools.md](contracts/tools.md), [contracts/cli-config-hook.md](contracts/cli-config-hook.md),
  [contracts/extractor.md](contracts/extractor.md).
- [quickstart.md](quickstart.md) — fixture validation, the live sequence, the record table.

# Implementation Plan: CodeMem 004 — The Bridge: Claude Code's Source for the Map, and the Green-Compile Trigger

**Branch**: `004-codemem-bridge` | **Date**: 2026-09-15 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/004-codemem-bridge/spec.md` (clarified 2026-09-15: Q1, Q3, Q4, Q6
ruled; FR-351 added). **Spike**: [spike.md](spike.md), run before this plan as the description required; both
facts proven, two mechanisms corrected (research R41–R42).

**Governing document**: `.specify/memory/constitution.md` **v1.3.0** — amended at STOP 1 (2026-09-15) with research
R55's wording: Article IX gains the ruled exception for the registry read; the Review Gates gain the
connection-site gate at two named files. Store code may proceed.

**Ceremony**: STANDARD, one spike (done). **One stop, after this plan, before tasks — RULED 2026-09-15** (§STOP 1):
all eleven decisions as proposed; one note accepted, not ruled (R46's dirty definition).

## Summary

Two projects in one runtime process (CON1): `CodeMem.Bridge`, a console holding the entry point, the command line
and the MCP host wiring, and `CodeMem.Bridging`, the class library holding everything else — a stdio MCP server
(`serve`, the default entry) with eight tools, plus two more entries of the same executable — `extract` (a human's one-shot) and `hook` (what Claude Code's
PostToolUse hook runs, reading the event JSON from stdin) — all reaching one extract door with a declared origin.
It reads the map through `MapDatabase.OpenReadOnly` and the registry through a new `StoreDatabase.OpenReadOnly`,
both in Core, both per call; every query is a named Core repository method; the schema pin is the bridge's own
constant 2. Five readers are written fresh against the 056/058/060 shapes; `type_usages` and `map_status` are new
query compositions; `extract` launches `CodeMem.Extractor.dll` (placed beside the bridge by a project reference)
with `--solution-key` from the registry and `--solution` from the map, under two gates read per call from
`bridge.config.json`, and appends one line to `extract.log`. Ships `.mcp.json` (repo-relative command), a
settings fragment, a sample config and the process document. No schema change; no MemOS file touched.

## Technical Context

**Language/Version**: VB.NET on .NET 8 (`net8.0`), Option Strict/Explicit On, Infer Off, `GenerateDocumentationFile`
in both new projects; `<Version>0.1.0</Version>`.

**Primary Dependencies**: `ModelContextProtocol.Core` 1.4.0 (new; the package MemOS's own server uses; verified
from VB by the spike, R43); `LibGit2Sharp` 0.32.0 (direct reference in the bridge, the version Extraction pins,
R46); `Microsoft.Data.Sqlite` 8.0.31 (through Core). Project references: `CodeMem.Bridging` → `CodeMem.Core` and `CodeMem.Extraction` (for `SolutionScope`, the one
containment rule, COR1); `CodeMem.Bridge` → `CodeMem.Bridging` and `CodeMem.Extractor` (R47: places the extractor
beside the bridge). All CodeMem-internal; permitted. **No reference of any kind to anything under `rchaudio-a11y\MemOS`** (FR-302; the new gate).

**Storage**: the map, read-only (`Mode=ReadOnly`, `Default Timeout=3`, no journal pragma); the store, read-only,
`code_map_solutions` only; one append-only text log. Schema version 2, unchanged; no DDL, no migration.

**Testing**: xUnit, real SQLite, real compiled fixture, no mocks (Article III). New folder
`tests/CodeMem.Tests/Bridge/` (B01–B08) plus two guards and seventeen support classes (I7); the test project references both bridge
projects. Production-route facts spawn the real executable over stdio (Article XIII). One seam,
`IExtractorLauncher`, so refusals can assert zero launches (R54). Live facts Skip-armed by `CODEMEM_LIVE_MAP` and
`CODEMEM_LIVE_STORE`.

**Target Platform**: Windows 11 developer workstation; Claude Code 2.1.272 (VS Code extension binary).

**Project Type**: CLI/stdio server over class libraries. **New projects**: two — an executable that only wires and
its library (CON1). Justification (Article I, restored at analyze pass 2, C1): an executable may hold nothing but
wiring, so the readers, status, scope, the twin fold, configuration, refusals and the extract door need a library
of their own, and a library cannot carry the entry point; hence two projects, the Extractor/Extraction shape.

**Performance Goals**: SC-301 — each read tool under 3 s on the 64 MB live map (its own work); `map_status` with
three repositories under 3 s; the hook entry's own cost under 100 ms (17 ms in the spike) so the extraction is the
whole wait.

**Constraints**: Article IX as amended (read the store; write neither file); Article XI (SQL in Core repositories —
neither bridge project holds a SQL literal); Article XII (one door for the map open, one for the store, one for the extract
verb, one for the twin fold, one for the prefix rule); Article XV (no foreign runtime — the hook command is the
bridge executable itself, no script); nothing writes to stdout in `serve` except the transport.

**Scale/Scope**: three registered solutions (14 713 + 2 170 + 725 active symbols; 172 358 MemOS edges); eight tools;
one hook; one log.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Article / Gate | Status | How this plan satisfies it |
|----------------|--------|----------------------------|
| I. Library-First | PASS | `CodeMem.Bridge` is an executable that only wires — `Program.vb`, `BridgeCommandLine.vb`, `Mcp/BridgeServer.vb` — and `CodeMem.Bridging` is the class library holding readers, status, scope, the twin fold, configuration, refusals and the extract door (CON1, the Extractor/Extraction shape; the justification Article I requires is under Technical Context, Project Type). One class per file; every SQL statement lives in Core. |
| II. Test-First | PASS | Every bridge fact is Red-first (no bridge exists: every fact is red until its slice lands); the two gate amendments and the read-only door carry their fire demonstrations; the description-phrase fact and the hash contract are Red on an empty tool set (§Test design). |
| III. Integration-First | PASS | Real SQLite (fixture maps, a version-1 map, a foreign file, a throwaway registry store), the real compiled fixture, a real repository created by LibGit2Sharp, the real executable over stdio, the real extractor for one launch. |
| IV. Compiler Fact Only | PASS | The bridge derives nothing: every row it returns is a map row; `map_status`'s facts are the repository's answers; verdict names are computed from those facts by a stated order (R52), never from a guess. |
| V. Green Only, Stamped | PASS | Unchanged; the bridge reads the stamp (`commit_sha`, `is_dirty`) to derive currency and stores no "stale" (FR-324). |
| VI. Reconcile, Never Truncate | PASS | No write path exists in the bridge; the tripwire scans Core's repositories, where only SELECTs are added. |
| VII. Evidence on Every Row | PASS | Every occurrence returned carries path, line, column and its source and target identities (FR-348's fact). |
| VIII. Counts That Reconcile | PASS | `extract` reads the ten counts back from the published row, never from the summary line alone; residuals are reported as recorded. |
| IX. One File, Many Solutions, One Writer | PASS under v1.3.0 (ruled at STOP 1) | The extractor stays the sole writer and the bridge opens the map read-only (FR-303, FR-342's fire). The registry read is the ruled exception now written into Article IX (research R55's wording, adopted 2026-09-15); the store is opened read-only, for `code_map_solutions` alone, through one door. |
| X. Absence Must Be Representable | PASS | Every nullable fact is `null` on the wire, never a sentinel; `behindBy` is null when no count can be claimed (`diverged`); `latestRun` null when no run. |
| XI. Anti-Abstraction | PASS | Microsoft.Data.Sqlite direct; all SQL in named Core repository methods (R45); one canonical model per concept (the envelope records); the two seams (`IExtractorLauncher`, `ReadSeams` — I7) exist for Article XIII's zero-launch assertion and the read-only straddle fact, not for anticipated call sites. |
| XII. One Door for Every Rule | PASS | Map open + pin: `MapAccess`; store open: `StoreAccess`; scope: `ScopeResolver`; twin fold: `TwinFolder`; prefix: `SolutionScope.Contains` (003's door, reused); extract: `ExtractDoor` for all three origins; refusal wording: `BridgeRefusal`; config: `BridgeConfigFile.Load`. |
| XIII. Production-Route Reachability | PASS | `BridgeProcess` spawns `CodeMem.Bridge.exe serve` and calls every tool once through JSON-RPC; the `hook` entry is driven with the spike's payload through the executable; one real extractor launch through the door. |
| XIV. Archive, Never Delete | PASS | Nothing retired; no file archived. |
| XV. Compiled .NET, No Foreign Runtime | PASS | The hook command is the bridge executable; no script; the spike's Python probe is scratch, replaced by the VB `BridgeProcess`. |
| Gate: Option settings in every project file | PASS | `CodeMem.Bridge.vbproj` carries the four settings; `ProjectFileGateTests` covers it (its "at least four" becomes six files scanned — I7). |
| Gate: no SQL outside a named repository method | PASS | Bridge project: zero SQL literals (new `BridgeSqlGateTests` asserts it); Core gains read methods only. |
| Gate: `New SqliteConnection` in exactly two named files (v1.3.0) | PASS (ruled at STOP 1) | `MapDatabase.vb` and `StoreDatabase.vb`; the test is amended in the task that lands `StoreDatabase.vb`, after its Red is recorded, and re-fired. |
| Gate: header block + XML docs | PASS | `FileHeaderGateTests` scans `src/` and `tests/` recursively; every new file complies. |
| Gate: tripwire (I13) | PASS | Core repositories gain SELECTs; the registry module lives in `Repositories/Registry/` (not scanned by the top-level tripwire, but scanned by the new bridge SQL gate for SELECT-only). |
| Gate: every guard fires; residuals block; candidates never applied | PASS / unchanged | New guards: the hash contract, the read-only fire, the MemOS-reference gate, the SQL gate, the launcher-count facts — each with a FIRE line. |
| Gate: three call sites for a new abstraction | PASS | `IExtractorLauncher` has two implementations (process, scripted) and one caller; it is a test seam of the `RunSeams` shape, not a Rule-of-Three abstraction. |

**Post-design re-check**: unchanged. The two rows that failed under v1.2.1 pass under v1.3.0; the Complexity
Tracking entries stay as the record of why the amendment was needed.

## Project Structure

### Documentation (this feature)

```text
specs/004-codemem-bridge/
├── spec.md                     # clarified 2026-09-15
├── spike.md                    # the two facts, verbatim payloads, timings
├── plan.md                     # this file
├── research.md                 # R41–R56
├── data-model.md               # what is read, the records, verdicts, refusal kinds
├── contracts/
│   ├── tools.md                # the eight tools: parameters, descriptions, shapes, refusal texts, order
│   └── cli-config-hook.md      # the three entries, bridge.config.json, .mcp.json, the fragment, extract.log, the README's sections
├── quickstart.md               # fixture validation, live steps, the record table
├── checklists/requirements.md
└── tasks.md                    # /speckit-tasks output — after STOP 1
```

### Source Code (repository root) — files that appear or change

```text
.mcp.json                                        # NEW: server "codemem", stdio, repo-relative command (spike 1d)
src/CodeMem.Bridge/                              # NEW project (Exe, net8.0): the entry point, the command line, the MCP host wiring — nothing else (CON1)
├── CodeMem.Bridge.vbproj                        # refs Bridging, Extractor (the extractor lands beside the exe); no package of its own
├── Program.vb                                   # wiring: entry → BridgeServer | ExtractDoor | HookEntry
├── BridgeCommandLine.vb                         # serve | extract (--solution-key | --repo-path | --stale) [--on-green-build] | hook; --config
├── Mcp/BridgeServer.vb                          # McpServer.Create + StdioServerTransport("codemem"); registers BridgeTools' delegates
├── bridge.config.sample.json
├── hooks/settings.fragment.json
└── README.md                                    # the process document (cli-config-hook.md §6)
src/CodeMem.Bridging/                            # NEW class library (net8.0): everything else of the bridge (CON1)
├── CodeMem.Bridging.vbproj                      # refs Core, Extraction (SolutionScope); packages ModelContextProtocol.Core 1.4.0, LibGit2Sharp 0.32.0
├── Configuration/BridgeConfig.vb                # the typed record
├── Configuration/BridgeConfigFile.vb            # Load(path): per call; Unconfigured(key)
├── Refusals/BridgeRefusalKind.vb                # the closed enum (data-model §10)
├── Refusals/BridgeRefusal.vb                    # For(kind, facts): the one wording owner
├── Reading/BridgeSchemaPin.vb                   # Const Required As Integer = 2 — the bridge's own constant (FR-305)
├── Reading/MapAccess.vb                         # OpenRead per call: OpenReadOnly, InspectSchema, pin, BeginRead — one open, one read transaction (CON4)
├── Reading/StoreAccess.vb                       # OpenReadOnly per call; "no such table" → RegistryAbsent (INC1)
├── Reading/ScopeResolver.vb                     # projectId | solutionKey → Scope (058 §3.1), on the call's open map
├── Reading/TwinFolder.vb                        # the presentation rule, in memory over raw rows (COR3)
├── Reading/Readers/SolutionsReader.vb           # 056 shape; latest run of any outcome (INC4)
├── Reading/Readers/SymbolSearchReader.vb        # 058 §3.2 + compiledInto
├── Reading/Readers/SymbolDetailReader.vb        # 058 §3.3 + compiledInto
├── Reading/Readers/ReferencesReader.vb          # 058 §3.4
├── Reading/Readers/OrphansReader.vb             # 060 §3.1
├── Reading/Readers/TypeUsagesReader.vb          # tools.md §3.6; edges deduplicated by id (DUP1)
├── Reading/Envelopes/*.vb                       # one record per shape (Scope, SymbolRef, Declaration, Occurrence, …), camelCase, nulls serialised
├── Status/RepositoryFacts.vb                    # LibGit2Sharp: discover, head, dirty, ancestor, count (R46)
├── Status/MapStatusReader.vb                    # entries + verdicts (R52); the latest completed run (INC4)
├── Extract/ExtractOrigin.vb                     # Tool | GreenBuild | Manual
├── Extract/ExtractRequest.vb, ExtractResult.vb, StaleResult.vb
├── Extract/TargetResolver.vb                    # key | path | stale → bound solution(s); ContainsDirectory (COR1); PathNotRegistered, AmbiguousRoot, KeyUnbound …
├── Extract/ExtractGates.vb                      # enabled / onGreenBuild by origin
├── Extract/IExtractorLauncher.vb                # the seam
├── Extract/ProcessExtractorLauncher.vb          # dotnet <dll> …; 540 s budget (TIM1); stdout/stderr captured
├── Extract/ExtractDoor.vb                       # cardinality → config → gate → resolve → launch → read counts → log → result; SemaphoreSlim(1)
├── Extract/ExtractLog.vb                        # extract.log beside the executable (FR-351, INC3)
├── Extract/HookRequest.vb                       # PostToolUse JSON → repoPath; positional targets after options (COR2)
├── Extract/HookEntry.vb                         # stdin → door (green-build) → one JSON line; exit 0 always
├── Mcp/ReadSeams.vb                             # test seam: AfterScopeResolved (CON4's straddle fact)
├── Mcp/BridgeToolDescriptions.vb                # the eight descriptions (tools.md §2)
└── Mcp/BridgeTools.vb                           # the eight tool methods; returns JSON string or CallToolResult(IsError)
src/CodeMem.Core/Repositories/
├── MapDatabase.vb                               # + ReadOnlyConnectionString, OpenReadOnly, BeginRead, EndRead (R44, R58)
├── StoreDatabase.vb                             # NEW: the store's read-only connection — second New SqliteConnection site (gate amended)
├── Registry/CodeMapSolutionsRepository.vb       # NEW: ReadAll(store) — the only store SQL; "no such table" → RegistryTableMissingException (INC1)
├── Registry/RegistryTableMissingException.vb    # NEW
src/CodeMem.Extraction/Workspace/SolutionScope.vb # + ContainsDirectory (COR1; additive, the extractor never calls it)
├── SolutionsRepository.vb                       # + ReadAll, ReadById, ReadByKey
├── ExtractRunsRepository.vb                     # + ReadLatest, ReadLatestCompleted, ReadById
├── CodeSymbolsRepository.vb                     # + Search (grouped, R50), ReadById, ReadTwins, ReadMembers, ReadProjects, ReadOrphans
├── CodeEdgesRepository.vb                       # + ReadOutbound, ReadInbound, ReadReferences, ReadTypeUsages (R51)
├── CodePartsRepository.vb                       # + ReadParts
└── MapIdentityRepository.vb                     # + ReadIdentity
src/CodeMem.Core/Records/                        # + the read rows the repositories return (RunRecord, SymbolRecord, EdgeRecord, PartRecord, RegistryRecord, GroupedSymbolRecord)
tests/CodeMem.Tests/
├── CodeMem.Tests.vbproj                         # + ProjectReferences CodeMem.Bridging, CodeMem.Bridge; Fixtures/Hooks/*.json copied to output
├── Bridge/B01_ReadOnlyContractTests.vb          # hash after every tool; Mode=ReadOnly fire; no journal
├── Bridge/B02_PortedReaderTests.vb              # shapes, refusals, order; description phrases (FR-312)
├── Bridge/B03_TypeUsagesTests.vb
├── Bridge/B04_MapStatusTests.vb                 # the six states on a repository fixture; unbound/inactive listing
├── Bridge/B05_ExtractGateTests.vb               # gate matrix; resolution; ExtractionRunning; one real launch; log lines
├── Bridge/B06_HookEntryTests.vb                 # the executable's hook entry with the spike payloads
├── Bridge/B07_TwinPresentationTests.vb
├── Bridge/B08_LiveMapTests.vb                   # Skip-armed: 3023, twins, map_status behind
├── Guards/MemOsReferenceGateTests.vb            # NEW (FR-343)
├── Guards/BridgeSqlGateTests.vb                 # NEW (FR-303/304)
├── Guards/SqlLocationGateTests.vb               # amended: two connection sites (after STOP 1)
├── Support/RegistryFixture.vb                   # throwaway store, 029 §1 DDL transcribed, seeded rows
├── Support/BridgeProcess.vb                     # spawns the exe; JSON-RPC over stdio; the hook entry with stdin
├── Support/BridgeHost.vb                        # in-process door with a config record, a launcher and ReadSeams (fast facts, never the production route)
├── Support/OccurrenceAssertions.vb              # AssertIdentity: id or external with a doc id, never neither (FR-348)
├── Fixpack003/R01_ScopeRootTests.vb             # + ContainsDirectory fact (COR1; in place, dated)
├── Support/GitFixture.vb                        # Repository.Init / Stage / Commit / branch on a FixtureCopy (R39)
├── Support/ScriptedLauncher.vb                  # IExtractorLauncher that records and returns scripted output
└── Fixtures/Hooks/posttooluse-green.json, posttoolusefailure-red.json   # the spike's payloads, verbatim
```

**Structure Decision**: four source projects become six (CON1). The executable only wires; the library owns
presentation, status and the extract door; Core owns every SQL statement and both connection doors, as the
constitution's gates require; the extractor is reached as a child process and, for one rule (`SolutionScope`,
which gains the additive `ContainsDirectory`, COR1), as a referenced assembly.

## Design

### The door and the three entries (R48)

`ExtractDoor.Run(request As ExtractRequest) As ExtractResult` (or `StaleResult`): validate cardinality (INC2) →
load config → gate (enabled; then onGreenBuild for `GreenBuild`) → `TargetResolver` (directory containment that
includes the root, COR1) → end the read and dispose → take the semaphore (else `ExtractionRunning`) → launcher
(540 s budget, TIM1) → parse the line or report the timeout → read the run row → release → `ExtractLog.Append`
beside the executable, one line per solution launched (INC3) → return. `BridgeTools.Extract` (origin Tool),
`Program` for `extract …` (Manual, or GreenBuild with `--on-green-build`) and `Program` for `hook` (HookRequest →
GreenBuild) are the three callers. The MCP tool never carries an origin argument.

### Reading (R44, R45, R50, R51)

Every tool (CON4, INC2): argument refusals → `BridgeConfigFile.Load` → `MapAccess.OpenRead` (read-only, pin, one
read transaction) → `ScopeResolver` on that map (the store is opened only for `projectId`) → reader → envelope →
JSON string → `EndRead`. One open and one read transaction per call, ended after serialisation, so a publication
cannot straddle a tool's reads (R58). The twin fold is applied in memory over raw rows by `SymbolSearchReader` and
`SymbolDetailReader` through `TwinFolder` only (COR3).

### Status (R46, R52)

`MapStatusReader.Read(config)`: registry rows → per bound row the map's solution and latest completed run →
`RepositoryFacts.Read(repoRoot, recordedSha)` → verdict. `RepositoryFacts` is the one place LibGit2Sharp is called
in the bridge.

### JSON

`System.Text.Json` with camelCase naming, `DefaultIgnoreCondition = Never`, `WriteIndented = False`; one options
instance in `Envelopes/BridgeJson.vb`. Refusals: `CallToolResult` with `IsError = True` and one `TextContentBlock`.

## Test design

| File | Facts (Red-first; every guard with a FIRE line) | Red before code | FIRE after Green |
|------|------------------------------------------------|-----------------|------------------|
| `B01_ReadOnlyContractTests` | hash equal after each of the seven read tools **through the executable over stdio** (CON3); no `-journal`; the descriptions `tools/list` returns equal the constants (G4); `MapDatabase.OpenReadOnly` + INSERT → `SqliteException` code 8; `OpenReadOnly` of a missing path → code 14 and the file still absent; **a publication cannot straddle a tool's reads** (CON4): a rename attempted mid-call gets busy (5), the response carries the old name, the rename succeeds afterwards | no bridge, no `OpenReadOnly` | change the connection string to `ReadWrite` → the INSERT succeeds → red; end the read transaction before the reader → the straddle fact red; revert |
| `B02_PortedReaderTests` | 056/058/060 shapes on the fixture map (every property present, nulls included); the refusal chain in order (absent, foreign, v1, v3, registry absent, scope missing/conflict, filter missing, kind unknown, key unknown, symbol not found/out of scope/retired, kind not examined, not a project row); FR-348 over every occurrence; the FR-312 phrases in the eight descriptions | no tools | drop one phrase from a description → red; revert |
| `B03_TypeUsagesTests` | fixture type: constructor call from the other project, member call from a sibling (`fromInside`), an `Implements`; `byVerb` seven keys; `total`/`fromOutside`; `NotAType` for a method | no tool | count inside uses as outside → `fromOutside` red; revert |
| `B04_MapStatusTests` | the six states on a `GitFixture` (current, behind 1 with HEAD named, dirty, diverged with both shas and null count, no_git, map_missing_solution); unbound/inactive listing; no guess: a root that is a subdirectory of a repository → `no_git` with the reason | no tool | swap the verdict order (behind before dirty) → the dirty-and-behind fact red; revert |
| `B05_ExtractGateTests` | the five-cell gate matrix by gate name, each with 0 launches and one log line; `PathNotRegistered` (0 launches, hash unchanged), `AmbiguousRoot`, `KeyNotRegistered`, `KeyUnbound`, `KeyInactive`, `MapMissingSolution`; `ExtractionRunning` with a sleeping scripted launcher; one real launch and one gate refusal **through the executable** (CON3); the root itself resolves (COR1); a scripted timeout is reported, never a success (TIM1); cardinality before configuration (INC2); `stale` considers three, extracts two, one log line each and no header (INC3) | no door | evaluate onGreenBuild before enabled → "onGreenBuild only, hook" cell red; hold the read transaction across the launch → the real-launch fact red (G2); revert |
| `B06_HookEntryTests` | `CodeMem.Bridge.exe hook` with the green payload → exit 0, one JSON line, `additionalContext` names the resolved key or the refusal, one log line; the failure payload → "not a Bash PostToolUse", no log line, 0 launches; `cd X && dotnet test Y.vbproj` → Y's directory; options before the path, a quoted path and a cross-repository path each name the target; two candidates → ambiguous, never a fallback (COR2); a named path under no root → refused naming it, `cwd` never tried (Q6, G1); `interrupted: true` → nothing | no entry | accept `PostToolUseFailure` → red; retry with `cwd` on `PathNotRegistered` → red (G1); revert |
| `B07_TwinPresentationTests` | a fixture copy with one file linked into both projects → one declaration, `compiledInto` 2, `total` 1; detail of either id names both; two overloads on one line under one project stay two declarations (COR3); a partial type's parts remain one symbol | no fold | drop the all-distinct check → the same-project fact red; revert |
| `B08_LiveMapTests` (SkippableFact) | `type_usages(3023)` ≥ 16 constructor calls and `references(3023)` = 0; twins 3556/4200; `map_status` MemOS `behind` with HEAD named | Skipped unarmed | — (live evidence, recorded in the quickstart) |
| `R01_ScopeRootTests` (amended, COR1) | `ContainsDirectory`: the root itself (with and without the trailing separator), a subdirectory, mixed case and forward slashes → True; the parent and a same-prefix sibling → False; `Contains` unchanged | red: no such method | prefix-only (drop equality) → the root case red; revert |
| `MemOsReferenceGateTests` | ≥ 1 `ProjectReference` found (vacuous guard); 0 `Include=` paths containing `MemOS` or `rchaudio-a11y` in ProjectReference/PackageReference/Compile | passes before code — recorded as a guard, fired | add `<ProjectReference Include="..\..\..\rchaudio-a11y\MemOS\MemOS.Core\MemOS.Core.vbproj" />` to a scratch copy of the bridge project → red; revert |
| `BridgeSqlGateTests` | 0 SQL literals in `src/CodeMem.Bridge`; every literal in the new Core read methods begins with SELECT/WITH (≥ 1 found first); the registry module's literals name `code_map_solutions` and no other `FROM`/`JOIN` target | passes on an empty bridge — vacuous guard first | add `"SELECT 1"` to `BridgeTools.vb` → red; add `FROM projects` to the registry module → red; revert |
| `SqlLocationGateTests` (amended, after STOP 1) | exactly `{MapDatabase.vb, StoreDatabase.vb}` | red the moment `StoreDatabase.vb` lands (the named Red) | add `New SqliteConnection` to `StoreAccess.vb` → red; revert |

**Expected Reds in existing tests, named before they happen**: `SqlLocationGateTests.OnlyMapDatabaseOpensAConnection`
goes red when `StoreDatabase.vb` appears — the amendment is applied in the same task, after the Red is recorded.
`ProjectFileGateTests` stays green (it scans every `.vbproj`; the new one complies). No other existing test pins
anything this feature changes. Any other Red is unexpected and stops the work.

**Order** (CON2): gates and support; the B01 Red, then the skeleton its facts name, the doors and the server; the B02 Red, then the
shared reads, the foundation and the five readers; `type_usages`; `map_status`; the `ContainsDirectory` Red and its
method; the B05, B06 and R01 Reds, then the seam and skeletons, the extract door with the scripted launcher, then the real launcher; the `hook` entry; the twin fold; the
shipped files and the README; the full suite; the quickstart's live steps read-only; the live extraction only at
the Architect's request. No slice before its Red.

## Operator steps (after implementation; the PM acceptance)

Exactly as [quickstart.md](quickstart.md) "Live steps": register and approve; the read-only session with the
side-by-side table against the Shell; the three findings; `map_status`; the gate refusal and the extraction on the
copy; the hook; the live MemOS run 7 only at the Architect's request, after a backup. `memos.sqlite` and the MemOS
tree hashed and `git status`ed before and after.

## Known limits (recorded for the bridge)

- **A never-published solution is not extractable through the bridge** (Q1 as ruled): the first run stays a hand
  run with an explicit `--solution-key`, then MemOS binds the row.
- **`map_status`'s dirty check is tree-wide**, broader than the run's `is_dirty` over compiled inputs (R46).
- **The hook fires for Claude Code's builds only**; a Visual Studio build refreshes nothing until the MSBuild
  target follow-on (152658 ruling 2).
- **Two sessions can race**: each has its own bridge; the extractor's lock (exit 3) is the arbiter and is returned
  verbatim.
- **The registry's `extraction_scope` is not read**; a row whose scope disagrees with the map's `last_seen_path` is
  extracted at the map's path (the last thing the extractor was pointed at).
- **A read during the bridge's own extraction is not tested** (analyze G8, accepted 2026-09-15): it is
  timing-dependent — the reader sees the previous completed run or refuses `Busy` during the publication instant
  (R53) — and is recorded here rather than asserted.
- **The one-extraction-at-a-time guard is per process** (FR-332 as scoped at analyze pass 2, I3, ruled 2026-09-15):
  the `hook` entry is a process of its own beside the `serve` session, so a hook-origin extract and a tool or
  manual extract can overlap; the extractor's lock (exit 3) is the cross-process arbiter and is returned verbatim.
  Accepted for this feature.

## STOP 1 — RULED 2026-09-15 (Architect)

**Rulings** (recorded in the spec, Clarifications, *Session 2026-09-15 — STOP 1 rulings*): **all eleven as
proposed.** (1) Article IX amended, v1.3.0, R55's wording as written; the connection-site gate to exactly two named
files with its fire; store code may proceed once the amendment is in — applied the same day. (2) Gates before
resolution. (3) The fragment as proposed; the README records that the `mcp_tool` hook type produced no observable
effect in the spike and is not retried. (4–11) As proposed. **Accepted, not ruled**: R46's tree-wide dirty check
differs from the run's `is_dirty`; accepted for this feature as stated; narrowing to source files stays a later
ruling and is not built now. Proceed: tasks, Red-first, the suite, the quickstart's live steps read-only, the live
extraction only at the Architect's request.

The decisions as they were put (each was one line to reverse). Items 1–3 are the ones the description asked to be
seen (gate wiring, hook fragment) plus the constitution amendment; 4–11 are the spec's remaining proposals and
the plan's own additions.

| # | Decision | Where | Alternative |
|---|----------|-------|-------------|
| 1 | **Article IX amendment v1.3.0 and the connection-site gate become two files** (`MapDatabase.vb`, `StoreDatabase.vb`); wording in research R55 | R55; Constitution Check | keep Article IX as written and read the registry through the map's class or not at all — the latter makes `projectId` scope and `--solution-key` construction impossible, contradicting 137077/142362 |
| 2 | **Gate wiring**: `extract.enabled` checked first for every origin; `extract.onGreenBuild` only for origin green-build and only after the first passed; both read per call; the gate is evaluated **before** target resolution so a disabled verb reveals nothing about the registry | contracts/tools.md §4; `ExtractGates` | resolve first, gate second (a refusal would then name the key before the gate) |
| 3 | **The hook fragment**: `PostToolUse`, matcher `Bash`, one `command` hook = the bridge exe + `hook`, `timeout` 600, no `PostToolUseFailure`; the entry reads stdin and answers with `additionalContext`; exit 0 always | contracts/cli-config-hook.md §1, §4; spike | an `mcp_tool` hook (no observable effect in the spike, dropped); a script wrapper (a foreign runtime or a PowerShell file where the exe suffices) |
| 4 | **`.mcp.json` uses a repository-relative command** (verified, spike 1d); the user-scope line and the fragment carry the absolute path | R41; contracts §3 | absolute everywhere |
| 5 | **`--config <path>` on every entry** (default beside the exe) | R49 | beside the exe only (tests then share one file in the output directory) |
| 6 | **The bridge references `CodeMem.Extractor`** so the extractor lands beside it and `SolutionScope` is the prefix door; default `extractorPath` = `<bridge dir>\CodeMem.Extractor.dll` via `dotnet` | R47 | no reference, mandatory `extractorPath` |
| 7 | **LibGit2Sharp in the bridge**, tree-wide dirty check, root must equal the discovered working directory, unknown recorded commit → `diverged` | R46, R52 | the `git` executable; narrowing dirty to source files |
| 8 | **Twin fold in `symbol_detail` = header names the twins, parts and edges are the requested id's** (spec Q2) | R50 | union the twins' edges |
| 9 | **`type_usages` type kinds only, one containment level** (spec Q3's unruled halves) | R51 | any symbol kind; recursive members |
| 10 | **Registry-only vocabulary**: no `ProjectUnknown`, no audit row; `hadNothingToSearch` says "no registry row names project N" (spec Q7); `KeyUnbound`/`KeyInactive`/`MapMissingSolution` as named | data-model §2, §10 | — |
| 11 | **Shipped files and the README** at the paths in contracts §3–§6 (spec Q11); the live acceptance on a copy first, live only at the Architect's request (spec Q10) | contracts/cli-config-hook.md; quickstart | — |

## Review rulings 2026-09-15 (Architect, after `/speckit-analyze`)

Applied across the artifacts the same day; each is one line to find by its tag. **CON1** two projects, one process
(the Extractor/Extraction shape; Article I passes without a justification). **CON2** no slice before its Red; the
shared reads and the foundation move behind B02's Red. **CON3** every read tool through the executable over stdio
in B01, and `extract` the same way for one refusal and one launch. **CON4** one open, one read transaction per call,
ended after serialisation, with the commit-straddling fact. **COR1** `SolutionScope.ContainsDirectory`, equality or
prefix; `Contains` unchanged; exact-root facts in both places. **COR2** positional targets after options; more than
one candidate is ambiguous, never a fallback. **INC1** the registry queried directly, "no such table" translated.
**INC2** cardinality before configuration. **INC3** the log beside the executable; `--stale` one line per solution
launched, no header. **DUP1** dedup by edge id with its fact. **INC4** `solutions` latest of any outcome,
`map_status` latest completed. **COR3** raw rows preserved; fold only all-distinct projects; the same-project shape
seeded. **TIM1** 540 s child budget inside the 600 s hook. **Branch** `004-codemem-bridge` from `b2e0168`, the
amendment and the artifacts committed there first.

**Pass 2, 2026-09-15 (Architect, after the second `/speckit-analyze`; last pass before implement)** — recorded in
the spec's Clarifications under the same name. Applied: **I1** the `BridgeTools`/`ReadSeams`/`BridgeHost` skeleton
right after T010's Red (T011); **I6** T038–T040 before T037; **G1** the B06 no-fallback fact and its FIRE; **U1** the
eleven refusal texts in contracts/tools.md §6; **I2** B04 (7) on a temp copy with no repository; **I3** FR-332 per
process, the race under Known limits; **I4** the segment rule in the spec's edge case; **I5** the six-name verdict
order in FR-321; **G2** the FR-334 FIRE; **G3** three Stopwatch assertions; **G4** descriptions compared over
`tools/list`; **U2** one collection for B05 and B06; **U3** `spike-hook.log` in the fixtures; **C1** the Article I
justification above. Carried (LOW): I7–I12, D1, G5, U4, U5, A1 — fixed as each file is touched; listed at close-out.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Article IX, last sentence (CodeMem opens no database other than its map) — the bridge opens the MemOS store read-only for `code_map_solutions` | Decisions 137077, 142362 and 152658 rule that the bridge constructs `--solution-key` from the registry and refuses unregistered solutions; without the read there is no registry and no `projectId` scope | Passing keys by hand (procedure, not protection — 132094's own argument); copying the registry into the bridge's config (a second hand-maintained source, ruled out at clarify Q1) |
| Review Gate "New SqliteConnection in exactly one file" — becomes two | The store needs its own door so it is never mistaken for a map (`InspectSchema`, the pin, the repositories' parameter type are the map's) | Opening the store through `MapDatabase` (the class's name is a role promise; Article IX) |

## Implementation record (2026-09-16)

**Environment**: Windows 11 Home 10.0.26200; `dotnet --version` 10.0.401; runtime Microsoft.NETCore.App 8.0.31 runs
the net8.0 projects. Starting point commit `fcde454` on `004-codemem-bridge` (the analyze pass-2 artifact edits
and `Fixtures/Hooks/` uncommitted in the tree); 68 passed / 1 skipped / 69 total in 1 m 41 s, 0 warnings. Packages
added: ModelContextProtocol.Core 1.4.0 and LibGit2Sharp 0.32.0 on `CodeMem.Bridging`. Committed 2026-09-16 at the
Architect's direction: the implementation (the two projects, the Core reads, the tests, the shipped files) as
`fe59871`; the records (this section, the quickstart tables, the spec status, the process document) in the commit
that follows it, whose sha the close-out report names.

**Order followed**: tasks.md T001–T061 in order, with the two pass-2 reorderings honoured — the
BridgeTools/ReadSeams/BridgeHost skeleton landed immediately after T010's Red (I1), and T038–T040 ran before T037
(I6). Phase 1 T001–T009; Phase 2 T010–T016; US1 T017–T026; US2 T027–T031; US3 T032–T036; US4 T038, T039, T040,
T037, T041–T047; US5 T048–T051; Phase 8 T052–T061. Every test file was written and run before the code it names;
every Red was recorded in the file's header before the code landed; every FIRE was injected alone, run on its one
fact, and restored from a byte copy (`cmp` equal) before the task closed. The whole-suite lines are in
quickstart.md "Implementation record — build and test lines".

**Reds observed** (each in its file's header, dated):

- B01 (T010): the assembly did not compile — twelve errors naming the door and the skeleton; (T015) with the server
  wired and no tool registered, (4) vacuous and (6) `NotImplementedException("US1")`.
- BridgeSqlGate (T008): (2) vacuous — no `Registry/` folder yet; green at T013 when the registry read landed.
- B02 (T017): (1)–(7) on `NotImplementedException("US1")`; (8) green (the descriptions were constants since T014).
- B03 (T027): (1), (1b), (1c), (3), (4) on `NotImplementedException("US2")`; (5) on KeyNotFound (not registered);
  (2) green already (it pins `references`' existing narrowness, FR-318).
- B04 (T032): all ten — the class fixture's first `map_status` threw `NotImplementedException("US3")`.
- B05, B06 and R01 (f) (T038–T040): the battery's Red was the compile failure; after T037's skeleton and T041, B05
  17 red on `NotImplementedException("US4")` and (15) on KeyNotFound; B06 11 red (the hook branch of `Program.vb`
  threw, so every entry exited non-zero).
- B07 (T048): (1), (2), (3) against the pass-through folder — total 3 where 2 was asserted, `compiledInto` of 1
  where 2 was; (4) green as T048 foresaw; (5) green, not red (the fold sentence was verbatim since T014).
- B08 (T025): (3) red on the first armed run — 299 orphans, not 279 (below, "Live figures"); (T051) (7) red on its
  first armed run — total 2, not 1 (the contains filter also finds `CodeMemMapFixtureTests`).
- Two unexpected Reds, both diagnosed and both mine: S02 at T026 (the line-ending pass had rewritten
  `SchemaRepository.vb` as CRLF; restored to LF with a header note — the DDL constants' newlines are the bytes
  SQLite stores), and B02 (4) at T023 (my miscount of MainForm.vb's lines: 5, 13, 17).

**FIREs** (every one restored from a byte copy, green after): T007 MemOS ProjectReference inserted → red naming
the project file. T011 `Mode` changed to ReadWriteCreate → B01 (1), (2), (3) red. T012 a second `New
SqliteConnection` in `Program.vb` → SqlLocationGate red. T013 three fires on BridgeSqlGate (a `projects` literal in
the registry read; a `SELECT 1` in `BridgeCommandLine.vb`; a `StoreDatabase` probe in `ExtractionRun.vb`). T024 (a)
the `part_of` phrase removed from the references description → B02 (8); (b) `is_active` dropped from `Search` →
B02 (7); (c) `verb <> 'part_of'` dropped from `ReadReferences` → B02 (4); (d) `EndRead` before serialisation →
B01 (6). T030 (a) every occurrence counted outside → B03 (1); (b) UNION → UNION ALL on the implements/extends arm →
B03 (1c). T034 behind before dirty → B04 (3). T041 `ContainsDirectory` prefix-only → R01 (f). T046 (a)–(i) as
tasks.md names them, one red each: (a) B05 (3), (b) B05 (5), (c) B06 (2), (d) B05 (10), (e) B06 (3e), (f) B05
(12b), (g) B05 (7b), (h) B06 (3f) (G1), (i) B05 (13) — the real child exited 1 with "database error: SQLite Error
5: 'database is locked'" while the bridge held its read transaction across the launch (FR-334, G2). T050 the
all-distinct check dropped from the folder → B07 (4).

**Review pass (T053), v1.3.0 Review Gates, every new and changed file**:

- Option settings: `CodeMem.Bridge.vbproj` and `CodeMem.Bridging.vbproj` carry Strict/Explicit On, Infer Off,
  `Version` 0.1.0; `CodeMem.Extraction.vbproj` stays 0.2.0 (one additive method); `ProjectFileGateTests` green over
  six project files.
- SQL location: every statement the bridge issues lives in `Core/Repositories` (`Registry/` for the store);
  `SqlLocationGateTests` (two connection sites: `MapDatabase.vb`, `StoreDatabase.vb`) and `BridgeSqlGateTests`
  (no literal under the two bridge projects; the registry read names `code_map_solutions` only; Core's read methods
  SELECT-only; `StoreDatabase` referenced only under `Registry/`) green. The test project's SQL stays in
  `MapQueries` (one write helper added, `SetStartLine`, for B07's shape).
- Header block and XML docs: `FileHeaderGateTests` green over `src/CodeMem.Bridge/**`, `src/CodeMem.Bridging/**`
  and `tests/CodeMem.Tests/Bridge/**`; `MemOsReferenceGateTests` green (no reference into `rchaudio-a11y\MemOS`).
- Tripwire green; the MemOS working tree untouched (`git status --porcelain` empty before and after the armed run).
- FIRE lines present for T007, T008 (its fire ran at T013, when the read it guards existed), T011, T012, T013,
  T024, T030, T034, T041 (in R01's header, on the GREEN line), T046, T050.
- CON1: the executable holds `Program.vb`, `BridgeCommandLine.vb` and `Mcp/BridgeServer.vb` only, plus the sample
  config, the fragment and the process document.
- No new abstraction beyond `IExtractorLauncher` (the launcher seam) and `ReadSeams` (the read-only straddle seam);
  no schema change; no constitution amendment beyond v1.3.0 (STOP 1).

**One door per rule — the doors and their callers**:

| Rule | Door | Callers |
|---|---|---|
| read-only open, pin, busy translation | `MapAccess.OpenRead` / `Translate` | `BridgeTools.RunRead`, `ExtractDoor.Run` / `RunStale`, `LiveBridge` (test) |
| registry read (Article IX exception) | `StoreAccess.ReadRegistry` → `CodeMapSolutionsRepository` | `BridgeTools.RunRead`, `ExtractDoor` |
| configuration | `BridgeConfigFile.Load` | `BridgeTools.RunRead`, `ExtractDoor`, `LiveBridge` (test) |
| scope (projectId / solutionKey) | `ScopeResolver.ValidateArguments` / `Resolve` | `BridgeTools` |
| symbol identity (found, in scope, active) | `SymbolResolver.RequireActive` / `RequireProjectRow` | `SymbolDetailReader`, `ReferencesReader`, `TypeUsagesReader`, `SymbolSearchReader`, `OrphansReader` |
| the presentation rule (twins) | `TwinFolder.Fold` / `HeaderOf` / `FoldsTogether` | `SymbolSearchReader`, `SymbolDetailReader` |
| refusal texts | `BridgeRefusal.Named` | every door and reader |
| the two gates | `ExtractGates.Check` / `GateName` | `ExtractDoor.Run` / `RunStale` |
| target resolution (key or path) | `TargetResolver.Resolve` (containment: `SolutionScope.ContainsDirectory`) | `ExtractDoor` |
| the hook's payload | `HookRequest.Parse` | `HookEntry` |
| the extract door | `ExtractDoor.Run` / `RunStale` | `BridgeTools.Extract`, `Program` (extract), `HookEntry`, `RunStale` (per solution) |
| the child process | `IExtractorLauncher` → `ProcessExtractorLauncher.Launch` | `ExtractDoor` (constructed by `BridgeServer`, `Program`, `HookEntry`; `ScriptedLauncher` in tests) |
| the verdicts | `MapStatusReader.Read` / `EntryOf` (facts: `RepositoryFacts.Read`) | `BridgeTools.MapStatus`, `ExtractDoor.RunStale` |
| the log | `ExtractLog.Append` | `ExtractDoor` |
| JSON on the wire | `BridgeJson.Serialize` | `BridgeTools`, `Program`, `HookEntry` |

**Deviations from the design documents, with reasons**:

1. **`BridgeHost.Invoke`, not `Call`**: `Call` is a VB keyword. Likewise `partialType` and `overloadPair` in B07,
   `ContextOf`, `RunIdPattern`: VB's case-insensitive names clash with methods of the same spelling.
2. **`MapAccess` issues `BEGIN` before the schema inspection** (T023): a deferred `BEGIN` alone holds no lock in
   rollback-journal mode; the SHARED lock is taken by the first SELECT, so the pin's own read had to be inside the
   transaction for B01 (6)'s straddle (busy code 5 during the call) to hold. The first run recorded 0, not 5.
3. **B01 (5) compares the tool list as sorted sets**: the SDK lists tools in its own order, not registration
   order; FR-321's six-name order is asserted on `RegisteredToolNames`, not on `tools/list`.
4. **The hook's answer line carries the key before "refused"** when the target resolved but the launch was refused
   (`…: <key>: refused — …`, e.g. ExtractorNotFound) — the contract's two shapes plus the resolved key, so a reader
   sees what resolved.
5. **`SchemaRepository.vb` stays LF** (header note): the CRLF pass turned S02 red; its multi-line DDL constants are
   the bytes SQLite stores in `sqlite_master`, compared to the v1 fixture.
6. **`RegistryFixture` opens with `PRAGMA foreign_keys = OFF`**: the bundled SQLite enforces foreign keys by default
   and the 029 DDL references `projects`, which the fixture does not create.
7. **`RepositoryFacts.StatusScan` runs a tuned `StatusOptions`** (no ignored, no unaltered, no rename detection, no
   recursion into untracked directories, submodules excluded): the default scan of the three live trees took
   3067 ms and broke SC-301; tuned, `map_status` answers in ~155 ms. Recorded as a limit below.
8. **B05 (6) asserts the path against the parsed refusal text**, not the raw JSON, where backslashes are escaped.
9. **B06 (3d)'s `Seed` copies the fixture tree only when the target is absent**, so a second scenario in one host
   reuses it.
10. **T048's same-project shape is made by moving a row**: VB refuses two method declarations on one logical line
    (BC32009) and the extractor records raw line spans, so `TwinScenario` extracts two overloads on adjacent lines
    and sets the second row's `start_line` to the first's in the temp map (`MapQueries.SetStartLine`). The rows,
    their doc ids and their project are the extractor's own; the T050 fire reddened (4) as designed. The overloads
    are named `Overloaded`, not `M` — the name filter is a contains match and `M` matches every method with an m;
    `DefineConstants` takes the VB form `$(DefineConstants),TWIN_APP=True` (the semicolon form is a C# idiom vbc
    rejects, BC31030).
11. **B07 (1), (3) and B08 (7) assert total 2, not 1**: the contains filter also finds the fixture's `Twins` class
    and MemOS's `CodeMemMapFixtureTests`; the facts pick the declaration by exact name.
12. **A solution key resolves against the map without a registry row** (B07 (3) as first written expected a
    refusal): registration gates `extract` and the `projectId` scope, not reads by key — FR-310's letter; recorded
    here so nobody re-derives it.
13. **B08 (3): 299 orphans, not 279** — diagnosed read-only: 279 over the kinds 003 examined plus 20 rows (19
    properties, 1 field) that 060 FR-405 made examined. The fact asserts both figures. **Ruled 2026-09-16 (Architect): 299
    is accepted as the live figure and is the baseline** — 279 over 003's kinds plus the 20 rows 060's filter made
    examinable; the next change to that number is a Red to diagnose.
14. **The eleven refusal texts were transcribed into contracts/tools.md §6** (pass 2, U1) and `BridgeRefusal.Named`
    composes all thirty-two from there; FR-306's list now names all thirty-two (A1).
15. **`type_usages`' four arms are a UNION**: arm (d) (implements/extends) duplicates arm (a)'s edge rows, so the
    dedup is by edge id; UNION ALL is the T030 (b) fire.
16. **T046 (a)'s injection**: swapping the two gate lines alone does not redden (3) (with `onGreenBuild` on, the
    swapped check passes and `enabled` still refuses); the injection that expresses the fault is the hook's own gate
    deciding the green-build origin, which is what was fired and recorded.

**Analyze pass 2 LOW items carried to implementation** (spec Clarifications "review rulings pass 2"):

| Tag | Item | Status |
|---|---|---|
| I7 | the plan's stale counts after CON1/COR1 | fixed at close-out in this plan: six project files scanned, `ContainsDirectory`, seventeen support classes, two seams |
| I8 | the two live-arming variables beside `AcceptanceRunner`'s | fixed: quickstart "Build and test" names both; B08's header names all three |
| I9 | Q11's "placeholder" versus the fragment's absolute path | fixed in spec Q11: the fragment ships the absolute path verbatim |
| I10 | the origin token `green-build` versus `green_build` | fixed in FR-331: the prose word and the wire token named together |
| I11 | FR-307 and `--config` | fixed in FR-307: beside the executable, or the file `--config` names |
| I12 | FR-323's counts versus the data model's lists | fixed in FR-323: the bound count and the two lists, as the envelope carries them |
| D1 | FR-303's clause restating FR-334 | fixed in FR-303: the extraction case points to FR-334 |
| G5 | zero launches and the hash in B05 (9) | fixed: B05 (9) asserts `Requests.Count` 0 and the hash equal |
| U4 | the SQL-literal scan's case sensitivity over the two prose files | fixed: `BridgeSqlGateTests` scans case-sensitively and drops comment lines |
| U5 | the subdirectory-root edge case in the spec | fixed: spec edge case "Two registered roots nest" |
| A1 | FR-306's 29 kinds versus the data model's 32 | fixed in FR-306: the three missing kinds named, thirty-two in all |

None remains open.

**Known limits carried** (the list above stands, G8 among them) **and two added by implementation**:

- **`map_status`'s dirty check counts tracked changes and top-level untracked entries**: the tuned status scan
  does not recurse into untracked directories (a new directory shows as one untracked entry) and skips ignored
  files; a file inside a new directory is seen as that directory. The verdict is still `dirty`; only the reason's
  count differs from `git status`'s file count.
- **The same-project twin shape cannot arise from VB source** (BC32009), so the folder's all-distinct rule is
  exercised by a moved row in a temp map, never by a real extraction; the rule stays because the contract states it
  (COR3) and a foreign or future producer could write such rows.

17. **B08 runs in a non-parallel collection** (`LiveMap`, `DisableParallelization`): the first armed whole-suite
    run (T054) measured `map_status` at 4108 ms while the extraction scenarios ran beside it, where it takes ~150 ms
    alone; SC-301 is a statement about the bridge, so its facts now run while nothing else does.
18. **The shipped sample was not JSON** — found by the live step (T055), not by a test: `bridge.config.sample.json`
    carried single backslashes in its paths, so every call against the config copied beside the Release
    executable was refused as Unconfigured. B05 (18) now parses the sample and the fragment and loads the sample
    through `BridgeConfigFile.Load` (Red on the broken file, green after the rewrite with JSON escapes).

**Whole-suite runs** (quickstart "Implementation record — build and test lines"): T047 139 passed / 7 skipped in
2 m 26 s; T054 unarmed 144 passed / 8 skipped in 2 m 54 s, armed first run 150 passed / 1 failed (B08 (6), the
timing above) / 1 skipped in 2 m 40 s; the final runs on the last build are the last two rows of that table. Every
armed run: the live map's hash `ab23953e…` and the store's `3b3f9aa2…` equal before and after; MemOS
`git status --porcelain` empty before and after (FR-349).

**Live steps (T055–T059), 2026-09-16, through the Release executable over stdio** (quickstart "Record" and "Side by
side"; a minimal MCP stdio client in the scratchpad drove the calls): `solutions` three solutions at runs 4, 5, 6;
`symbol_search(projectId 132040, "btnDeal_Click")` id 584; `symbol_detail(1200)` `M:GameRoom.Shoe.Draw` at
`Shoe.vb:36`; `references(1200)` 7 calls in `BlackjackControl.vb`; `orphans(GameRoom)` 299 (91 + 208); `type_usages
(3023)` 20 with 16 from outside and `references(3023)` 0; `CodeMemMapFixture` 3556 + 4200 as one declaration;
`map_status` MemOS `behind` by 11 at `cbeb6615`; `extract(MemOS)` with both gates off refused naming
`extract.enabled` with one log line; on a byte-equal copy of the map with `extract.enabled` on, `extract(MemOS)` exit
0 in 30.6 s — run 7, 15792 observed, 14650 matched, 1142 new, 63 retired, digest `051faf7d…` — and `map_status`
MemOS `current`; the hook entry by hand on the copy: a green build in `repos\CodeMem` extracted CodeMem as run 8 in
7 s, the gate-off case named `extract.onGreenBuild`, an unregistered directory named the path, exit 0 every time.
Every call under 110 ms but the extraction itself. The live map and the store were never written; the MemOS tree
stayed clean.

**Left to the Operator and the Architect** (not this feature's code, or not this feature's hand):

- Approve the `codemem` project server in an interactive Claude Code session in `repos\CodeMem` (`claude mcp list`
  shows it connected) and, for other repositories, the user-scope line in the process document.
- The MemOS Shell column of the side-by-side table (SC-302).
- Merge `hooks/settings.fragment.json` into `~/.claude/settings.json` and watch a real `dotnet build` in a session
  produce the line (T059's mechanism was exercised by hand; the merge is the Operator's).
- **T060**, the live MemOS run 7, only at the Architect's request and after a backup; both gates stay off in the
  config beside the executable until the Architect flips them.
- ~~Accept 299 orphans on GameRoom~~ — ruled 2026-09-16: 299 is the accepted baseline (deviation 13).
- The commits.

## Phase 0 / Phase 1 outputs

- [spike.md](spike.md) — the two facts, proven; the mechanism corrections.
- [research.md](research.md) — R41–R56.
- [data-model.md](data-model.md) — what is read, the records, verdict order, refusal kinds.
- [contracts/tools.md](contracts/tools.md), [contracts/cli-config-hook.md](contracts/cli-config-hook.md).
- [quickstart.md](quickstart.md) — fixture validation and the live steps with their record table.

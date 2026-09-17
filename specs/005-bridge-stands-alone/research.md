# Research: CodeMem 005 — The Bridge Stands Alone

**Date**: 2026-09-16 | **Spec**: [spec.md](spec.md) (clarified; five rulings) | **Numbering**: R60 onward (004 ended at R59).

Every "Verified" entry names the artefact that proved it; every artefact lives under this session's scratchpad
(`plan-research/`) and is scratch, not shipped. Nothing here opened either live database for write; the two extractor
probes wrote only to scratch maps.

## R60. Environment facts (Verified 2026-09-16)

- `CodeMem.Extraction` references `Microsoft.CodeAnalysis.VisualBasic.Workspaces` 4.14.0, `Microsoft.CodeAnalysis.Workspaces.MSBuild`
  4.14.0, `LibGit2Sharp` 0.32.0, and the 002 pins `Microsoft.Build.Tasks.Core` 17.8.43 and `System.Formats.Asn1` 8.0.1. **No
  `Microsoft.Build.Locator` anywhere in `src/`**: the extractor process loads `Microsoft.Build` from the package graph, which resolves to
  **17.7.2** (`obj/project.assets.json`; the only `Microsoft.Build` in the NuGet cache, `lib/net472` and `lib/net7.0`).
- The `.slnx` defect reproduced twice: `CodeMem.Extractor --solution …\RicksLife\RicksLife.slnx --db <scratch>` → exit 1,
  `InvalidProjectFileException: No file format header found.`, **0.42 s** — too fast for a build-host launch; the classic parser
  (`Microsoft.Build.Construction.SolutionFile`, 17.7.2) fails in-process before any workspace work. The harness (R61) reproduced it
  with `OpenSolutionAsync` on a `.slnx` twin of the fixture.
- Packages that could parse a `.slnx` are not loadable on `net8.0`: MSBuild gained `.slnx` in 17.12, whose packages target `net9.0`
  (the 17.x line follows its SDK's framework: 17.7 → net7.0 in the cache, 17.8 → net8.0); the cache's `Microsoft.Build.Framework`
  18.0.2 and `Microsoft.CodeAnalysis.Workspaces.MSBuild` 5.9.0 both ship `net10.0` and `net472` only. Route (a) of 152688 is therefore
  a target-framework move of the whole solution, not a package bump.
- The live map (through the 004 bridge, 2026-09-16 21:37 local): five solutions; the registry's five rows all active and bound.
  `DSP_Processor.sln` is committed in that repository (`d07d341`); `RicksLife.sln` was never committed and no longer exists.
- Suite baseline on `main` at `90d62ca`: 145 passed / 8 skipped / 0 failed, 2 m 33 s.

## R61. `.slnx`: route (b), the loader owns the parse (Verified — harness `plan-research/Harness`)

**Decision**: the loader parses the `.slnx` itself and opens each project it names with `OpenProjectAsync` into one `MSBuildWorkspace`,
in document order, skipping a path the workspace already holds (loaded transitively as another project's reference); everything after
that — `CompileAll`, staging, reconciliation, publication — is unchanged.

**Evidence** (scratch VB console, the extractor's package set, `SampleSlnx/` = the committed fixture plus a two-line `Sample.slnx`):

| Run | Result |
|---|---|
| `OpenSolutionAsync(Sample.sln)` | 2 projects; `Sample.App` → `Sample.Lib` reference intact; docs 7 + 5; 0 errors; 1.9 s |
| `OpenSolutionAsync(Sample.slnx)` | `InvalidProjectFileException: No file format header found.` (route (a) with today's packages) |
| parse `Sample.slnx` → `OpenProjectAsync` each | `Sample.Lib` opened (1 project), `Sample.App` opened (2 projects — Lib reused, not duplicated); the same docs, references and 0 errors as the `.sln`; 2.4 s |

**The parse** (data-model §10): well-formed XML with a `Solution` root; every `Project` element at any depth (folders are organisation);
its `Path` attribute resolved against the `.slnx` directory with `/` normalised to the platform separator; a path that does not exist
refuses naming it before any project is opened; no `Project` element refuses naming the file; a `Project` without `Path` is ignored. The
`.sln` path (`OpenSolutionAsync`) and the `.vbproj` path (`OpenProjectAsync`) are untouched; the extension check accepts exactly the
three and refuses any other by name before a workspace is created (FR-424).

**Alternatives**: (a) bump `Microsoft.Build.*` ≥ 17.12 — rejected by R60 (net9.0+ only; it would also move the BuildHost and the SDK
stamp, the risk 152688 named); (a′) `Microsoft.CodeAnalysis.Workspaces.MSBuild` 5.x — net10.0 only, the same move; `dotnet sln migrate`
in reverse (generate a `.sln`) — the workaround this feature retires.

**Known limit** (plan §Known limits): a `.slnx` `<Configurations>`/`<Build>` section is not read; every project is built under the run's
`Configuration` global property, which is also what the `.sln` path effectively does through the same global properties.

## R62. NU1xxx: classify a workspace Failure through NuGet's own record (Verified — harness, three fixtures)

**What the workspace says** (MSBuildWorkspace 4.14, design-time build): a NuGet restore log message replayed by `ResolvePackageAssets`
arrives as `WorkspaceDiagnosticKind.Failure` with the text `Msbuild failed when processing the file '<project>' with message: <NuGet
message>` — **no code, no severity word** — and the compilation is nonetheless produced (the NU1701 project: 3 documents, 0 errors,
2 top-level members). The loader's "any Failure aborts" rule is what fails RicksLife; the compiler was never the problem.

**What NuGet records**: the project's `obj/project.assets.json` carries `logs[]`, each with `code`, `level`, `message`, `libraryId`,
`targetGraphs`. Verified on three scratch projects that **the failure text after `with message: ` equals the assets log's `message`
exactly**:

| Fixture | Assets log | Failure text equals message |
|---|---|---|
| `Nu1701` (OpenTK 3.1.0, `lib/net20`, on net8.0) | `NU1701`, level **Warning**, libraryId OpenTK | yes |
| `Nu1101` (a package no source holds) | `NU1101`, level **Error** | yes |
| `Nu1701b` (a source folder that did not exist yet) | `NU1301`, level **Error** | yes |

**Decision (Q8 as ruled, mechanised)**: after the workspace opens, for each Failure diagnostic the loader parses the project path and
the text; reads that project's assets file (`obj/project.assets.json` under the project directory); finds the `logs[]` entry whose
`message` equals the text. Level `Warning` → the load continues and the warning is recorded (code, the project file, message). Level
`Error` → the load aborts naming the code and the project, exit 1, before any compilation is requested. No entry, or no assets file →
the Failure aborts as today, with the message. The "severity word" of the ruling is NuGet's `level`. **No `NoWarn` anywhere**: the
compilation exists despite the Failure, so nothing needs suppressing; the property-dictionary supplement the spec allowed is not needed
and is not built. The extractor reads no environment variable for this (FR-427's fact: the child runs with `NoWarn` absent).

**Alternatives**: match the message text against known NU17xx wordings (brittle, no code); `NoWarn=NU1701` in the property dictionary
(suppresses what must be recorded; it is the environment workaround in another coat); treat every Failure whose text says "was restored
using" as a warning (the same brittleness, and NU1101's text would still abort — by accident, not by rule).

**Known limit**: a project whose `BaseIntermediateOutputPath` is not `obj/` has its assets file elsewhere; the loader finds no entry and
the Failure aborts as today, naming the message. Recorded, not handled: none of the five live solutions does this.

## R63. Schema version 3 (Decided — Q2 as ruled)

**DDL** (contracts/extractor.md §5): one table, `extract_run_warnings` (`id`, `run_id → extract_runs(id)`, `code`, `project_path` NULL,
`message`), one index on `run_id`; no trigger (the obligation is on the loader, not on the row). The migration text is one constant,
`MigrationToVersion3`, applied by `UpgradeToVersion3`; the version-1 and version-2 texts stay frozen (002's rule). `SchemaVersion.Current`
= 3; `SchemaState` gains `Version2`; `InspectSchema`: 1 → `Version1`, 2 → `Version2`, 3 → `Current`, above → `Newer`.

**Paths** (`ExtractionRun` step 4): Fresh → `CreateVersion1`, identity row, `[DuringInitialize]`, `UpgradeToVersion2`, `UpgradeToVersion3`,
`[DuringUpgrade]`, `SetSchemaVersion(3)`; Version1 → `UpgradeToVersion2`, `UpgradeToVersion3`, `[DuringUpgrade]`, `Set(3)`; Version2 →
`UpgradeToVersion3`, `[DuringUpgrade]`, `Set(3)`. The `DuringUpgrade` seam keeps its meaning — after every migration statement, before
the version is written — so an aborted upgrade of a version-2 map leaves it at 2 with the table present and the next run completes it,
exactly as 002's S02 (4) shows for 1 → 2. The fresh path and the upgrade paths yield byte-identical schemas (002 R22's fact, extended).

**Writes** (landed at T034–T036 behind X02's Red, not with the schema — analyze C1): `ExtractRunWarningsRepository.InsertAll(db, runId,
warnings)` right after `InsertCompleted` or `InsertFailed` — a run fact
keyed by the run, inside the same transaction; nothing on exit 1 or 2 (no run row exists). `ReadByRun(db, runId)` for the bridge and
the tests; `CountByRun` is not needed — `SolutionsReader` counts the list.

**Bridge**: `BridgeSchemaPin.Required` = 3; `VersionBelow` now also fires for a version-2 map with the same remedy ("run the extractor
once; it upgrades in place"); B02's "hand-bumped version 3" fixture for `VersionAbove` becomes version 4.

**Expected Reds in 002's tests, named** (plan §Test design): `F06_FreshMapTests` (two asserts of 2), `S02_SchemaUpgradeTests` (1) and
(4) (2 → 3) and (3) (the "newer" refusal fixture 3 → 4), `US1_FirstRunTests` (2 → 3). Each is revised in the task that lands version 3,
after its Red is recorded; `SchemaConstraintTests` is untouched (its raw inserts stay legal at any version).

## R64. `not_in_map` from the extract log (Decided — Q1 as ruled)

`NotInMapReader.Read(mapSolutions)`: open `extract.log` (`ExtractLog.LogPath()`, UTF-8, `FileShare.ReadWrite`); split each line on tabs
(the seven columns of 004 §5); keep lines whose resolution column is `PathNotInMap` or `AmbiguousSolutionFile` and whose target column
begins `repoPath=`; the directory is the rest of that column, normalised as `SolutionScope` normalises a directory (full path, platform
separators, one trailing separator, compared `OrdinalIgnoreCase`); one entry per directory, the latest line's time and origin kept;
drop every entry that a mapped root now contains (the same containment `TargetResolver` uses, R65); for each survivor recompute the
suggestion (R66). Lines with 004's retired kinds (`PathNotRegistered`) are history and are ignored. A log that cannot be opened or
read sets `notInMapError` on the envelope and leaves `notInMap` empty. The log is small (one line per invocation) and read once per
call; SC-301's 3 s per read tool is inherited and asserted for `map_status` with a 200-line log.

## R65. Resolution from the map (Decided — FR-410, Q6 as ruled)

`TargetResolver.ByPath`: for every `solutions` row, root = `SolutionScope.Resolve(row.RepoRoot, Path.GetDirectoryName(row.LastSeenPath))`
— the extractor's own rule (repository root when recorded, else the solution file's directory) — `ContainsDirectory(repoPath)`; the
longest root wins; two roots of equal length → `AmbiguousRoot` naming the keys; none → the not-in-map answer (R66). `ByKey`:
`SolutionsRepository.ReadByKey`; absent → `SolutionKeyUnknown` with the add remedy, unless `solutionPath` was given (R67). The registry
stage, `StoreAccess`, `RegistryRecord` and the four registry counts leave the door and the scope envelope.

## R66. The not-in-map answer and its suggestion (Decided — Q4 as ruled)

`SolutionFileSuggestion.Inspect(directory)`: `Directory.GetFiles(directory, "*.sln")` plus `"*.slnx"` — the top level only, no
recursion, no parent (`Directory.Exists` false → "could not be read now", no files); 0 → `PathNotInMap` with placeholders; 1 →
`PathNotInMap` with the suggested key (file name without extension) and the command `extract --solution-key <key> --solution <full
path>`; > 1 → `AmbiguousSolutionFile` naming every file, no key. The command text is the CLI form; the text also names the tool form
(`solutionKey` and `solutionPath`). The same module serves the refusal (at `extract` time) and the `notInMap` entries (at `map_status`
time), so the two never disagree (Article XII).

## R67. The by-key shape with an explicit path (Decided — Q3 as ruled)

`ExtractRequest` gains `SolutionPath`; `ExtractDoor.RequireOneTarget`: exactly one of `SolutionKey`, `RepoPath`, `Stale`; `SolutionPath`
only when `SolutionKey` is set — otherwise `TargetMissing` whose text names the three shapes and the optional path. `TargetResolver.ByKey`
with a path: no map lookup is required; the target is (`key`, `Path.GetFullPath(solutionPath)`); the file's existence is the
extractor's own preflight ("solution not found", exit 1), reported verbatim — the bridge does not pre-check it, so one door owns that
rule. The launch line is `--solution-key <key> --solution <path> --db <map>`, unchanged in shape. Command line: `--solution <path>`
parsed on the `extract` entry only; the log's target-as-given column reads `solutionKey=<key> solution=<path>`.

## R68. `projectId` refused by name on the MCP surface (Decided; confirmed by the first Red)

The SDK binds a method-backed tool's parameters by name and ignores an argument no parameter names, so a bare removal of the parameter
would make `projectId` silently ignored, not refused. Every tool delegate therefore takes one more parameter of type
`RequestContext(Of CallToolRequestParams)` — the SDK injects it and excludes it from the advertised schema — and `BridgeTools` reads
`Params.Arguments`; a key equal to `projectId` (ordinal, and also ignoring case) refuses `ProjectIdRemoved` at the argument stage, before
the configuration is read. The in-process host (`BridgeHost`) passes the raw dictionary the same way. If the injection does not bind in
a VB delegate, the fallback is one wrapping delegate per tool over the raw `CallToolRequestParams` with a hand-written input schema — the
first Red of B09 (2) decides, and the plan records which landed.

## R69. Archive (Decided — Q9; Article XIV)

`_Archive/004-store/` at the repository root, outside every project folder and every gate's scan (`SqlLocationGateTests`,
`FileHeaderGateTests`, `MemOsReferenceGateTests` and the new `BridgeStandaloneGateTests` scan `src/` and `tests/` only): `StoreDatabase.vb`,
`Registry/CodeMapSolutionsRepository.vb`, `Registry/RegistryTableMissingException.vb`, `Records/RegistryRecord.vb`, `Reading/StoreAccess.vb`,
`Envelopes/UnboundEntryEnvelope.vb`, `Envelopes/InactiveEntryEnvelope.vb`, `tests/Support/RegistryFixture.vb`, and a `README.md` naming what
moved, why (152687, v1.4.0) and the commit it came from. `BridgeSqlGateTests` facts (2) and (4), and the retired facts of B02, B04 and B05, move to
`_Archive/004-store/tests/` as `.vb` fragments with their headers (Article XIV; analyze C2); facts (1) and (3) stay. No file is
deleted.

## R70. Constitution v1.4.0 (Decided — for STOP 1; FR-431)

Article IX: the paragraph beginning "One exception is ruled (decisions 137077, 142362, 152658; feature 004, amendment v1.3.0)" is
removed; nothing else in the article changes, so the body reads as v1.2.1's. Review Gates: the connection-site line becomes "New
SqliteConnection appears in exactly one production file, `MapDatabase.vb` (the map: read-write for the extractor, read-only for the
bridge), asserted by a test that names it and fires when a second site appears (v1.4.0; the v1.3.0 second site is archived)." Amendment
Record entry (the wording proposed for the ruling):

> **2026-09-16 — v1.4.0 (MINOR).** Article IX's ruled exception (v1.3.0) is withdrawn: its text returns to v1.2.1's — CodeMem opens no
> database in any role other than its own map — and the connection-site gate names one production file again, `MapDatabase.vb`. No
> article is removed or redefined; an obligation v1.3.0 narrowed is restored.
>
> Rationale: task 152687 (the Architect, 2026-09-16): "CodeMem is supposed to be a standalone project completely independent of MemOS;
> MemOS is a project that can look into CodeMem if it's there." Feature 004 inverted that direction — the bridge could not answer a
> question about its own map without MemOS's store present — and it drifted in four recorded steps: 137077's store-read clause
> (2026-09-12), 142362's "a file, not a dependency" (2026-09-13), 152658 ruling 1's store clause (2026-09-15) and 152672 item 1, this
> document's v1.3.0 (2026-09-15). All four are superseded by 152687. What the bridge needs — a solution's key, repository root and
> last-seen path — is in the map's own `solutions` table; the store supplied only MemOS's project-to-key mapping, which is MemOS's.
>
> Migration path: feature 005. `StoreDatabase.vb`, the registry module and every reader of them move to `_Archive/004-store/` (Article
> XIV); `SqlLocationGateTests.OnlyMapDatabaseOpensAConnection` is amended to the one file in the task in which `StoreDatabase.vb` leaves
> `src/`, after its Red is recorded, and re-fired; `bridge.config.json` loses `storePath`, and a file still carrying it is refused
> naming the key.

Version line: `1.4.0 | Ratified: 2026-09-10 | Last Amended: 2026-09-16`. A fact transcribes v1.2.1's Article IX body and asserts the
current article equals it (FR-431).

## R71. Test fixtures for the warnings (Verified — harness `Nu1701b`, `NetFxOnly`)

A hand-packed package reproduces NU1701 **offline** and weighs 3.6 KB: `NetFxOnly.vbproj` (net8.0, one class) packed with
`IncludeBuildOutput=false` and its assembly placed under `lib/net461/` — NuGet judges compatibility by the folder name, not the
assembly — as `CodeMem.NetFxOnly.1.0.0.nupkg` (`dotnet build` then `dotnet pack --no-build`; pack without a prior build fails to find
the file). A consumer whose `NuGet.config` clears the sources and names the local folder restores it with `warning NU1701`, the assets
log carries `NU1701 / Warning / CodeMem.NetFxOnly`, and the workspace reports the Failure whose text equals it. OpenTK's real package
(4.4 MB) is not committed.

**Shipped fixtures** (`tests/CodeMem.Tests/Fixtures/Warnings/`): `NuGet.config` (clear; `packages/` only), `packages/CodeMem.NetFxOnly.1.0.0.nupkg`
(the binary, with the `NetFxOnly/` source it was packed from beside it so it can be rebuilt), `Nu1701/Nu1701.vbproj` + one class
(references the package), `Nu1101/Nu1101.vbproj` + one class (references `CodeMem.NoSuchPackage 1.0.0`, which no source holds →
`NU1101`, level Error, from the local source — never `NU1301`, because the folder exists). A `WarningsFixture` collection fixture restores
both once (`dotnet restore`, offline; the NU1101 restore fails by design and still writes its assets file, as observed). The compile-error
variant is a `FixtureCopy` of `Nu1701` with one broken `.vb` added at test time. "The compiler was not invoked" is observed on the
production route: exit 1, stderr's first line is the loader's sentence naming `NU1101` and the project, and no `errors=` line (the
compile stage's output) appears.

**Format-parity fixture**: `tests/CodeMem.Tests/Fixtures/Sample/Sample.slnx` (the two-line file of R61) committed beside `Sample.sln`;
`FixtureSolution` gains `SolutionXPath`; one restore covers both (restore is per project).

## R72. What changes for the hook and the process document (Decided)

`HookRequest.IsSolutionOrProject` accepts `.slnx` (one token in a list of three). The hook's answer line is unchanged in shape; a
not-in-map refusal's text — command included — rides in it. `.mcp.json` and the settings fragment are byte-identical to 004's. The
README gains: the one-file rule, "not in the map" and how a solution is added, `--solution`, the config without `storePath`, the
archive, and the two extractor changes; it loses the registry and bind steps.

## R73. Live sequence (Decided — Q10)

Copy first, then the live map at the Architect's request after a backup. The bridge at pin 3 refuses the live map (version 2) with
`VersionBelow` until the first extractor run at version 3 — which is the `DSP_Processor` re-point; that refusal is recorded as the
first live line, not hidden. Order: back up → remove `storePath` from the config beside the Release executable → `extract --solution-key
DSP_Processor --solution …\DSP_Processor.slnx` (upgrades the map to 3, re-points) → `extract --solution-key RicksLife --solution
…\RicksLife.slnx` with nothing in the environment (three NU1701 rows expected: OpenTK, OpenTK.GLControl, SkiaSharp.Views.WindowsForms) →
`map_status`, `solutions` → `extract --repo-path <DSP_Processor root>` with `memos.sqlite` renamed for the check and the Shell closed →
`extract --repo-path <an unmapped repository>` and `map_status` listing it → restore `memos.sqlite`'s name. The Operator then deletes
`DSP_Processor.sln` (a commit in that repository) and re-points the two registry rows' `extraction_scope` in MemOS; neither is this
feature's code.

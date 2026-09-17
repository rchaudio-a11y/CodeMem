# Data Model: CodeMem 005 — The Bridge Stands Alone

**Date**: 2026-09-16 | **Spec**: [spec.md](spec.md) | **Research**: [research.md](research.md)

Deltas against 004's data model; a section not listed here is unchanged (the presented declaration, the occurrence, the
`type_usages` result, the presentation rule).

## 1. What is read

One database, the map, read-only per call (`MapAccess.OpenRead`, `Mode=ReadOnly`, one read transaction). One text file, `extract.log`
beside the executable, read by `map_status` only (§4). Nothing else on disk is opened by the bridge; the store, its path and its
records leave the product (R69).

## 2. Mapped solution and mapped root (FR-410, Q6)

```text
SolutionRecord   : id, key (UNIQUE), name, repoRoot|null, lastSeenPath, createdUtc, firstRunId|null      (unchanged rows)
Mapped root      : SolutionScope.Resolve(repoRoot, directoryOf(lastSeenPath)).Root
                   — the repository root when recorded, else the solution file's directory; the extractor's own rule, one door
```

A `repoPath` resolves to the mapped solution whose root contains it or is it (`ContainsDirectory`); the longest root wins; equal
lengths → `AmbiguousRoot`; none → §3.

## 3. Scope (FR-405)

```text
Scope            : by = 'solutionKey', solutionKey, solutions[{solutionId, key}]        (exactly one solution)
```

`projectId`, `activeRegistryRows`, `unboundRegistryRows`, `inactiveRegistryRows`, `danglingSolutionIds`, `hadNothingToSearch` and
`reason` leave the envelope. A call carrying `projectId` is `ProjectIdRemoved` at the argument stage (R68).

## 4. Observed directory and the `not_in_map` entry (FR-411, FR-419; R64, R66)

```text
LogLine          : utc | origin | target-as-given | resolution | gate | exit | line                     (004 §8, unchanged shape)
Observed         : a LogLine whose resolution ∈ {PathNotInMap, AmbiguousSolutionFile} and whose target begins 'repoPath='
Directory key    : the target's directory normalised as SolutionScope normalises one (full path, platform separators, one trailing
                   separator; compared OrdinalIgnoreCase) — one entry per key, the latest line's utc and origin kept
Suggestion       : Inspect(directory) → files[] (top-level *.sln and *.slnx), suggestedKey|null (one file: its name without extension),
                   command|null ('extract --solution-key <key> --solution <full path>'), ambiguous (files.Count > 1), readable (bool)
NotInMapEntry    : path, verdict = 'not_in_map', observedUtc, origin, solutionFiles[], suggestedKey|null, command|null, note|null
```

An entry is dropped the moment a mapped root contains its directory. Nothing is written; the log is never rewritten.

## 5. `map_status` result (FR-418–FR-420; Q5)

```text
MapStatus        : readAtUtc, mapPath, entries[Entry], notInMap[NotInMapEntry], notInMapError|null
Entry            : solutionKey, solutionId,
                   run{runId, commitSha|null, isDirty|null, finishedUtc}|null,
                   repoRoot|null, head{sha|null, treeDirty|null, behindBy|null, note|null},
                   verdict ('current' | 'behind' | 'dirty' | 'no_git' | 'diverged'), reason
```

Verdict order: `no_git` → `dirty` → `diverged` → `behind` → `current` (004's order minus `map_missing_solution`). `storePath`, `bound`,
`unbound`, `inactive`, `projectId` and `codememSolutionId` leave. `extract --stale` iterates entries with `behind`, `dirty` or
`diverged`; `notInMap` entries are listed in the stale result under `notInMap` and never extracted.

## 6. Extract request, result and log line (FR-414–FR-417; R67)

```text
ExtractRequest   : origin, solutionKey|null, solutionPath|null (only beside solutionKey), repoPath|null, stale (bool)
Cardinality      : exactly one of solutionKey, repoPath, stale; solutionPath without solutionKey → TargetMissing
Target as given  : 'solutionKey=<key>' | 'solutionKey=<key> solution=<path>' | 'repoPath=<dir>' | 'stale'
ResolvedTarget   : solutionKey, solutionPath (the map's lastSeenPath, or the explicit path, full), mapPath, solutionId|null (null for a key not yet in the map)
ExtractResult    : as 004 §8, plus run.warnings[RunWarning] (the published run's rows, empty list when none)
StaleResult      : as 004 §8, plus notInMap[NotInMapEntry]
```

The launch line is unchanged: `--solution-key <key> --solution <path> --db <map>`.

## 7. Bridge configuration (FR-403)

```text
BridgeConfig     : mapPath, extractorPath|null, extract{enabled=false, onGreenBuild=false}, source
```

A file carrying `storePath` → `ConfigKeyRetired` naming the key, before anything is opened; a missing `mapPath` → `Unconfigured`.

## 8. Run warning — schema version 3 (FR-429; R63)

```text
extract_run_warnings : id (AUTOINCREMENT), run_id → extract_runs(id) NOT NULL, code TEXT NOT NULL, project_path TEXT NULL, message TEXT NOT NULL
                       index ix_extract_run_warnings_run_id
RunWarningRecord     : id, runId, code, projectPath|null, message
On the wire          : latestRun.warnings (count) in solutions; run.warnings[{code, projectPath, message}] in extract
```

`project_path` is the solution-relative path of the project file the failure named (the same convention as every stored path), NULL when
the failure named none. Rows are written right after the run row, completed or failed; never for exit 1 or 2. Operational metadata:
excluded from the canonical fact set (Article IV; I02 unchanged).

Map states at open: `Fresh`, `Version1`, `Version2` (new), `Current` (= 3), `Foreign`, `Newer`. Seam values unchanged; `DuringUpgrade`
fires after the last migration statement of whichever path ran, before `schema_version` is written.

## 9. Refusal kinds (FR-409; twenty-nine)

Kept from 004 (twenty-five): `Unconfigured`, `MapAbsent`, `NotAMap`, `VersionBelow`, `VersionAbove`, `Busy`, `Unopenable` (map only),
`ScopeMissing` (text revised), `FilterMissing`, `KindUnknown`, `SymbolIdMissing`, `KindNotExamined`, `SolutionKeyUnknown` (remedy
extended), `SymbolNotFound`, `SymbolOutOfScope`, `SymbolRetired`, `NotAProjectRow`, `NotAType`, `GateOff`, `AmbiguousRoot` (text revised),
`ExtractionRunning`, `ExtractorNotFound`, `TargetMissing` (text revised), `AmbiguousTarget`, `ChildTimedOut`.

Retired (seven): `RegistryAbsent`, `ScopeConflict`, `KeyNotRegistered`, `KeyUnbound`, `KeyInactive`, `MapMissingSolution`, `PathNotRegistered`.

New (four): `PathNotInMap`, `AmbiguousSolutionFile`, `ProjectIdRemoved`, `ConfigKeyRetired`.

25 + 4 = 29. Texts: contracts/tools.md §6.

## 10. The `.slnx` the loader accepts (FR-421–FR-424; R61)

```text
Document         : well-formed XML; root element local name 'Solution' (any namespace)
Project          : every element with local name 'Project' at any depth; its 'Path' attribute, '/' → the platform separator,
                   resolved against the .slnx directory; an element without 'Path' is ignored; 'Type' and other attributes are not read
Refusals (exit 1): not well-formed → "solution file is not well-formed XML: <file>: <parser message>"
                   no Solution root → "solution file has no Solution root: <file>"
                   no Project with Path → "solution file names no project: <file>"
                   a Path that does not exist → "solution file names a project that does not exist: <path> (in <file>)" — before any project is opened
Extension        : .sln → OpenSolutionAsync; .slnx → this parse + OpenProjectAsync per project in document order, skipping paths already loaded;
                   .vbproj → OpenProjectAsync; anything else → "unsupported solution file: <path> (expected .sln, .slnx or .vbproj)"
Base directory   : SolutionPaths.BaseDirectory(path) — the .slnx directory, as for a .sln; solution_key defaults to the file name without extension
```

## 11. The warning rule (FR-427; R62)

```text
Failure          : WorkspaceDiagnosticKind.Failure with text "Msbuild failed when processing the file '<project>' with message: <text>"
Assets log       : <project dir>/obj/project.assets.json → logs[] {code, level, message, libraryId, targetGraphs}
Match            : the entry whose message equals <text> (ordinal)
  level Warning  → RunWarning{code, project_path = relative(<project>), message}; the load continues
  level Error    → WorkspaceLoadException "restore error <code> in <project>: <text>"; exit 1; no compilation requested
  no match / no assets file / no '<project>' shape → WorkspaceLoadException with the text, as today
```

# Data Model: CodeMem 004 — The Bridge

**Feature**: `004-codemem-bridge` | **Date**: 2026-09-15 | **Spec**: [spec.md](spec.md) | **Research**: [research.md](research.md)

**No schema change.** The map stays at version 2; `memos.sqlite` is not changed. Everything below is read from
existing tables or computed per call; the one thing the bridge writes is the extract log (a text file, FR-351).

## 1. What is read

| Source | Table | Columns used | By |
|---|---|---|---|
| map (read-only) | `map_identity` | `map_guid`, `schema_version`, `created_utc` | every tool (the pin), `solutions` |
| map | `solutions` | `id`, `key`, `name`, `repo_root`, `last_seen_path` | scope, `solutions`, `map_status`, `extract` |
| map | `extract_runs` | every column; `solutions` reads the latest row of any outcome, `map_status` and `extract` the latest completed row (INC4) | `solutions`, `map_status`, `extract` (counts) |
| map | `code_symbols` | every column; `is_active = 1` only | search, detail, references, orphans, `type_usages` |
| map | `code_parts` | `symbol_id`, `path`, `start_offset`, `length`, `start_line`, `start_column` | detail |
| map | `code_edges` | every column | detail, references, orphans, `type_usages` |
| store (read-only) | `code_map_solutions` | `id`, `project_id`, `solution_key`, `codemem_solution_id`, `state` (`extraction_scope`, `notes` read but unused) | scope by `projectId`, `map_status`, `extract` |

Nothing else in the store is opened or named (FR-304; `BridgeSqlGateTests`).

## 2. Registry row and bound solution

```text
RegistryRow      : id, projectId, solutionKey, codememSolutionId (nullable), state, extractionScope, notes
BoundSolution    : RegistryRow (state = 'active', codememSolutionId not null) + map SolutionRow (id = codememSolutionId)
RegisteredRoot   : BoundSolution.map.repo_root  (Q1, ruled) — trailing separator as written by the extractor
SolutionPath     : BoundSolution.map.last_seen_path
```

States of a registry row as the bridge sees them (no transition happens here; the store is read-only):

| Row | `map_status` | `extract(solutionKey)` | scope by `projectId` |
|---|---|---|---|
| active, bound, map row exists | one entry with a verdict | resolvable | included |
| active, bound, map row missing | `map_missing_solution` | refused: `MapMissingSolution` | listed in `danglingSolutionIds` |
| active, unbound | listed under `unbound`, no verdict | refused: `KeyUnbound` | counted in `unboundRegistryRows` |
| inactive (any binding) | listed under `inactive`, no verdict | refused: `KeyInactive` | counted in `inactiveRegistryRows` |
| no row for the key | — | refused: `KeyNotRegistered` | — |

## 3. Scope (058 §3.1, unchanged in shape)

```text
Scope : by ('projectId' | 'solutionKey'), projectId, solutionKey, solutions[{solutionId, key}],
        activeRegistryRows, unboundRegistryRows, inactiveRegistryRows, danglingSolutionIds[], hadNothingToSearch
```

By `projectId`: every active registry row with that `project_id` whose binding names an existing map solution;
the four counts describe the rows with that `project_id`; a bound id with no map row is a dangling id. By
`solutionKey`: the one map solution; the four registry fields are `null` (the registry was not consulted).
`hadNothingToSearch` is true when `solutions` is empty; the result then says why: "no registry row names project
N" (Q7) or "every row for project N is unbound or inactive".

## 4. Presented symbol and declaration (twin fold, Q2)

```text
SymbolRef        : solutionId, solutionKey, id, docCommentId, kind, name, container{id,name}|null, project{id,name}|null, path, line
Declaration      : kind, name, path, line, container, compiledInto[{projectSymbolId, projectName, symbolId, docCommentId}], total = compiledInto.length
```

`symbol_search.symbols[]` holds Declarations (one per group of active symbols of one solution sharing kind, name,
path and line whose `project_symbol_id` values are all distinct and more than one — any other group stays one
declaration per row, COR3); a declaration compiled into one project has one entry and
its `id`/`docCommentId` mirrored at the top level for 058 compatibility. `symbol_detail.symbol` is a SymbolRef
plus `compiledInto` (every twin, the requested id first). Rows sharing the four fields under one project, or
across solutions, are never folded (FR-341).

## 5. Occurrence

```text
Occurrence       : verb, path, line, column, source{id,name,kind}, target{id|null, docCommentId, name|null, kind|null, external}
```

The identity invariant (FR-348): `target.id` non-null **or** `target.external = true` with `docCommentId`
present; never neither. In `references` the target is the asked symbol (id always present); in `symbol_detail`'s
outbound groups a framework target has `id = null`, `external = true`; in `type_usages` the target is the type, a
constructor or a member (id always present) and the occurrence also carries `fromInside`.

## 6. `type_usages` result (Q3, ruled)

```text
TypeUsages       : readAtUtc, scope, symbol (SymbolRef), byVerb{calls, uses, implements, extends, imports, depends_on, handles},
                   total, fromOutside, occurrences[Occurrence + fromInside + target{…, role: 'type'|'constructor'|'member'}]
```

`byVerb` lists all seven non-`part_of` verbs with zeros; `total = occurrences.length`; `fromOutside = count(not
fromInside)`. `fromInside` is true when the source symbol is the type or lies in its containment subtree (R51).

## 7. `map_status` result (Q4, ruled)

```text
MapStatus        : readAtUtc, mapPath, storePath, entries[Entry], bound, unbound[{solutionKey, projectId}], inactive[{solutionKey, projectId, state}]
Entry            : solutionKey, projectId, codememSolutionId,
                   run{runId, commitSha|null, isDirty|null, finishedUtc}|null,
                   repoRoot|null, head{sha|null, treeDirty|null, behindBy|null, note|null},
                   verdict ('current' | 'behind' | 'dirty' | 'no_git' | 'map_missing_solution' | 'diverged'),
                   reason (one sentence naming the facts: HEAD, the count, the missing thing)
```

Verdict order (R52): `map_missing_solution` → `no_git` → `dirty` → `diverged` → `behind` → `current`. Every fact
field is filled whenever it could be read, whatever the verdict.

## 8. Extract request, result and log line (FR-325–FR-334, FR-351)

```text
ExtractRequest   : origin ('tool' | 'green_build' | 'manual'), solutionKey|null, repoPath|null, stale (bool), configPath|null
ExtractResult    : origin, target{asGiven, resolvedKey|null, solutionPath|null, mapPath}, gate ('passed' | 'extract.enabled' | 'extract.onGreenBuild'),
                   refusal|null (kind + text), launched (bool), exitCode|null, timedOut (bool; TIM1: killed after the 540 s budget), line|null (summary, refusal or timeout line verbatim),
                   run{runId + the ten counts}|null, elapsedMs, logged (bool), logError|null
StaleResult      : origin, considered[{solutionKey, verdict, extracted (bool), result: ExtractResult|null}], extractedCount
LogLine          : utc | origin | target-as-given | resolution (key or refusal kind) | gate | exit code, '-' or 'timeout' | line (verbatim, one line)
                   — beside the executable; a stale run writes one line per solution launched and no header (INC3)
```

Resolution of `repoPath` (FR-326): among active, bound solutions whose map row has a non-null `repo_root`, those
whose root contains the path or is the path (`SolutionScope.ContainsDirectory`, COR1); one → resolved; none → `PathNotRegistered`
naming the path; several → the longest root; two roots of equal length (the same root bound twice) → `AmbiguousRoot`
naming both keys.

Gate evaluation (FR-330): `extract.enabled` false → `GateOff("extract.enabled")` for every origin; then
`origin = green_build` and `extract.onGreenBuild` false → `GateOff("extract.onGreenBuild")`.

Concurrency (FR-332): one in-process `SemaphoreSlim(1)`; a second request while one holds it → `ExtractionRunning`.
Across processes (two sessions) the extractor's own lock answers with exit 3, returned verbatim.

## 9. Bridge configuration (Q8, R49)

```text
BridgeConfig     : mapPath, storePath, extractorPath|null, extract{enabled=false, onGreenBuild=false}, source (the file path read)
```

Read on every call from `--config <path>` or `<exe directory>\bridge.config.json`; a missing file or key →
`Unconfigured(key)`.

## 10. Refusal kinds (FR-306; one kind per remedy; the wording owner is one module)

| Kind | Decided against | Names |
|---|---|---|
| `Unconfigured` | configuration | the key, the file |
| `MapAbsent`, `NotAMap`, `VersionBelow`, `VersionAbove`, `Busy`, `Unopenable` | the map | the map path (+ found/required versions) |
| `RegistryAbsent` | the store | the store path |
| `ScopeMissing`, `ScopeConflict`, `FilterMissing`, `KindUnknown`, `SymbolIdMissing`, `KindNotExamined` | arguments | the argument |
| `SolutionKeyUnknown`, `SymbolNotFound`, `SymbolOutOfScope`, `SymbolRetired`, `NotAProjectRow`, `NotAType` | the map | id, key, kind, path, last-seen run |
| `GateOff` | configuration | the gate |
| `PathNotRegistered`, `AmbiguousRoot`, `KeyNotRegistered`, `KeyUnbound`, `KeyInactive` | the store (+ the map for roots) | the path / the key(s) |
| `MapMissingSolution` | the map | the key and the bound id |
| `ExtractionRunning`, `ExtractorNotFound`, `TargetMissing` (`extract` with none of the three) | the bridge | the path / the argument |
| `AmbiguousTarget` (the hook's command names more than one `.sln`/`.vbproj`/directory, COR2) | arguments | the candidates |
| `ChildTimedOut` (the extractor exceeded the 540 s budget, TIM1) | the bridge | the key, the budget |

Texts are in contracts/tools.md §6; facts assert each kind's distinguishing phrase, never the whole sentence
(058's rule).

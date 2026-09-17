# Contract: the eight tools after 005

**Date**: 2026-09-16 | **Spec**: [../spec.md](../spec.md) | Supersedes 004's `contracts/tools.md` where the two differ; a section
not restated here (3.3–3.6, 5, 7) is 004's unchanged.

## 1. Parameters

| Parameter | Type | `solutions` | `symbol_search` | `symbol_detail` | `references` | `orphans` | `type_usages` | `map_status` | `extract` |
|---|---|---|---|---|---|---|---|---|---|
| `solutionKey` | string | — | required | required | required | required | required | — | one of the three |
| `solutionPath` | string | — | — | — | — | — | — | — | optional, only beside `solutionKey` |
| `name` / `kind` | string | — | at least one | — | — | `kind` optional | — | — | — |
| `projectSymbolId` | integer | — | optional | — | — | optional | — | — | — |
| `symbolId` | integer | — | — | required | required | — | required (a type) | — | — |
| `repoPath` | string | — | — | — | — | — | — | — | one of the three |
| `stale` | boolean | — | — | — | — | — | — | — | one of the three (`true`) |

`projectId` is no parameter of any tool; a call carrying it — whatever else it carries — is refused `ProjectIdRemoved` at the argument
stage (FR-406, R68). Any other unknown argument is ignored, as the protocol layer does today.

## 2. Descriptions (as registered; the phrases in bold are asserted by a fact — 004 FR-312's set, unchanged, plus FR-407's absence check)

A fact asserts that none of the eight contains `projectId`, `registry`, `code_map_solutions` or `MemOS project`.

**solutions** — "Every solution in the CodeMem code map with its latest extraction run: key, name, repo root, last-seen path; outcome,
schema and SDK version, source digest, commit and dirty state (unknown when there is no commit), start and finish, the ten
reconciliation counts, and the number of restore warnings recorded on the run. Read-only; the map's extractor is its only writer.
Refuses by name when the bridge is unconfigured or the map is absent, not a map, at another schema version, or busy under an
extraction. Call map_status to learn whether a run is current."

**symbol_search** — "Find symbols in the CodeMem code map by name and/or kind within one map solution (solutionKey — exact; solutions
lists the keys), optionally narrowed to one project row (projectSymbolId — the id of a project-kind row inside the solution; the filter
narrows the rows and never changes what the total counts). The name matches case-insensitively anywhere in the symbol's name; kind is
one of: namespace, class, module, structure, interface, enum, enum_member, delegate, method, constructor, property, field, event,
project. Returns active symbols only, **at most 200** declarations, always with the total matched and whether the result was truncated —
**narrow the filter rather than page**. A file linked into several projects declares one symbol per project; such twins are presented
**as one declaration compiled into N projects, carrying every project's symbol id.** This bridge scopes by solutionKey only. Read-only."

**symbol_detail** — 004's text with its last two sentences replaced by: "Requires symbolId and solutionKey; a symbol outside that
solution, one that does not exist, and one that is retired are each refused by name. Read-only."

**references** — 004's text with "Requires symbolId and exactly one of projectId or solutionKey." replaced by "Requires symbolId and
solutionKey."

**orphans** — 004's text with its opening replaced by: "Active symbols in the CodeMem code map that no recorded reference reaches, scoped
to one map solution (solutionKey), optionally narrowed to one kind (kind) or to one project row within the solution (projectSymbolId —
the id the result's byProject lists); a filter narrows the rows, it never changes what counts as an orphan, and the totals describe the
narrowed set and state the filter." Every mechanism phrase 060 registered stays verbatim.

**type_usages** — 004's text with "Requires symbolId and exactly one of projectId or solutionKey." replaced by "Requires symbolId and
solutionKey."

**map_status** — "For every solution the CodeMem map holds: the commit and dirty flag its latest completed run recorded, what HEAD is now
in the solution's repository root (the root the extractor scoped the run to; the solution file's directory when there was no
repository), whether the tree is dirty, how many commits HEAD is past the recorded one, and one verdict by name: current, behind (by
N, HEAD named), dirty (the tree has uncommitted or untracked changes), no_git (the root cannot be read as a repository, or the run
recorded no commit), or diverged (the recorded commit is not an ancestor of HEAD; no count is claimed). Nothing is guessed: a
repository the bridge cannot read says so. Also lists, as not_in_map, every directory an extract has refused as not in the map — a
green build the hook observed in a repository the map does not hold, or a repoPath given directly — that no mapped root contains yet,
with the solution file found there, the suggested key and the exact extract command that would add it; the bridge never adds a
solution itself. Call this at session start and before relying on references, orphans or type_usages; an extraction is the remedy
(the hook on a green build, or extract). Read-only."

**extract** — "Refreshes the CodeMem map by running the CodeMem extractor as a child process for one solution — named by solutionKey
(the map's last-seen solution file is used; with solutionPath the file is given explicitly and the solution need not be in the map
yet: that is how a solution is added, and how a solution is re-pointed at a moved or renamed file), or by a repoPath that lies under a
mapped solution's root — or, with stale set to true, for every mapped solution whose map_status is behind, dirty or diverged, in
sequence, one run each. A repoPath under no mapped root is answered not in the map with the command that adds it, and nothing runs;
the bridge never chooses a key. Gated: extract.enabled in bridge.config.json must be true (default false) — the automatic green-build
trigger additionally needs extract.onGreenBuild. A refusal names the gate that stopped it. Returns the extractor's exit code, its
summary or refusal line verbatim, the published run's ten counts and its recorded restore warnings. The bridge never writes the map
itself; the extractor is its only writer. Normally called by the green-build hook, not directly; a human runs extract with stale, or
with solutionKey and solutionPath to add a solution."

No description contains an uppercase SQL keyword (the bridge SQL gate scans literals) nor, in lowercase, a word the tree-wide
SQL-location gate matches; "adds", "refreshes" and "re-pointed" are the verbs used.

## 3. Result shapes (deltas)

### 3.1 `solutions` — `latestRun` gains `warnings` (integer; 0 when none; the run's rows counted).

### 3.2 `symbol_search` — the `scope` envelope is `{ "by": "solutionKey", "solutionKey": "GameRoom", "solutions": [ { "solutionId": 1, "key": "GameRoom" } ] }`; the registry counts, `dangling`, `hadNothingToSearch` and `reason` are gone. The same envelope in 3.3–3.6.

### 3.7 `map_status`

```json
{ "readAtUtc": "…", "mapPath": "C:\\_DB\\codemem.sqlite",
  "entries": [
    { "solutionKey": "DSP_Processor", "solutionId": 4,
      "run": { "runId": 8, "commitSha": "017459af…", "isDirty": true, "finishedUtc": "2026-09-16T05:58:49.9892099Z" },
      "repoRoot": "C:\\Users\\rchau\\source\\repos\\DSP_Processor\\",
      "head": { "sha": "d07d341b…", "treeDirty": false, "behindBy": 1, "note": null },
      "verdict": "behind", "reason": "HEAD d07d341b is 1 commit past the recorded 017459af; the tree is clean." },
    { "solutionKey": "GameRoom", "solutionId": 1, "…": "…", "verdict": "dirty", "reason": "…" } ],
  "notInMap": [
    { "path": "C:\\Users\\rchau\\source\\repos\\vbCalc\\", "verdict": "not_in_map", "observedUtc": "2026-09-17T03:10:00.0Z", "origin": "green_build",
      "solutionFiles": [ "C:\\Users\\rchau\\source\\repos\\vbCalc\\vbCalc.sln" ], "suggestedKey": "vbCalc",
      "command": "extract --solution-key vbCalc --solution C:\\Users\\rchau\\source\\repos\\vbCalc\\vbCalc.sln", "note": null } ],
  "notInMapError": null }
```

Entries ordered by key; `notInMap` ordered by path. A directory that could not be read now carries `solutionFiles: []`, `suggestedKey:
null`, `command: null` and `note: "directory could not be read now"`. An ambiguous directory carries every file, `suggestedKey: null`,
`command: null`, `note: "more than one solution file"`.

### 3.8 `extract`

`target.asGiven` is one of `solutionKey=<key>`, `solutionKey=<key> solution=<path>`, `repoPath=<dir>`, `stale`; `run` gains
`warnings: [ { "code": "NU1701", "projectPath": "RicksLife/RicksLife.vbproj", "message": "…" } ]` (an empty list when none); the stale
result gains `notInMap: [ … ]` (the entries of 3.7, never extracted). A refused not-in-map call: `refusal.kind` `PathNotInMap` or
`AmbiguousSolutionFile`, `launched: false`, the map byte-identical, `IsError = true` on the wire.

## 4. Order of refusal

Arguments (`ProjectIdRemoved` → `ScopeMissing` → `FilterMissing` → `KindUnknown` / `SymbolIdMissing` / `KindNotExamined` /
`TargetMissing`) → configuration (`Unconfigured`, `ConfigKeyRetired`) → map (`MapAbsent` → `NotAMap` → `VersionBelow` / `VersionAbove`
→ `Busy` / `Unopenable`) → question (`SolutionKeyUnknown` → `SymbolNotFound` → `SymbolOutOfScope` → `SymbolRetired` → `NotAProjectRow` /
`NotAType`); for `extract`: `TargetMissing` → `Unconfigured` / `ConfigKeyRetired` → `GateOff` → the map kinds → `SolutionKeyUnknown` /
`PathNotInMap` / `AmbiguousSolutionFile` / `AmbiguousRoot` → `ExtractionRunning` → `ExtractorNotFound`; a child over budget is
`ChildTimedOut`. There is no store stage. The gate is checked before resolution, so a disabled verb reveals nothing about the map.

## 6. Refusal texts (the bold phrases are asserted; 004 §6 for the kinds not listed)

| Kind | Text |
|---|---|
| `ScopeMissing` | Supply **solutionKey** — one map solution, exact; solutions lists the keys. Nothing was opened. |
| `SolutionKeyUnknown` | The CodeMem map at '{mapPath}' **holds no solution with key** '{key}'. Keys are exact; solutions lists them. To add a solution, run: extract --solution-key {key} --solution <path to its .sln or .slnx>. |
| `TargetMissing` | Supply **exactly one of solutionKey, repoPath or stale**; solutionPath only beside solutionKey. Nothing ran. |
| `PathNotInMap` (one file) | '{path}' is **not in the map**: no mapped solution's root contains it. Mapped roots: {roots}. It holds {file}; to add it, run: **extract --solution-key {key} --solution {file}** (the extract tool: solutionKey and solutionPath). Nothing ran and nothing was added. |
| `PathNotInMap` (no file) | '{path}' is **not in the map**: no mapped solution's root contains it, and it holds **no solution file**. Mapped roots: {roots}. To add a solution, run: extract --solution-key <key> --solution <path to its .sln or .slnx>. Nothing ran and nothing was added. |
| `AmbiguousSolutionFile` | '{path}' is not in the map and holds **more than one solution file** ({files}); no key is suggested. Choose one and run: extract --solution-key <key> --solution <that file>. Nothing ran and nothing was added. |
| `AmbiguousRoot` | '{path}' lies under a root shared by **more than one** mapped solution ({keys}); name the solutionKey. Nothing ran. |
| `ProjectIdRemoved` | **projectId is not an argument** of this bridge: scope by solutionKey (solutions lists the keys). MemOS's own codemem tools take a project id. Nothing was opened. |
| `ConfigKeyRetired` | '{key}' is **no longer a key** of '{configPath}': the bridge opens no store. Remove it and call again; nothing was opened. |
| `Unopenable` | The CodeMem map at '{path}' **could not be opened**: {driver text}. Check the path and its permissions. (the store variant is gone) |

`{roots}` lists every mapped root (`none` when the map holds no solution). `{file}` and `{files}` are full paths. The retired seven
kinds have no text; their 004 texts live in `_Archive/004-store/README.md`.

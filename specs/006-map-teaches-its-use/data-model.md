# Data Model: CodeMem 006 — The Map Teaches Its Own Use

**Date**: 2026-09-29 | **Spec**: [spec.md](spec.md) | **Research**: [research.md](research.md)

No table, column, index or schema version changes (Q11). Everything below is read from schema 3 as it stands, or is
text the bridge ships.

## 1. What is read

| Table | Columns read | By | Filter |
|---|---|---|---|
| `rename_candidates` | `id`, `solution_id`, `run_id`, `retired_symbol_id`, `new_symbol_id`, `same_path`, `offset_distance`, `rank` | `RenameCandidatesRepository.ReadCandidates` (new) | `solution_id`; optional `run_id`; optional `retired_symbol_id` |
| `extract_runs` | the columns `ExtractRunsRepository` already reads into `RunRecord` | `ReadById` (exists); `ReadBySolution`, `ReadRetiringRun` (new) | by id; by solution, `id DESC`; the first completed run after a run id |
| `code_symbols` (+ container and project names) | the columns `CodeSymbolsRepository.ReadById` already reads into `SymbolRecord` | `ReadById` (exists) | by id, active or not |
| `solutions` | key | `SolutionsRepository.ReadByKey` / `ReadById` (exist) | the scope; a foreign run's key |

`body_hash` is not read: the equality it records is the reason a row exists, and its value helps no reader.

## 2. The `rename_candidates` result (FR-502–FR-506)

Run 13 of the live map, as the tool answers `solutionKey "MemOS", runId 13`:

```json
{ "readAtUtc": "2026-…Z",
  "scope": { "by": "solutionKey", "solutionKey": "MemOS", "solutions": [ { "solutionId": 3, "key": "MemOS" } ] },
  "filters": { "runId": 13, "retiredSymbolId": null },
  "symbol": null,
  "runs": [
    { "runId": 13, "outcome": "completed", "finishedUtc": "2026-09-17T21:25:29.6054869Z",
      "symbolsRetired": 9, "renameCandidatesRecorded": 1, "candidatesReturned": 1,
      "countChecked": true, "countNotCheckedReason": null } ],
  "total": 1,
  "candidates": [
    { "runId": 13,
      "retired": { "id": 5339, "docCommentId": "F:MemOS.Core.Models.CodeMem.CodeMemMapContract.SupportedSchemaVersion",
                   "kind": "field", "name": "SupportedSchemaVersion",
                   "container": { "id": …, "name": "CodeMemMapContract" }, "project": { "id": …, "name": "MemOS.Core" },
                   "path": "MemOS.Core/Models/CodeMem/CodeMemMapContract.vb", "line": 34, "isActive": false },
      "new":     { "id": 23219, "docCommentId": "F:MemOS.Core.Models.CodeMem.CodeMemMapContract.MinimumSupportedSchemaVersion",
                   "kind": "field", "name": "MinimumSupportedSchemaVersion",
                   "container": { "id": …, "name": "CodeMemMapContract" }, "project": { "id": …, "name": "MemOS.Core" },
                   "path": "MemOS.Core/Models/CodeMem/CodeMemMapContract.vb", "line": 38, "isActive": true },
      "samePath": true, "offsetDistance": 284, "rank": 1 } ],
  "note": null }
```

And `retiredSymbolId 6754`:

```json
{ "readAtUtc": "…", "scope": { "…": "…" },
  "filters": { "runId": null, "retiredSymbolId": 6754 },
  "symbol": { "id": 6754, "docCommentId": "M:…PmWriteManager.#ctor(…)", "kind": "constructor", "name": "New",
              "container": { "id": …, "name": "PmWriteManager" }, "project": { "id": …, "name": "MemOS.Business" },
              "path": "MemOS.Business/Managers/PmWriteManager.vb", "line": …, "isActive": false,
              "lastSeenRunId": 52, "retiredInRunId": 53 },
  "runs": [
    { "runId": 53, "outcome": "completed", "finishedUtc": "2026-09-28T04:42:01.4615420Z",
      "symbolsRetired": 4, "renameCandidatesRecorded": 0, "candidatesReturned": 0,
      "countChecked": false, "countNotCheckedReason": "filtered by retiredSymbolId" } ],
  "total": 0,
  "candidates": [],
  "note": "No rename candidate names this symbol in the runs examined. A changed signature or body leaves no candidate by design (the body hash differs): search for the current symbol by name." }
```

**Field rules**:
- `filters` echoes the arguments as given; absent means `null`.
- `symbol` is `null` unless `retiredSymbolId` was given. When given, it is the named symbol, retired or active (Q6).
  `retiredInRunId` is set only when the symbol is retired now (R77); otherwise `null`.
- `runs` follows Q5's table, `runId` descending. `countNotCheckedReason` is `null` when checked, otherwise
  `"failed run"` or `"filtered by retiredSymbolId"`.
- `total` is the number of candidates returned.
- `candidates` are ordered `runId` descending, then `retired.id`, then `rank`, then `new.id`. One row is one candidate,
  never folded (Q1).
- `offsetDistance` is `null` when `samePath` is false (Article X; nulls are written, never omitted, as every bridge
  envelope does).
- `note` is `null` when `total > 0`. Otherwise it is the empty-answer sentence: the variant above under
  `retiredSymbolId`; with no filter or `runId` only, "No rename candidate was recorded in the runs examined. A changed
  signature or body leaves no candidate by design (the body hash differs)."

## 3. Candidate side (FR-502; Q1)

| Field | Source | Absent when |
|---|---|---|
| `id` | `code_symbols.id` | never |
| `docCommentId` | `doc_comment_id` | never |
| `kind`, `name` | `kind`, `name` | never |
| `container` | `container_id` + its name (`{ id, name }`) | the symbol has no container (`null`) |
| `project` | `project_symbol_id` + its name (`{ id, name }`) | a namespace or project row (`null`; neither is ever a candidate side in practice) |
| `path`, `line` | `path`, `start_line` of the primary declaration, as last observed | never |
| `isActive` | `is_active` **now** | never |

Twins are two candidates whose sides differ by `project` and `container.id` and `id` and agree on `name`, `path` and
`line`.

## 4. Examined run (FR-503)

| Field | Source |
|---|---|
| `runId`, `outcome`, `finishedUtc` | `extract_runs` |
| `symbolsRetired`, `renameCandidatesRecorded` | the run's recorded counts |
| `candidatesReturned` | the candidates in this result with this `runId` |
| `countChecked`, `countNotCheckedReason` | R78 |

## 5. Refusal kinds (FR-508; thirty-two)

005's twenty-nine, plus:

| Kind | Stage | Names |
|---|---|---|
| `RunNotFound` | question (run) | the map path and the run id |
| `RunOutOfScope` | question (run) | the run id, its own solution's key, the requested key |
| `CandidateCountMismatch` | question (count) | the run id, the requested key, the recorded count, the count found |

Order for `rename_candidates`: `ProjectIdRemoved` → `ScopeMissing` → configuration → map → `SolutionKeyUnknown` →
`RunNotFound` → `RunOutOfScope` → `SymbolNotFound` → `SymbolOutOfScope` → `CandidateCountMismatch`. `SymbolRetired`
is never raised by this tool (Q6). `SymbolRetired`'s own text changes for every other tool (contracts/tools.md §6).

## 6. The shipped texts (FR-511, FR-516, FR-517, FR-520)

| Text | Lives at | Source (spec Q3 table) |
|---|---|---|
| Server instructions | `BridgeServerInstructions.Text` (`src/CodeMem.Bridging/Mcp/BridgeServerInstructions.vb`, `eol=lf`) | 191490 §1 |
| The public snippet | `docs/claude-code/CLAUDE.snippet.md` | 191490 §2, first block |
| The `codemem` skill | `docs/claude-code/skills/codemem/SKILL.md` | 191490 §3 |
| README Quick start step 6 | `README.md`, after step 5 | 191490 §4 block |
| README step 5's last line | `README.md`, step 5's paragraph | 191490 §4 quoted line |
| README `rename_candidates` row | `README.md`, the tools table | 191490 §4 "Also" sentence |
| `symbol_search`'s added sentence | `BridgeToolDescriptions.SymbolSearch` | 191490 §1 note / 191497 #3 (spec FR-514) |
| `rename_candidates`'s description | `BridgeToolDescriptions.RenameCandidates` | contracts/tools.md §2 (STOP 1) |

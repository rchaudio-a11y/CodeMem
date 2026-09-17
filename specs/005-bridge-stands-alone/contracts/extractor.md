# Contract: the extractor after 005 — inputs, warnings, the summary line, schema version 3

**Date**: 2026-09-16 | **Spec**: [../spec.md](../spec.md) FR-421–FR-430 | **Research**: R61, R62, R63 | Amends 001's `contracts/cli.md`
and `contracts/schema.sql` where stated; everything else stands (exit codes 0–4, the lock, the counts, the ten-count summary).

## 1. Inputs

```text
CodeMem.Extractor --solution <path.sln|path.slnx|path.vbproj> --db <path to codemem.sqlite>
                  [--configuration Debug|Release] [--framework <tfm>] [--solution-key <name>]
```

| Extension | Opened by | Base directory | Default key |
|---|---|---|---|
| `.sln` | `OpenSolutionAsync` (unchanged) | the file's directory | file name without extension |
| `.slnx` | the loader's own parse (§2), then `OpenProjectAsync` per project into one workspace | the file's directory | file name without extension |
| `.vbproj` | `OpenProjectAsync` (unchanged) | the file's directory | file name without extension |
| anything else | refused before a workspace is created: `unsupported solution file: <path> (expected .sln, .slnx or .vbproj)`, exit 1 | — | — |

`last_seen_path` records the file passed, extension included; `repo_root` is the repository working directory when one holds the base
directory, as before.

## 2. The `.slnx` grammar the loader accepts

Data-model §10. In one sentence: a well-formed XML document whose root is `Solution`, every `Project` element at any depth with a `Path`
attribute naming a file that exists, resolved against the `.slnx` directory with forward slashes normalised; folders, configurations,
properties and `Type` attributes are not read. Refusals, all exit 1 with one stderr line and nothing written:

| Condition | Line |
|---|---|
| not well-formed XML | `solution file is not well-formed XML: <file>: <parser message>` |
| root is not `Solution` | `solution file has no Solution root: <file>` |
| no `Project` element with a `Path` | `solution file names no project: <file>` |
| a `Path` that does not exist | `solution file names a project that does not exist: <path> (in <file>)` — checked for every project before any is opened |

Projects are opened in document order; a path the workspace already holds (loaded as another project's reference) is not opened
again. After the last open, `loader.Solution` is the workspace's current solution and everything downstream is unchanged.

**Parity** (FR-422, SC-406): the same projects named by a `.sln` and a `.slnx` yield identical canonical fact sets in two fresh maps.

## 3. Restore warnings and errors (the loader's rule; data-model §11)

A `WorkspaceFailed` event of kind `Failure` whose text has the shape `Msbuild failed when processing the file '<project>' with message:
<text>` is looked up in `<project dir>/obj/project.assets.json` → `logs[]` by `message == <text>`:

| Assets entry | Action | Where it shows |
|---|---|---|
| `level: Warning` (NU1701, NU1603, …) | recorded; the load continues; the compiler judges the code as always | `extract_run_warnings` rows; `warnings=N` on the summary line |
| `level: Error` (NU1101, NU1301, …) | `workspace load failed: restore error <code> in <project>: <text>`, exit 1, no compilation requested | stderr, one line |
| no matching entry, or no assets file | `workspace load failed: <text>`, exit 1, as today | stderr |

Any Failure that does not have the `Msbuild failed …` shape aborts as today. The extractor reads no environment variable for this and
passes no `NoWarn`; a run under `NoWarn=NU1701` in the environment is not different from one without (the fact asserts the child's
environment is clean). Compile errors are unchanged: exit 2, `errors=N`, nothing written.

## 4. The summary line and the usage text

The summary line gains one trailing field: `… digest=<sha256> sha=<commit|null> warnings=<n>` (the number of rows written for the run;
`0` when none). The exit-4 line carries the same field after its prefix. The bridge parses `run_id=` only and reports the line verbatim,
so the addition is invisible to it except through the map.

Usage gains, after the exit-code line:

```text
inputs: a .sln, a .slnx (opened by CodeMem's own parse; folders and configurations are not read) or a .vbproj
failure classes: "workspace load failed" — the solution could not be opened or restored (exit 1); "errors=N" — the compiler found
                 errors (exit 2). A NuGet restore WARNING (NU17xx and the like) is recorded on the run and does not fail the load;
                 a restore ERROR (NU11xx, NU13xx) does.
```

## 5. Schema version 3 — MIGRATION 2 → 3 (appended to `contracts/schema.sql`'s history; the version-1 and version-2 texts are frozen)

```sql
-- MIGRATION 2 -> 3 (feature 005, FR-429): restore warnings the load tolerated, one typed row each (Article X).
-- A row is written right after its run row, completed or failed; never for a run that wrote no row (exit 1, exit 2).
CREATE TABLE extract_run_warnings (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    run_id          INTEGER NOT NULL REFERENCES extract_runs(id),
    code            TEXT    NOT NULL,               -- NuGet's code, e.g. NU1701
    project_path    TEXT    NULL,                   -- solution-relative path of the project file the warning names; NULL when it names none
    message         TEXT    NOT NULL
);
CREATE INDEX ix_extract_run_warnings_run_id ON extract_run_warnings(run_id);
-- then, after the DuringUpgrade seam: UPDATE map_identity SET schema_version = 3 WHERE id = 1;
```

`SchemaVersion.Current = 3`. A fresh map is created at 1 and upgraded 1 → 2 → 3 in one transaction; a version-1 or version-2 map is
upgraded in place on its next run; a version above 3 is refused (`schema version mismatch`, exit 1); the fresh path and both upgrade
paths yield byte-identical schemas.

**Core API** (`ExtractRunWarningsRepository`): `InsertAll(db, runId, warnings)` (the only writer; called by `ExtractionRun` after
`InsertCompleted` / `InsertFailed`), `ReadByRun(db, runId)` (SELECT-only, for the bridge and the tests). `RunWarningRecord`: `Id`, `RunId`,
`Code`, `ProjectPath|Nothing`, `Message`.

## 6. What does not change

Exit codes; the lock and exit 3; the residual rule and exit 4; the ten counts; `--solution-key` never defaulted by the bridge (the
extractor still defaults it to the file name when a human omits it); the source digest and the scope rule; the abort seams and their
nonce; `--configuration` and `--framework` as global properties.

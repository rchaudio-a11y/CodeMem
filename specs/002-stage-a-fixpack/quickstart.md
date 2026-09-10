# Quickstart: CodeMem Fixpack 002

Validation guide for the eight corrections and the schema-2 upgrade. Schema:
[contracts/schema.sql](contracts/schema.sql); command line: [contracts/cli.md](contracts/cli.md);
Stage A's quickstart still applies for build, hand extraction and the original refusals.

## Prerequisites

- Windows 11, .NET SDK 9 or 10 (10.0.401 observed), .NET 8 runtime and WindowsDesktop runtime.
- No other tooling (Article XV). The SDK version stamp is read in-process from the .NET host.

## Build and test

```powershell
dotnet build CodeMem.sln -c Debug
dotnet test tests/CodeMem.Tests/CodeMem.Tests.vbproj
```

Expected: 0 errors; Stage A's 38 tests plus this feature's `Fixpack/` tests green; `AcceptanceRunner`
Skipped.

Observed 2026-09-10 on the finished tree (T045; `dotnet build` 0 warnings, 0 errors):

```text
  Skipped CodeMem.Tests.AcceptanceRunner.ReportOnTheNamedSolution
Passed!  - Failed:     0, Passed:    59, Skipped:     1, Total:    60, Duration: 1 m 35 s - CodeMem.Tests.dll (net8.0)
```

38 Stage A facts + 22 new: F02 (1), F04_SdkVersion (2), F04_BuildInputs (1), F05 (1), F06 (3), F08 (5),
F09 (4), S02 (4), and SchemaConstraintTests gained the upgraded-map trigger fire (1).

## Dependency audit (F3)

```powershell
dotnet list CodeMem.sln package --vulnerable --include-transitive
```

Expected after the pins (`Microsoft.Build.Tasks.Core` 17.8.43, `System.Formats.Asn1` 8.0.1):

```text
The given project `CodeMem.Core` has no vulnerable packages given the current sources.
The given project `CodeMem.Extraction` has no vulnerable packages given the current sources.
The given project `CodeMem.Extractor` has no vulnerable packages given the current sources.
The given project `CodeMem.Tests` has no vulnerable packages given the current sources.
```

Observed on the scratch audit 2026-09-10 (research R25): no vulnerable packages. Observed on the solution
itself, 2026-09-10 (T002; re-run at T041 on the finished tree, see below):

```text
  Determining projects to restore...
  All projects are up-to-date for restore.

The following sources were used:
   https://api.nuget.org/v3/index.json
   C:\Program Files (x86)\Microsoft SDKs\NuGetPackages\

The given project `CodeMem.Core` has no vulnerable packages given the current sources.
The given project `CodeMem.Extraction` has no vulnerable packages given the current sources.
The given project `CodeMem.Extractor` has no vulnerable packages given the current sources.
The given project `CodeMem.Tests` has no vulnerable packages given the current sources.
```

`dotnet build CodeMem.sln` after the pins: 0 warnings, 0 errors; the Stage A suite stayed at 38
(37 passed, 1 skipped) before any source change (FR-120).

**Finding (FR-119)**: the out-of-process build host (`BuildHost-netcore/`) ships only
`Microsoft.Build.Locator.dll` and loads MSBuild from the installed SDK, so the pins govern the extractor
process and cannot govern the build host; the build host runs the SDK's own MSBuild (17.x from SDK
10.0.401, patched upstream). Recorded, not forced.

Observed 2026-09-10 (T003, after the pins): `tests/CodeMem.Tests/bin/Debug/net8.0/BuildHost-netcore/`
lists exactly one MSBuild assembly, `Microsoft.Build.Locator.dll`. The pinned
`Microsoft.Build.Tasks.Core.dll`, `Microsoft.Build.Utilities.Core.dll` and `Microsoft.Build.Framework.dll`
(17.8.43) sit in the extractor's own output directory only. The SDK the build host loads MSBuild from is
the one the host resolver selects for the solution directory: 10.0.401 on this machine (`dotnet --list-sdks`:
9.0.308, 10.0.401).

## Upgrade a version-1 map (schema 2)

Never run first against the only copy. Copy, then extract into the copy:

```powershell
Copy-Item C:\_DB\codemem.sqlite $env:TEMP\codemem-v1-copy.sqlite
dotnet run --project src/CodeMem.Extractor -- `
  --solution <the solution that map was built from> `
  --db $env:TEMP\codemem-v1-copy.sqlite
```

Expected: exit 0; one summary line; `map_identity.schema_version` reads 2; every pre-existing row of
every table is still present; `extract_runs.sdk_version` is NULL on the old rows and e.g. `10.0.401` on
the new one. Inspect:

```sql
SELECT schema_version FROM map_identity;
SELECT id, schema_version, sdk_version FROM extract_runs ORDER BY id;
SELECT type, name FROM sqlite_master WHERE name LIKE 'tr_%';      -- the two triggers exist
```

**Observed 2026-09-10 (T046)** on a copy of `C:\_DB\codemem.sqlite` (a Stage A map of `GameRoom.sln`, one
completed run, 2172 symbols / 2178 parts / 22652 edges, schema version 1, no trigger), extracted with the
built extractor (`dotnet src/CodeMem.Extractor/bin/Debug/net8.0/CodeMem.Extractor.dll --solution
C:\Users\rchau\source\repos\GameRoom\GameRoom.sln --db $env:TEMP\codemem-v1-copy.sqlite`):

```text
exit 0 in 6.2 s
solution=GameRoom run_id=2 observed=2172 matched=2172 reactivated=0 new=0 retired=0 registry_before=2172 notes_orphaned=0 candidates=0 unaccounted_observed=0 unaccounted_registry=0 digest=9883407a... sha=e1bbe024...
schema_version: 1 before -> 2 after
extract_runs: id 1 schema_version 1 sdk_version NULL | id 2 schema_version 2 sdk_version 10.0.401
code_symbols 2172 (2172 active), code_parts 2178, code_edges 22652: unchanged counts
sqlite_master: tr_extract_runs_sdk_version_insert, tr_extract_runs_sdk_version_update
C:\_DB\codemem.sqlite itself: untouched (schema_version still 1, same size and timestamp)
```

The original is upgraded by the Operator's next real run against it.

The same check runs automatically against the committed version-1 fixture
`tests/CodeMem.Tests/Fixtures/Maps/sample-v1.sqlite` (S02 tests), including `sqlite_master` equality
with a fresh map.

## Prove the corrections by hand

| Item | Command | Expected |
|------|---------|----------|
| F2 kind refresh | change a class to a structure in a fixture copy; extract twice | same `id`, `kind = 'structure'`, `symbols_matched` counts it |
| F5 `;` path | `--db "$env:TEMP\a;b\codemem.sqlite"` (directory exists) | exit 0; the file is at that path |
| F6 0-byte file | `New-Item $env:TEMP\empty.sqlite; extract --db` that file | exit 0; a complete map |
| F6 foreign file | a SQLite file with a table `t` at `--db` | exit 1, `not a map: <path>`, file byte-identical |
| F9 invalid path | `--db " "` and `--db "$env:TEMP\ma<p>.sqlite"` | exit 1, one stderr line, no stack trace |
| F8 nonce | `$env:CODEMEM_TEST_ABORT_AT='DuringPublish:x'` alone | run completes, exit 0 |
| F8 nonce | plus `$env:CODEMEM_TEST_NONCE='x'` | `Process terminated. CODEMEM_TEST_ABORT_AT=DuringPublish`, non-zero exit, map unchanged |
| F4 build files | place an empty `Directory.Build.props` above a fixture copy; extract twice | different `source_digest`, same fact set |
| F4 sdk stamp | any run | `sdk_version` equals `dotnet --version` run in the solution directory |
| F10 gates | inject `delete   from code_symbols` (lowercase) into a repository file; run `dotnet test --filter Tripwire` | red; revert → green |

Observed 2026-09-10 (T047), each row run by hand with the built extractor
(`dotnet src/CodeMem.Extractor/bin/Debug/net8.0/CodeMem.Extractor.dll …`) against the committed fixture:

| Item | Observed |
|------|----------|
| F2 kind refresh | by test F02 (three runs, class → structure → module, one id); not repeated by hand |
| F5 `;` path | `--db "$env:TEMP\codemem-hand\a;b\codemem.sqlite"` → exit 0; `codemem.sqlite` inside `a;b\`; no file named `a` |
| F6 0-byte file | exit 0; `schema_version` 2; one run |
| F6 foreign file | `CREATE TABLE t (x)` file → exit 1; stderr `not a map: C:\…\foreign.sqlite`; SHA-256 identical before and after; no journal |
| F9 `--db " "` | exit 1; one line `The path is empty. (Parameter 'path')` |
| F9 `--db …\ma<p>.sqlite` | exit 1 (not 3); one line `database error: SQLite Error 14: 'unable to open database file'.` |
| F9 `--db <directory>` | exit 1; one line `db path is a directory: C:\…\codemem-hand` |
| F8 `CODEMEM_TEST_ABORT_AT=DuringPublish:x` alone | exit 0, run completed |
| F8 plus `CODEMEM_TEST_NONCE=x` | `Process terminated. CODEMEM_TEST_ABORT_AT=DuringPublish`; exit 0x80131623 (35 in an 8-bit shell); the fresh file has no user table (the run rolled back) and an unsynced `-journal` beside it, as I9 documents |
| F4 build files | by test F04_BuildInputs (digest differs on runs 2 and 3, facts equal); not repeated by hand |
| F4 sdk stamp | `sdk_version` = `10.0.401` = `dotnet --version` in the fixture directory |
| F10 gates | T044: lowercase `delete   from code_symbols where id = @id` → Tripwire red; lowercase `select 1` in ExtractionRun.vb → SqlLocation red; both green on revert |
| `--help` | both variables and the both-required rule under `test-only environment variables:` |

## Conventions to carry into implementation

- Every new `.vb` file: header block and XML docs (Stage A gates enforce both).
- Tests for this feature live in `tests/CodeMem.Tests/Fixpack/`, one file per review item, each with
  RED / GREEN / FIRE lines in its header.
- The version-1 map fixture is produced by the Stage A extractor before any source file changes, and
  committed as a binary.

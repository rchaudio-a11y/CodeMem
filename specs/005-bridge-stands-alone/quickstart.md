# Quickstart: CodeMem 005 — The Bridge Stands Alone

**Date**: 2026-09-16 | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Contracts**: [contracts/](contracts/)

A validation guide: what to build, which facts prove each story, and the live steps in the order they are safe. Implementation lives
in `tasks.md` and the code.

## Prerequisites

- Windows 11, .NET SDK 10.0.401 (the one on this machine; the projects target `net8.0`), `git` and LibGit2Sharp as today.
- The repository on branch `005-bridge-stands-alone` from `main` at `90d62ca`; constitution **v1.4.0** at T003 (the store code leaves at T024 — analyze C3).
- No network: the warning fixtures restore from the local package folder beside them (research R71); the Sample fixture as before.
- For the live steps: `C:\_DB\codemem.sqlite` backed up first; `memos.sqlite` is not needed by anything and is renamed for one check.

## Build and test

**T001 (2026-09-17)**: branch `005-bridge-stands-alone` from `90d62ca`; the artefacts committed as `ff0ce54`; `dotnet build` 0 / 0; `dotnet test` **144 passed / 1 failed / 8 skipped (2 m 27 s)** — S02 (2) `UpgradedMapHasTheSameSchemaObjectsAsAFreshMap` red because the 004 merge checkout had rewritten `src/CodeMem.Core/Schema/SchemaRepository.vb` to CRLF under `core.autocrlf=true` (the 004 record's "must stay LF" trap): the DDL literals' line endings become the sql text in `sqlite_master`, and the version-1 fixture map was created from LF text. Fixed on the branch by `.gitattributes` (`text eol=lf` for that file; `*.sqlite` and `*.nupkg` binary) and normalising the file; S02 4 / 4 green; the whole suite re-run at T011.

```powershell
dotnet build CodeMem.sln -c Release --nologo -v q
dotnet test --nologo -v q                                   # the whole suite; baseline 145 passed / 8 skipped / 2 m 33 s before 005
dotnet test --nologo -v q --filter "FullyQualifiedName~Bridge.B09|FullyQualifiedName~Bridge.B10|FullyQualifiedName~Bridge.B11"
dotnet test --nologo -v q --filter "FullyQualifiedName~Extraction.X0"       # .slnx and warnings
dotnet test --nologo -v q --filter "FullyQualifiedName~Fixpack.S03"         # schema version 3
dotnet test --nologo -v q --filter "FullyQualifiedName~Guards"              # every gate, the amended connection-site gate included
$env:CODEMEM_LIVE_MAP = "C:\_DB\codemem.sqlite"; dotnet test --nologo -v q --filter "FullyQualifiedName~B08"   # armed live facts
```

## Fixture validation (what the new facts do; runnable by hand against the built bridge)

| Story | Fact | Expected |
|---|---|---|
| US1 one database | B09 (1)–(5): every tool against a fixture map with no store file anywhere and no `storePath`; `projectId` on each tool; `storePath` in a config; the gate over the two bridge projects (+ FIRE) | every call answers; `ProjectIdRemoved` ×8; `ConfigKeyRetired` naming `storePath`; 0 `SqliteConnection`, 0 path-shaped (case-sensitive) `memos`; `MapDatabase.vb` the one site |
| US2 resolve from the map | B05 (amended) + B10 (1)–(8): a root, a subdirectory, nested roots, a shared root, a NULL-root solution's directory (Q6); a directory with one `.sln`, one `.slnx`, both, two `.sln`, none; hash before/after; 0 launches | the key; the longest root; `AmbiguousRoot`; the base-directory fallback resolves; `PathNotInMap` with `extract --solution-key <key> --solution <file>` in the text / with placeholders; `AmbiguousSolutionFile` naming both; map byte-identical |
| US2 hook | B06 (amended): `dotnet build X.slnx` → X's directory; an unmapped build → the answer line carries the command; exit 0 | as stated |
| US3 add a solution | B10 (9)–(12): `--solution-key Fresh --solution <fixture copy>` on a copy of a fixture map (one real launch); the same call again; `--solution-key Fresh` alone; `--solution` alone; the gate matrix for the shape; one log line each | exit 0, one new row, ten balanced counts; matched > 0 and new = 0, still one row; `SolutionKeyUnknown` with the add remedy; `TargetMissing`; the five gate cells; `solutionKey=Fresh solution=<path>` in the log |
| US3 `not_in_map` | B11 (1)–(6): refuse a directory, call `map_status`; refuse it twice; a fresh `BridgeTools` instance (a "restarted server"); add the solution, call again; a log with a 004-era `PathNotRegistered` line; an unreadable log | listed once with key and command; still once; still listed; gone, the log line intact; the old line ignored; `notInMapError` set, entries intact |
| US4 `.slnx` | X01 (1)–(9): `Sample.sln` vs `Sample.slnx` into two fresh maps; key default; `last_seen_path`; a missing project path; malformed XML; no `Solution` root; no project; a `<Folder>`-nested project and forward slashes; `.txt` extension; the usage text | identical fact sets (≥ 1 S/P/E line); `Sample` both; `.sln` / `.slnx` recorded; exit 1 naming the path before any open; the four refusals by name; nested and slashed paths open; `unsupported solution file`; the three inputs named |
| US5 warnings | X02 (1)–(6): the NU1701 fixture; the same with a compile error; the NU1101 fixture; `NoWarn` absent from the child's environment (asserted); the summary line; `solutions` and `extract` carry the rows | exit 0, ≥ 1 row `NU1701` naming `Nu1701/Nu1701.vbproj`; exit 2, 0 rows; exit 1, stderr names `NU1101` and the project, no `errors=` line; `warnings=1`; counts and rows on the wire |
| Schema 3 | S03 (1)–(6): fresh at 3; a version-1 map → 3; a version-2 map → 3; abort `DuringUpgrade` on a version-2 map; fresh vs upgraded schema text; warnings on a failed (exit 4) run | 3; 3 with rows kept; 3; still 2 with the table present, the next run completes it; byte-identical; rows beside the failed run row |
| Governance | `BridgeStandaloneGateTests` (1)–(4): Article IX body vs the v1.2.1 transcription; the version line; archived names absent under `src/` and `tests/`; `SqlLocationGateTests` one file (+ FIRE) | equal; `1.4.0`; 0 hits; `["MapDatabase.vb"]` |
| MemOS untouched | the 004 project-file gate; `git -C …\rchaudio-a11y\MemOS status --porcelain` before and after | green; empty both times |

## Live steps (after implementation; on a copy first, the live map only at the Architect's request — R73)

1. **Backup**: copy `C:\_DB\codemem.sqlite` to `C:\_DB\codemem.sqlite.bak-<date>-pre-005`; hash both; record.
2. **Config**: remove `storePath` from `bridge.config.json` beside the Release executable; both gates off. Call `solutions` through the
   Release executable: expect `VersionBelow` naming version 2 and the remedy — the honest first line of the record.
3. **On a copy of the map** (`--config` pointing at a copy): `extract.enabled` on; `CodeMem.Bridge extract --solution-key DSP_Processor
   --solution C:\Users\rchau\source\repos\DSP_Processor\DSP_Processor.slnx` → exit 0, the copy now at schema 3, `last_seen_path` ends in
   `.slnx`, counts balance; the same for RicksLife with nothing in the environment (`Get-ChildItem env:NoWarn` empty) → exit 0,
   `warnings=3` (OpenTK, OpenTK.GLControl, SkiaSharp.Views.WindowsForms). `solutions` and `map_status` on the copy: five entries, no
   registry fields.
4. **The live map, at the Architect's request**: steps 3's two extractions against `C:\_DB\codemem.sqlite` (the DSP_Processor run
   upgrades it to 3); record both summary lines and the map's hash after.
5. **From the map alone**: rename `memos.sqlite` for the duration (the Shell closed); `extract --repo-path
   C:\Users\rchau\source\repos\DSP_Processor` → resolved key `DSP_Processor`, launched from the `.slnx` (or refused `ExtractionRunning`
   / by a gate if so configured — whichever, the resolution line names the key); `extract --repo-path C:\Users\rchau\source\repos\vbCalc`
   (or any unmapped repository holding one `.sln`) → `PathNotInMap` with the command; `map_status` → the directory under `notInMap`;
   nothing added, the map's hash unchanged by the refusal. Restore the file's name.
6. **The Operator, outside this repository**: delete `DSP_Processor.sln` (a commit in that repository); re-point the two registry rows'
   `extraction_scope` at the `.slnx` in MemOS. Then `extract --repo-path <DSP_Processor root>` once more: it runs from the `.slnx`.
7. **MemOS**: `git status --porcelain` in `rchaudio-a11y\MemOS` before step 1 and after step 6 — empty both times; nothing in this feature
   opened `memos.sqlite`.

### Record (filled at implementation)

| Step | Expected | Observed (date, figures) |
|---|---|---|
| 1. backup + hash | equal hashes | |
| 2. `solutions` at pin 3 vs a version-2 map | `VersionBelow` | |
| 3. copy: DSP_Processor from `.slnx` | exit 0, schema 3, `.slnx` recorded | |
| 3. copy: RicksLife from `.slnx`, no NoWarn | exit 0, `warnings=3` | |
| 4. live: the two runs | as 3 | |
| 5. `--repo-path` DSP_Processor, store renamed | key `DSP_Processor` | |
| 5. `--repo-path` unmapped + `map_status` | `PathNotInMap` with the command; listed | |
| 6. after the `.sln` is deleted | runs from the `.slnx` | |
| 7. MemOS `git status` | empty, twice | |
| suite | green, under 5 min | |

## What must not happen

- The bridge opens any file but the map: a second `SqliteConnection` site, a path containing `memos`, a `storePath` read.
- A solution is added by anything but the Architect's `--solution-key … --solution …`: no auto-add, no key chosen, no directory walked.
- An `extract` refusal launches anything or changes the map's hash.
- A `NoWarn` in the environment makes any difference; a restore error reaches the compiler.
- The log is rewritten, truncated or rotated by the bridge.
- A file in the MemOS repository changes; a registry row is written by this feature's code.

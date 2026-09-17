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

**T011 (2026-09-17)**: after schema version 3 (T007–T010): `dotnet test` **152 passed / 2 failed / 8 skipped (2 m 55 s)** — the two failures are `BridgeStandaloneGateTests` (1) and (2), red as named until the archive at T024. Named Reds observed and amended in place: F06 ×2, S02 (1) (3) (4), US1, B02 (the pin; `schemaVersion` 2 → 3; the hand-bump 3 → 4); one Red the plan did not name, `RefusalTests.SchemaVersionMismatchIsRefused` (the same shape as S02 (3): its literal 3 is current now; stamps 4). Note: the `--filter` lines below use class-name prefixes (`FullyQualifiedName~CodeMem.Tests.B09`), because the test namespace is flat; the folder-name filters first written here matched nothing.

**T025 (2026-09-17)**: after US2 (T019–T024): `dotnet build CodeMem.sln` 0 / 0 with the eight store files under `_Archive/004-store/` (the build is the proof nothing references them); `dotnet test` **165 passed / 0 failed / 9 skipped (2 m 38 s)** — the 9 skipped are B08's eight (unarmed) and the acceptance runner. `FileHeaderGateTests`, `MemOsReferenceGateTests`, `SqlLocationGateTests` (the connection-site fact at one file after its named Red) and `BridgeStandaloneGateTests` (1)–(4) green. CON3 through the executable: B10 (9) `TheNotInMapAnswerReachesTheCommandLine` runs `CodeMem.Bridge.exe extract --repo-path <temp dir holding Fresh.slnx> --config <fixture config>` → exit 2, `refusal.kind` `PathNotInMap`, `extract --solution-key Fresh --solution …` in `refusal.text`, `launched` false. Reds named and observed on the way: B10 (1)–(9) at T019 (every door fact refused `RegistryAbsent` while the door read a store no configuration names), B05 (18) at T022 (`ConfigKeyRetired` on the shipped sample), `SqlAppearsOnlyInRepositories` at T023 (the registry fixture's DDL once its exclusion was dead), `OnlyMapDatabaseOpensAConnection` at T024 (expected two files, actual `["MapDatabase.vb"]`). One test-side amendment not in the plan: B10's not-in-map helper asserted the directory against the wire JSON, where backslashes are escaped; it reads `refusal.text` now. Fires: B10 (8) and (4) at T021; the four archive probes at T024.

**T032 (2026-09-17)**: after US3 (T026–T029) and US4 (T030–T031): `dotnet test` **190 passed / 0 failed / 9 skipped (2 m 38 s)** — I02 and the 003 suites unchanged; X01 (1)–(10) green on the loader's own `.slnx` parse (route (b)); B10 (10)–(16) and B11 (1)–(8) green over the shared `AddedSolutionFixture` (one real launch per run of the ExtractLog collection). Reds named and observed: B10 (10)–(15) and B11 at T026 (the build: `BC30456 'SolutionPath' is not a member of 'ExtractRequest'`, three sites), B11 8 of 8 at T027 (no reader), X01 10 of 10 at T030 (`InvalidProjectFileException: No file format header found` on the `.slnx`; the workspace's own `.txt` text; the usage without the inputs). Fires: B11 (3) at T028 (a per-instance birth stamp — (3), (4), (5), (7) red); X01 (1) at T031 (first project only — 25 observed against 38).

**T037 (2026-09-17)**: after US5 (T033–T036): `dotnet test` **197 passed / 0 failed / 9 skipped (3 m 0 s)**. X02 (1)–(7) green: a NU1701 is recorded on the run (one `extract_run_warnings` row, `warnings=1` on the summary line) and the load continues; NU1101 stops the load by name before the compiler; a compile error still exits 2; an unmatched failure aborts as before; the failed run row carries its warning; the bridge reports `run.warnings` and `latestRun.warnings`. Reds observed: X02 6 of 7 at T033 (`workspace load failed: | Msbuild failed … Package 'CodeMem.NetFxOnly 1.0.0' was restored using '.NETFramework…'`), `RefusalTests` (d) at T035 as named (the anchored summary regex), and two the plan did not name — `US1_FirstRunTests` (1) and `R02_BareNameTests` (3) pin the extractor version literal (`0.2.0` → `0.3.0`, the same shape as 003's) — amended with dated lines. **Not red as named**: B05 (2) — its summary line is scripted into the launcher and echoed verbatim, never the extractor's; the scripted string now carries `warnings=0`. **Deviation (for the Architect)**: X02 (4) as specified ("NoWarn in the environment changes nothing") is false in MSBuild's hands — with `NoWarn=NU1701` in the child's environment the SDK folds it into `MSBuildWarningsAsMessages`, NuGet's replay never reaches the loader, and the run exits 0 with **no** warning row (observed twice, before and after T035). The fact is rewritten to assert that observation against the clean launch's row; the extractor itself reads no such variable and passes no `NoWarn` (R62 holds), which is why every launch in the suite scrubs `NoWarn*` first and why an Operator's shell must not carry the 152705 workaround when the bridge launches the extractor. Fires: X02 (3) at T036 (every matched entry a warning — NU1101 continued to the compiler, exit 0).

**T040 (2026-09-17, partial)**: after T038 (the process document) and T039 (bridge 0.2.0; B01 (5) reads `serverInfo.version` 0.2.0 over stdio): `dotnet test` on the Debug build **197 passed / 0 failed / 9 skipped (2 m 53 s)**; every new test file carries at least one `' FIRE:` line (B09, B10, B11, X01, X02, S03 one each; BridgeStandaloneGateTests two). `dotnet build CodeMem.sln -c Release` **did not complete**: MSB3021/MSB3027 copying `CodeMem.Bridging.dll`, `CodeMem.Core.dll` and `CodeMem.Extraction.dll` into `src/CodeMem.Bridge/bin/Release/net8.0/` — the files are held by PID 86044, `CodeMem.Bridge.exe serve`, the session's own `codemem` MCP server running from that folder since 2026-09-16 21:24. The Release build, the Release suite and the live steps (T041–T044) wait for that server to be stopped or reconnected; nothing else is outstanding before them.

**T040 (2026-09-17, complete)**: the Architect said "stop the process"; PID 86044 stopped; `dotnet build CodeMem.sln -c Release` **0 errors / 0 warnings** (two B10 summaries that quoted `<path>` and `<its .sln>` literally were reworded first — ten BC42304 XML-doc warnings); `dotnet test -c Release` **197 passed / 0 failed / 9 skipped (3 m 1 s)**, under the five-minute bound.

```powershell
dotnet build CodeMem.sln -c Release --nologo -v q
dotnet test --nologo -v q                                   # the whole suite; baseline 145 passed / 8 skipped / 2 m 33 s before 005
dotnet test --nologo -v q --filter "FullyQualifiedName~CodeMem.Tests.B09|FullyQualifiedName~CodeMem.Tests.B10|FullyQualifiedName~CodeMem.Tests.B11"
dotnet test --nologo -v q --filter "FullyQualifiedName~CodeMem.Tests.X0"       # .slnx and warnings
dotnet test --nologo -v q --filter "FullyQualifiedName~CodeMem.Tests.S03"         # schema version 3
dotnet test --nologo -v q --filter "FullyQualifiedName~GateTests|FullyQualifiedName~Tripwire|FullyQualifiedName~RefusalTests"              # every gate, the amended connection-site gate included
$env:CODEMEM_LIVE_MAP = "C:\_DB\codemem.sqlite"; dotnet test --nologo -v q --filter "FullyQualifiedName~CodeMem.Tests.B08"   # armed live facts
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

**T041 (2026-09-17)**: steps 1–3 done on a copy. One deviation from step 3's wording: the bridge's `extract` refuses a version-2 map with `VersionBelow` for every target shape, the by-key-with-path one included, because the door opens the map (and checks the pin) before it resolves — the order contracts/tools.md §6 gives. So the copy's first upgrade ran through `CodeMem.Extractor.dll` directly (`--solution …DSP_Processor.slnx --db <copy> --solution-key DSP_Processor`), which is exactly the refusal's remedy; the second extraction (RicksLife) then went through the bridge. Step 4 on the live map will have the same shape: the extractor first, then the bridge. Also worth the Architect's eye: `map_status` over five repositories took 3.6 s on the copy (004's SC-301 measured 3 s for one call; the working trees are dirty and MemOS is behind), while `solutions` took 62 ms.

**T042/T043 (2026-09-17)**: live steps 4 and 5 done, in the order T041 predicted — the extractor first, then the bridge. One Red, diagnosed
and resolved at the Architect's direction: B08 `LiveMapStatusReportsMemOsBehind` asserted MemOS `current` with `behindBy` 0, and the live
map reported `behind` — HEAD `bf275596` was **2 commits past** the recorded `cbeb6615`, tree clean. The two commits are MemOS's own
`16a1949` (the fixpack that widened its map-schema pin to accept version 3) and `bf27559` (its merge), made minutes earlier so that MemOS
could read a version-3 map at all. So the figure moved for a known reason and the fact is right to fire — its comment says as much ("the
next MemOS commit turns this red, to diagnose"). **The remedy was not a re-pin.** Ruled 2026-09-17 (Architect): re-extract MemOS — wanted for the MemOS fixpack's P008 regardless —
**and** change the fact so it stops asserting a transient. MemOS run 13 records `bf275596` (observed 15,824, matched 15,783, new 41,
retired 9, 1 rename candidate, balanced), and the fact now recomputes HEAD, dirtiness, ancestry and distance from git itself and asserts
the verdict those inputs imply under `MapStatusReader`'s precedence, so it holds at any staleness (`LiveMapStatusVerdictsAgreeWithGit`).
All five entries compared and agreed: `current` ×2, `dirty` ×3, one of them dirty WITH `behindBy` 20, which exercises the
dirty-before-behind precedence. The re-extraction then moved (7)'s absorbed-twin count 277 → 279; diagnosed to the two members the
fixpack added to `CodeMemMapFixture.vb`, a file linked into MemOS's integration project as well, so each folds as one twin pair —
15,824 active − 279 = 15,545 presented, exactly the figures reported. Re-pinned with that arithmetic at the assertion. **B08 8/8.** The live map's hash chain across the whole window, every step measured at disk: `fdac5897…` (pre-005, equal to the T041 backup)
→ `6c4ade46…` (T042's two runs) → `26dfbacf…` (T043's run 12, and **unchanged across the PathNotInMap refusal**) →
`43b7eb74…` (MemOS run 13), 81,473,536 B. `C:\_DB\memos.sqlite` is `11c69aad…` at the end, byte-identical to the snapshot taken
before the rename: nothing in this feature opened it.

**Install and the gates (2026-09-17, at the Architect's request)**: README Install steps 4-6 checked and completed.
**Step 4 was already done** - `~/.claude.json` top-level `mcpServers.codemem` already held the user-scope entry with the absolute
path, so the bridge already answered from every repository; the direct evidence is that `solutions`, `map_status` and `extract` were
all called from a **MemOS** session during T042/T043, not from CodeMem's folder. **Step 5 done**: the committed
`hooks/settings.fragment.json` merged verbatim into `~/.claude/settings.json` (PostToolUse / matcher `Bash`, timeout 600), the two
pre-existing keys preserved; proved live - the next Bash call returned `codemem hook: not a dotnet build or test; nothing ran`.
**Step 6 done**: `extract.enabled` then `extract.onGreenBuild` both set true in the Release `bridge.config.json`, superseding step 2's
"gates off" line; verified through the already-running `serve` process, which answered `gate: passed` on an unmapped `--repo-path`
(`launched` false, nothing ran) - the config is read per call and never cached, so **no restart was needed**. The running bridge was
checked and is **not** an old build: zero files under `src/**/*.vb` are newer than the Release executable it runs.
⚠ **Consequence now live**: a `dotnet build` or `dotnet test` in any mapped repository triggers an extraction that writes the map
and holds it exclusively for the run (~8 s DSP_Processor, ~26 s MemOS); concurrent `codemem_*` reads refuse `Busy` in that window.

### Record (filled at implementation)

| Step | Expected | Observed (date, figures) |
|---|---|---|
| 1. backup + hash | equal hashes | 2026-09-17: `C:\_DB\codemem.sqlite.bak-2026-09-17-pre-005`; SHA-256 `fdac5897…c5404a` for both (80,982,016 bytes) |
| 2. `solutions` at pin 3 vs a version-2 map | `VersionBelow` | 2026-09-17: `storePath` removed from the Release `bridge.config.json` (gates off); `solutions` over stdio: `isError` true, "…is at schema version 2; this bridge requires 3. Run the CodeMem extractor once: it upgrades the map in place; the bridge is read-only and cannot." (47 ms) |
| 3. copy: DSP_Processor from `.slnx` | exit 0, schema 3, `.slnx` recorded | 2026-09-17: **through the bridge first: exit 2, `VersionBelow`** (the door's pin check precedes resolution for every shape — see the T041 note); then the extractor directly on the copy: exit 0 in 8 s, `run_id=10 observed=2811 matched=2811 new=0 … sha=d07d341b… warnings=0`; the copy at schema 3; `last_seen_path` `…\DSP_Processor.slnx` |
| 3. copy: RicksLife from `.slnx`, no NoWarn | exit 0, `warnings=3` | 2026-09-17: through the bridge on the upgraded copy (no `NoWarn*` in the launching environment): exit 0 in 9 s, `run_id=11 observed=1644 matched=1644 new=0 … sha=6aa2b895… warnings=3`; the three rows NU1701 on `RicksLife/RicksLife.vbproj`: OpenTK 3.1.0, OpenTK.GLControl 3.1.0, SkiaSharp.Views.WindowsForms 3.119.4; `last_seen_path` `…\RicksLife.slnx`. `solutions` on the copy: five entries, RicksLife `latestRun.warnings` 3, no registry field; `map_status`: five entries with `solutionId` (DSP_Processor `current`, MemOS `behind`, the other three `dirty`), `notInMap` empty, no `storePath`/`bound`/`unbound`/`inactive` — 3,577 ms on five repositories |
| 4. live: the two runs | as 3 | 2026-09-17 (T042, at the Architect's request, after step 1's verified backup — `fdac5897…c5404a` equal on both, re-checked at disk immediately before): map before `fdac5897…c5404a` (80,982,016 B). **Extractor directly** on `C:\_DB\codemem.sqlite` (`--solution …\DSP_Processor.slnx --db C:\_DB\codemem.sqlite --solution-key DSP_Processor`): exit 0, `run_id=10 observed=2811 matched=2811 reactivated=0 new=0 retired=0 registry_before=2811 candidates=0 unaccounted_observed=0 unaccounted_registry=0 digest=80199c40… sha=d07d341b… warnings=0`. The live map is then at **schema 3**, carrying `table\|extract_run_warnings` and `index\|ix_extract_run_warnings_run_id`, **both version-2 triggers still present (2)** — additive, as the contract says. **Then through the bridge** (`extract --solution-key RicksLife --solution …\RicksLife.slnx`, a `--config` copy naming the live map with `extract.enabled` true, so the Release `bridge.config.json` and the running `serve` process were left untouched; no `NoWarn*` in the environment): exit 0 in 8,363 ms, `run_id=11 observed=1644 matched=1644 … sha=6aa2b895… warnings=3`, the three NU1701 rows on `RicksLife/RicksLife.vbproj` (OpenTK 3.1.0, OpenTK.GLControl 3.1.0, SkiaSharp.Views.WindowsForms 3.119.4). Map after the two runs **`6c4ade46…ea002`** (81,059,840 B), no journal sidecar. `solutions` over the running bridge — the same call that answered `VersionBelow` at step 2 — now returns five entries at `schemaVersion` 3, no registry field, both new runs' `lastSeenPath` ending in `.slnx`, RicksLife `latestRun.warnings` 3. **B08 armed with `CODEMEM_LIVE_MAP`: 7 of 8 green**; the one Red is `LiveMapStatusReportsMemOsBehind` and it is the Red its own comment predicted ("the next MemOS commit turns this red, to diagnose") — see the note below the table |
| 5. `--repo-path` DSP_Processor, store renamed | key `DSP_Processor` | 2026-09-17 **on the copy** (the rename itself waits for the Shell to be closed): `extract --repo-path …\DSP_Processor` → `resolvedKey` `DSP_Processor` from the map alone, `solutionPath` the `.slnx`, launched, exit 0, run 12, 2811 observed and matched, balanced. **2026-09-17 live (T043)**, the Shell closed and the store verified unlocked, `C:\_DB\memos.sqlite` snapshotted to `memos.pre-005-T043.2026-09-17.sqlite` (`11c69aad…`, 644,247,552 B, equal hashes) and then renamed to `memos.sqlite.renamed-for-005`: `extract --repo-path C:\Users\rchau\source\repos\DSP_Processor` → `resolvedKey` `DSP_Processor`, `solutionPath` the `.slnx`, `launched` true, exit 0 in 5,909 ms, run 12, 2811 observed and matched, balanced — **resolved from the map alone with no store on disk at all**. `map_status` also answered with the store absent (five entries, no `unbound`). Store renamed back and re-verified `11c69aad…`, same size and same mtime 15:57:55.089 — never opened, never written; `memos.sqlite` was never recreated under its real name while renamed |
| 5. `--repo-path` unmapped + `map_status` | `PathNotInMap` with the command; listed | 2026-09-17 **on the copy**: `--repo-path …\vbCalc` (holding one `vbCalc.slnx`) → exit 2, `PathNotInMap`, the five mapped roots listed, the text carrying `extract --solution-key vbCalc --solution …\vbCalc.slnx`, 0 launches, the map byte-identical (`ed9c2ac8…` before and after); `map_status` → one `notInMap` entry, `verdict` `not_in_map`, key `vbCalc`, its file and the command; the log's resolution column reads `PathNotInMap`. **2026-09-17 live (T043)**, store still renamed: exit 2, `PathNotInMap`, `launched` false, the same five roots, the same adding command, "Nothing ran and nothing was added", 64 ms; the live map byte-identical across the refusal — **`26dfbacf…de15ef` before and after**. `map_status` → vbCalc under `notInMap` with `observedUtc` **`21:16:49.638Z`, which is this refusal's own `readAtUtc`**, refreshed from T041's `08:16:57.903Z`: the entry is proved to come from THIS refusal and not from the pre-existing copy-run line, which was already in the log and would otherwise have satisfied the check on its own |
| 6. after the `.sln` is deleted | runs from the `.slnx` | **open — the Operator's step** (T044) |
| 7. MemOS `git status` | empty, twice | 2026-09-17 before the live steps: empty. The second reading belongs to T044 |
| suite | green, under 5 min | 2026-09-17: Debug 197 passed / 0 failed / 9 skipped in 2 m 53 s; Release 197 / 0 / 9 in 3 m 1 s |

## What must not happen

- The bridge opens any file but the map: a second `SqliteConnection` site, a path containing `memos`, a `storePath` read.
- A solution is added by anything but the Architect's `--solution-key … --solution …`: no auto-add, no key chosen, no directory walked.
- An `extract` refusal launches anything or changes the map's hash.
- A `NoWarn` in the environment makes any difference; a restore error reaches the compiler.
- The log is rewritten, truncated or rotated by the bridge.
- A file in the MemOS repository changes; a registry row is written by this feature's code.

# Quickstart: CodeMem 004 — The Bridge

Validation guide. Tools: [contracts/tools.md](contracts/tools.md); command line, configuration, `.mcp.json`, the
fragment and the log: [contracts/cli-config-hook.md](contracts/cli-config-hook.md); spike evidence:
[spike.md](spike.md). 001–003's quickstarts still apply for the extractor.

## Prerequisites

- As 003: Windows 11, .NET SDK 9 or 10 (10.0.401 observed), .NET 8 runtime. Claude Code 2.1.272 (the VS Code
  extension's binary at `…\anthropic.claude-code-<version>-win32-x64\resources\native-binary\claude.exe`; not on
  PATH). The live map and store at `C:\_DB\`; the three repositories checked out where the map's `repo_root`
  columns say.
- For the live steps: a copy of `C:\_DB\codemem.sqlite` outside `C:\_DB\` (the read-only tests may run against the
  live file; the extraction step runs against the copy first).

## Build and test

```powershell
dotnet build CodeMem.sln -c Debug
dotnet test CodeMem.sln
```

Expected: 0 errors, 0 warnings; every 001–003 test green; the new `Bridge/` tests green; `AcceptanceRunner` and the
live-map facts Skipped (unarmed). Baseline before this feature: 69 total, 68 passed, 1 skipped, 2.2 min.

Live facts armed:

```powershell
$env:CODEMEM_LIVE_MAP = "C:\_DB\codemem.sqlite"; $env:CODEMEM_LIVE_STORE = "C:\_DB\memos.sqlite"
dotnet test CodeMem.sln --filter "FullyQualifiedName~B08_"
```

### Implementation record — build and test lines (2026-09-15)

| Task | What ran | Result |
|---|---|---|
| T001 | `dotnet build CodeMem.sln -c Debug`; `dotnet test CodeMem.sln` on branch `004-codemem-bridge` at `fcde454` (the analyze pass-2 artifact edits and `Fixtures/Hooks/` uncommitted in the tree — the commits are the Architect's; recorded, not hidden) | build 0 warnings / 0 errors; 68 passed / 1 skipped / 69 total in 1 m 41 s |
| T016 | `dotnet test CodeMem.sln` after Phase 2 (six projects; the doors, the registry read, the server skeleton; B01 written) | 77 passed / 2 failed / 1 skipped / 80 total in 1 m 47 s — the two failures are B01 (4) and (6), the Reds T015 named; every 001–003 test green, the amended `SqlLocationGateTests` included |
| T026 | `dotnet test CodeMem.sln` after US1 (the five readers; B01, B02 green; B08 (1)–(4) Skip-armed) | first run 87 passed / 1 failed / 5 skipped / 93 total in 2 m 1 s — the failure was `S02_SchemaUpgradeTests.UpgradedMapHasTheSameSchemaObjectsAsAFreshMap`, an unexpected Red: the line-ending pass had rewritten `SchemaRepository.vb` as CRLF, and the DDL constants' embedded newlines are the text SQLite stores, so a fresh map's `sqlite_master` no longer matched the version-1 fixture; LF restored, the file's header now says why; second run: **88 passed / 0 failed / 5 skipped / 93 total in 2 m 14 s** — green (SC-311, under 5 min) |
| T036 | `dotnet test CodeMem.sln` after US2 and US3 (type_usages, map_status; B03, B04 green; B08 (1)–(6) Skip-armed) | Passed:   105, Skipped:     7, Total:   112, Duration: 2 m 31 s  — green |
| T047 | `dotnet test CodeMem.sln` after US4 (the extract door, the hook entry; B05, B06 green with the nine T046 fires reverted; B08 (1)–(6) Skip-armed) | Passed:   139, Skipped:     7, Total:   146, Duration: 2 m 26 s — green |
| T051 | `dotnet test CodeMem.sln --filter "FullyQualifiedName~B08_LiveMapTests.LiveTwinsFoldOnMemOs"` armed, after US5 (the fold; B07 green with the T050 fire reverted) | first armed run 1 failed — total 2 where the task said 1 (the contains filter also finds `CodeMemMapFixtureTests`); corrected, 1 passed in 489 ms: twins 3556/4200, 14713 active rows → 14491 declarations, 222 absorbed |
| T054 (first) | `dotnet test CodeMem.sln` unarmed, then armed, after Phase 8 (README, the review pass) | unarmed Passed: 144, Skipped: 8, Total: 152, Duration: 2 m 54 s — green; armed Failed: 1, Passed: 150, Skipped: 1 in 2 m 40 s — the failure B08 (6): `map_status took 4108 ms` under the parallel load of the extraction scenarios (154 ms alone); B08 moved into the `LiveMap` collection with parallelization disabled. MemOS `git status --porcelain` empty before and after; live map `ab23953e…` and store `3b3f9aa2…` unchanged |
| T054 (final) | `dotnet test CodeMem.sln` unarmed, then armed, on the last build (B05 (18), the `LiveMap` collection, the corrected sample) | unarmed Passed: 145, Skipped: 8 (B08 ×7, AcceptanceRunner), Total: 153, Duration: 2 m 28 s — green (SC-311, under 5 min); armed Passed: 152, Skipped: 1 (AcceptanceRunner), Total: 153 in 2 m 21 s — green, `map_status` 68 ms; MemOS `git status --porcelain` empty before and after (FR-349); live map `ab23953e…` and store `3b3f9aa2…` unchanged (SC-301, SC-304) |

## Fixture validation (what the new tests do; runnable by hand against the built bridge)

1. **Read-only contract** — hash the fixture map; call each of the seven read tools once in process and once
   through the spawned executable; hash again: equal; no `-journal` beside it. Then the fire: an INSERT through
   `MapDatabase.OpenReadOnly` → `SqliteException` 8 (readonly).
2. **Shapes** — on the fixture map, `symbol_search(solutionKey "Sample", name "Draw"…)` and the others return the
   056/058/060 shapes with every property present; every occurrence carries an id or `external` with a doc id.
3. **Refusals** — absent path, foreign database, version-1 fixture map, a map bumped to 3, a store without the
   table, a retired symbol, a key the map lacks, a symbol outside scope: each named, nothing created (the absent
   path still absent afterwards).
4. **Twins** — a fixture copy with one file linked into both projects: one declaration, `compiledInto` of 2;
   `symbol_detail` of either id names both.
5. **`type_usages`** — on the fixture: a type with a constructor call from the other project, a member call from a
   sibling (`fromInside`), an `Implements`: counts as expected; `references` on the type returns only the
   non-`part_of` edges targeting it.
6. **`map_status` states** — a fixture copy inside a repository (LibGit2Sharp): extract at commit 1 → `current`;
   commit 2 → `behind` 1 with HEAD named; a modified file → `dirty`; a branch from commit 1 with its own commit
   → `diverged` (both shas named, `behindBy` null); `.git` removed → `no_git`; a registry row bound to id 99 →
   `map_missing_solution`; an unbound row and an inactive row listed without verdicts.
7. **Gate matrix** (scripted launcher, 0 real launches): both off → `extract.enabled`; enabled only, green-build
   origin → `extract.onGreenBuild`; enabled only, tool origin → launched; onGreenBuild only → `extract.enabled`;
   both on → launched. Each with one `extract.log` line.
8. **Resolution** — `repoPath` under the fixture root → resolved; under the temp directory → `PathNotRegistered`
   naming the path, 0 launches, map hash unchanged; two rows bound to one root → `AmbiguousRoot`.
9. **One real launch** — `extract(solutionKey "Sample")` with both gates on against the fixture map: exit 0,
   summary line, ten counts read back equal to the run row; a second call while a scripted launcher sleeps →
   `ExtractionRunning`.
10. **The hook entry** — pipe `Fixtures/Hooks/posttooluse-green.json` (the spike's payload) into `CodeMem.Bridge
    hook --config <fixture config>`: exit 0, one JSON line with `additionalContext` naming the resolved key (or the
    refusal), one log line; pipe `posttoolusefailure-red.json`: exit 0, "not a Bash PostToolUse", no log line, no
    launch; a payload whose command is `cd X && dotnet test Y.vbproj` resolves Y's directory.
11. **Descriptions** — every phrase of FR-312 present in the eight registered descriptions.
12. **Gates** — `MemOsReferenceGateTests`: no project file names `MemOS` or `rchaudio-a11y`; `BridgeSqlGateTests`:
    no SQL literal in either bridge project, SELECT-only literals in the new Core read methods, the registry module
    names `code_map_solutions` only and never `sqlite_master`, the store types referenced only by the bridge; `SqlLocationGateTests` amended to the two connection sites (after STOP 1).

## Live steps (after implementation; read-only unless stated)

1. **Register and approve**: build Release; copy `bridge.config.sample.json` beside the exe with the two live paths,
   both gates off; open a Claude Code session in `repos\CodeMem`; approve the `codemem` project server when asked;
   `claude mcp list` shows it connected.
2. **Read-only session** (SC-301, SC-302): hash the live map; call `solutions`, `symbol_search(projectId 132040,
   name "btnDeal_Click")` (expect one row, id 584), `symbol_detail(1200)`, `references(1200)` (expect 7, all
   `calls`, `BlackjackControl.vb`), `orphans(solutionKey "GameRoom")` (expect 279); hash again: equal. Record the
   same calls through the MemOS Shell for the side-by-side table.
3. **Findings** (SC-303, SC-309): `type_usages(3023, solutionKey "MemOS")` → ≥ 16 constructor calls, `references
   (3023)` → 0; `symbol_search(solutionKey "MemOS", name "CodeMemMapFixture", kind "class")` → total 1,
   `compiledInto` 3556 and 4200.
4. **Status** (SC-304): `map_status()` → MemOS `behind` (HEAD named, count ≥ 1), GameRoom `dirty` or `current` as
   `git status` says that minute, CodeMem `behind`/`dirty` (this feature's tree).
5. **Gates** (SC-305, SC-310): with both off, `extract(solutionKey "MemOS")` → refused naming `extract.enabled`,
   map hash unchanged, one log line. Flip `extract.enabled` on; call again **against the copy** (edit `mapPath` to
   the copy first): expect exit 0 and run 7 for MemOS at HEAD `cbeb661`; `map_status()` → MemOS `current`. Restore
   `mapPath`.
6. **The hook**: merge the fragment into the user's settings; flip `extract.onGreenBuild` on; in a session in
   `repos\CodeMem`, run `dotnet build CodeMem.sln`: the session shows the bridge's line (exit 0 and the CodeMem
   summary line, against the configured map); with `extract.onGreenBuild` off, the line names that gate; a build
   in an unregistered directory names the path. The tool call itself never fails.
7. **Live extraction of MemOS** (the operator step, at the Architect's request only, after a backup as 003 did):
   `mapPath` at the live file, `extract(solutionKey "MemOS")` or the hook on a green MemOS build → run 7;
   `map_status()` → `current`; `memos.sqlite` hashed before and after: unchanged.

### Side by side (SC-302; step 2)

The bridge's column is what the Release executable answered over stdio on 2026-09-16 (T055). The MemOS Shell's
column is the Operator's to fill from a Shell session against the same live map; audit-only fields are not compared.

| Call | Bridge (Release exe, stdio) | MemOS Shell |
|---|---|---|
| `solutions` | GameRoom run 4, CodeMem run 5, MemOS run 6 | |
| `symbol_search(projectId 132040, name "btnDeal_Click")` | total 1: method `btnDeal_Click`, id 584 | |
| `symbol_detail(1200)` | method `Draw` (`M:GameRoom.Shoe.Draw`), `GameRoom/Games/Cards/Shoe.vb:36`, 1 part; outbound part_of 1, calls 8, uses 2; inbound calls 7 | |
| `references(1200)` | 7, all `calls`, all in `GameRoom/Games/Blackjack/BlackjackControl.vb` | |
| `orphans(solutionKey "GameRoom")` | 299 (GameRoom 91, GameRoom.Tests 208); over 003's kinds 279 — see the Record row | |

### Record

| Step | Expected | Observed |
|---|---|---|
| 2. map hash before / after | equal | equal (`ab23953e…`) across every B08 fact, armed 2026-09-15 (T025) |
| 2. references(1200) | 7 | 7, all `calls`, all in `GameRoom/Games/Blackjack/BlackjackControl.vb` (B08 (2), in-process, 4 ms; T055 over stdio: the same 7, 3 ms; `symbol_detail(1200)`: method `Draw`, `M:GameRoom.Shoe.Draw`, `GameRoom/Games/Cards/Shoe.vb:36`, one part, outbound part_of 1 / calls 8 / uses 2, inbound calls 7, 10 ms) |
| 2. orphans(GameRoom) | 279 (73 + 206) | **299** — over the kinds 003 examined exactly 279 = 73 + 206; the 20 extra rows are the 19 properties and 1 field 060 FR-405 made examined (named in the B08 (3) diagnosis); **ruled 2026-09-16: 299 accepted as the live baseline; the next change is a Red to diagnose**; 110 ms; T055 over stdio: 299 = GameRoom 91 + GameRoom.Tests 208 (by kind: method 235, constructor 21, class 19, property 19, enum_member 3, module 1, field 1), 39 ms |
| 2. symbol_search(projectId 132040, "btnDeal_Click") | 1 row, id 584 | 1 row, id 584, scope by projectId through the registry (B08 (4), 7 ms; T055 over stdio: total 1, id 584, `compiledInto` [584], 18 ms) |
| 2. solutions | three, runs ≥ 4 / 5 / 6 | GameRoom, CodeMem, MemOS with runs 4, 5, 6 (B08 (1), 5 ms; T055 over stdio through the Release executable: the same three, 59 ms) |
| 3. type_usages(3023) constructor calls / references(3023) | ≥ 16 / 0 | 16 constructor calls naming 6661, total 20, fromOutside 16, no edge repeated; references(3023) 0 (B08 (5), armed 2026-09-15, T031; 73 ms / 4 ms; hash unchanged); T056 over stdio: total 20, calls 20, fromOutside 16, 84 ms; references(3023) 0, 3 ms |
| 3. twins 3556/4200 | 1 declaration, compiledInto 2 | one declaration `CodeMemMapFixture` with `compiledInto` 3556 and 4200 (total 2: the contains filter also lists `CodeMemMapFixtureTests`); across every kind 14713 active MemOS rows present as 14491 declarations — 222 absorbed, every twin group a pair (B08 (7), armed 2026-09-16, T051; 63 ms, the fourteen per-kind searches 4–52 ms; hash unchanged); T056 over stdio: total 2, `CodeMemMapFixture` 3556 with `compiledInto` [3556, 4200], 12 ms |
| 4. map_status MemOS | behind by N, HEAD cbeb661 | MemOS `behind`: "HEAD cbeb6615 is 11 commits past the recorded 806f5f3f; the tree is clean"; GameRoom `dirty` (HEAD equals the recorded e1bbe024); CodeMem `dirty` (HEAD fcde4546 is 3 commits past f6488469, this feature's uncommitted tree); unbound empty (B08 (6), armed 2026-09-15, T035; 154 ms after the status scan was tuned — the default scan of the three trees read 3.07 s, over SC-301's 3 s, so ignored files are no longer enumerated and renames not detected; the dirty verdict is unaffected); T056 over stdio the same minute: MemOS `behind` (HEAD cbeb6615, 11 commits past 806f5f3f, tree clean), GameRoom `dirty`, CodeMem `dirty`, unbound 0, inactive 0, 107 ms |
| 5. extract refused | names extract.enabled | `GateOff`: "extract is refused: **extract.enabled is false** in '…\bin\Release\net8.0\bridge.config.json'"; live map hash `ab23953e…` unchanged; one line in `extract.log` beside the Release executable (`tool | solutionKey=MemOS | - | extract.enabled | - | refused: …`) (T057, over stdio through the Release executable, 37 ms) |
| 5. extract on the copy | exit 0, run 7, then current | on `codemem.004-copy.sqlite` (hash equal to the live map's before) with `extract.enabled` on: `extract(solutionKey "MemOS")` → launched, exit 0 in 30.6 s, run 7 at `cbeb6615…`, line `solution=MemOS run_id=7 observed=15792 matched=14650 reactivated=0 new=1142 retired=63 registry_before=14713 notes_orphaned=0 candidates=0 unaccounted_observed=0 unaccounted_registry=0 digest=051faf7d…`; `map_status()` → MemOS `current` ("HEAD equals the recorded cbeb6615 and the tree is clean"); `memos.sqlite` hash `3b3f9aa2…` before and after; MemOS `git status --porcelain` empty before and after; the live map untouched (T058; the copy named through `--config`, the config beside the executable left with both gates off) |
| 6. hook, green build in CodeMem | one line, exit 0 | the `hook` entry driven by hand with the spike's payload shape (the merge into `~/.claude/settings.json` is the Operator's hand, not done here): (a) cwd `repos\CodeMem`, `dotnet build CodeMem.sln`, both gates on, map = the copy → `codemem extract (green build, C:\Users\rchau\source\repos\CodeMem): CodeMem: exit 0 — solution=CodeMem run_id=8 observed=1732 matched=725 reactivated=0 new=1007 retired=0 …` in 7 s, exit 0; (b) `extract.onGreenBuild` off → `…: refused — extract is refused: extract.onGreenBuild is false in '…'`, exit 0; (c) cwd an unregistered directory → `…: refused — No registered solution's repository root contains '…\codemem-live-nowhere'. Registered roots: …`, exit 0; `map_status()` on the copy afterwards: CodeMem `dirty` with HEAD equal to the recorded `fcde4546` (run 8 recorded it) (T059, SC-310) |
| 7. live MemOS run 7 | current; store hash unchanged | **not run** — only at the Architect's request, after a backup (T060 open); the live map's hash is `ab23953e…` after every step above |

## What must not happen

- No write to `C:\_DB\memos.sqlite` (hash before and after every step).
- No write to the live map except by the extractor in step 7, and only at the Architect's request.
- No edit to `~/.claude/settings.json` by anything but the Operator's hand in step 6.
- No file in `rchaudio-a11y\MemOS` changes (`git status` clean before and after).

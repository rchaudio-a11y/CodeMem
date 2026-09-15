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

### Record

| Step | Expected | Observed |
|---|---|---|
| 2. map hash before / after | equal | |
| 2. references(1200) | 7 | |
| 3. type_usages(3023) constructor calls / references(3023) | ≥ 16 / 0 | |
| 3. twins 3556/4200 | 1 declaration, compiledInto 2 | |
| 4. map_status MemOS | behind by N, HEAD cbeb661 | |
| 5. extract refused | names extract.enabled | |
| 5. extract on the copy | exit 0, run 7, then current | |
| 6. hook, green build in CodeMem | one line, exit 0 | |
| 7. live MemOS run 7 | current; store hash unchanged | |

## What must not happen

- No write to `C:\_DB\memos.sqlite` (hash before and after every step).
- No write to the live map except by the extractor in step 7, and only at the Architect's request.
- No edit to `~/.claude/settings.json` by anything but the Operator's hand in step 6.
- No file in `rchaudio-a11y\MemOS` changes (`git status` clean before and after).

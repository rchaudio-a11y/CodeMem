# Research: CodeMem 004 — The Bridge

**Feature**: `004-codemem-bridge` | **Date**: 2026-09-15 | **Spec**: [spec.md](spec.md) | **Spike**: [spike.md](spike.md)

Every item is **Verified** on this machine (with the evidence) or **Decided** among alternatives. Numbering continues
003's research (R31–R40) at R41. Probes live in the session scratchpad and are not part of the repository.

## Environment facts (Verified, 2026-09-15)

| Fact | Evidence |
|------|----------|
| Working tree at `b2e0168` (003 shipped), only `specs/004-codemem-bridge/` untracked | `git status` |
| Suite baseline before this feature: 69 total, 68 passed, 1 skipped, 2.2 min (003's record) | 003 quickstart |
| SDK 10.0.401; net8.0 projects run on runtime 8.0.31 | `dotnet --version`, 002's record |
| Claude Code 2.1.272 (bundled with the VS Code extension; `claude` is not on PATH) | spike |
| `ModelContextProtocol.Core` 1.4.0, `LibGit2Sharp` 0.32.0, `Microsoft.Data.Sqlite` 8.0.31 all in the NuGet cache | file system; Extraction's project file |
| Live map `C:\_DB\codemem.sqlite` schema 2, three solutions (runs 4, 5, 6); live store `C:\_DB\memos.sqlite` registry rows MemOS→3, GameRoom→1, CodeMem→2, all active | read-only queries |
| MemOS HEAD `cbeb661` is 11 commits past run 6's `806f5f3`, tree clean; CodeMem 1 past run 5's `f648846`, clean; GameRoom HEAD = run 4's `e1bbe02`, tree dirty (3 entries) | `git rev-list --count`, `git status --porcelain` |
| Symbol 3023 (`CodeMemMapReader`): 0 non-`part_of` inbound edges; constructor 6661 has 16 `calls` (15 `MemOS.ContractTests`, 1 `MemOS.Modules.Mcp`); 4 member `calls`, all from its own `New` and `Read` | read-only query |
| 222 twin groups in the MemOS map (6 class, 6 constructor, 68 field, 65 method, 77 property); none in GameRoom or CodeMem | read-only query |

## R41. Claude Code loads a stdio server from `.mcp.json` in this repository (Verified — spike fact 1)

**Decision**: register the bridge through `.mcp.json` at the repository root, `type: stdio`, a **repository-relative**
`command` (`src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe`), `args: ["serve"]`; server name `codemem`,
so the tools are `mcp__codemem__<tool>` to the model.

**Evidence**: spike §Fact 1 — `claude mcp list` shows the project-scope entry pending approval; a non-interactive
session called `mcp__spike__ping` and got the server's answer; the server is launched with its working directory at
the repository root; and a relative `command` resolved against that root and answered (spike 1d). The file is
therefore portable across checkouts; the user-scope registration for other repositories and the hook fragment,
which run from any directory, carry the absolute path, and the process document names those two lines to edit on
another machine.

**Alternatives**: an absolute path in `.mcp.json` (rejected once the relative form was verified: the registry's
absolute paths are a carve-out, not a preference); user-scope registration only (rejected as the primary: the
ruling names one `.mcp.json` entry at the root; user scope is the documented way to reach the bridge from other
repositories' sessions).

## R42. A PostToolUse hook observes a green `dotnet build` and calls the bridge (Verified — spike fact 2)

**Decision**: the fragment subscribes to `PostToolUse`, matcher `Bash`, one `command` hook whose command is
`"<bridge>\CodeMem.Bridge.exe" hook`; the `hook` entry reads the event JSON from stdin.

**Facts the design rests on** (spike §Fact 2): `tool_response` for Bash carries no exit code (`stdout`, `stderr`,
`interrupted`, `isImage`, `noOutputExpected`); a non-zero exit fires `PostToolUseFailure` instead, with
`error: "Exit code N\n…"` and no `tool_response`; `cwd` is the session directory, not the build's;
`tool_input.command` is the exact command; `hookSpecificOutput.additionalContext` reaches the model verbatim;
the hook ran in 17 ms. The `mcp_tool` hook type had no observable effect and is not used (Q5 (b) dropped).

**Consequence**: "exited 0" is established by the event that fired. The entry still refuses a payload whose
`hook_event_name` is not `PostToolUse`, whose `tool_name` is not `Bash`, or whose `tool_response.interrupted` is
true, and it never launches anything on the failure event because the fragment does not subscribe to it.

## R43. The MCP library: `ModelContextProtocol.Core` 1.4.0 (Verified in VB.NET by the spike)

**Decision**: `McpServer.Create(New StdioServerTransport("codemem", Nothing), options, Nothing, Nothing)` with
`options.ServerInfo` and `options.ToolCollection` holding eight `McpServerTool.Create(delegate, createOptions)`
tools; `RunAsync` until the client closes stdin; `DisposeAsync` after the run (VB forbids `Await` in `Finally`).
The SDK builds each tool's input schema from the delegate's parameters (`Optional … As Long? = Nothing` becomes an
optional integer; a `String` with no default is required), and `McpServerToolCreateOptions.ReadOnly = True` sets the
`readOnlyHint` annotation — set on the seven read tools, not on `extract`. A tool returning a `String` is sent as
one text content block; a tool returning `CallToolResult` with `IsError = True` is the refusal shape (058's
"never thrown"). The `ModelContextProtocol` hosting package is not needed (no DI, no host).

**Alternatives**: a hand-rolled JSON-RPC loop (rejected: the SDK is what MemOS's server already runs against
Claude Code; two implementations of the wire format would be the one duplication nobody asked for);
`ModelContextProtocol` with `Host.CreateApplicationBuilder` (rejected: DI and hosting for eight delegates).

## R44. Read-only opens: two connection doors, both in Core (Decided; the write refusal demonstrated at FR-342's fire)

**Decision**: `MapDatabase.OpenReadOnly(path)` — `Mode=ReadOnly`, `Pooling=False`, `Default Timeout=3`, no
`journal_mode` pragma (a write on a read-only connection), `foreign_keys` not set (reads need none) — returns a
`MapDatabase` whose `InspectSchema` works unchanged and whose `BeginImmediate` would throw
`MapLockHeldException` on `SQLITE_READONLY` (existing code; the bridge never calls it). A second class,
`StoreDatabase.OpenReadOnly(path)`, is the one door to `memos.sqlite`. `SqliteOpenMode.ReadOnly` opens with
`SQLITE_OPEN_READONLY`: a missing file fails with `SQLITE_CANTOPEN` (14) and is not created; a write fails with
`SQLITE_READONLY` (8). MemOS 056 verified both on this machine (its contract §4); FR-342's fire re-demonstrates the
write refusal through the bridge's own door.

**Gate consequence**: `SqlLocationGateTests.OnlyMapDatabaseOpensAConnection` asserts `New SqliteConnection` appears
in exactly `{MapDatabase.vb}`; it becomes exactly `{MapDatabase.vb, StoreDatabase.vb}` — an amendment the
Architect rules with the Article IX amendment (R55), with its own fire.

**Alternatives**: one generic `ReadOnlyDatabase` for both files (rejected: the map's door must keep
`InspectSchema` and the repositories' `MapDatabase` parameter; the store's door must never be mistaken for a map);
opening the store through `MapDatabase` (rejected: the class's name is a promise about role, Article IX).

## R45. Where the SQL lives (Decided by the gates)

`SqlLocationGateTests.SqlAppearsOnlyInRepositories` allows SQL literals under `CodeMem.Core/Repositories/` and
`CodeMem.Core/Schema/` only (recursive path match). So every bridge query is a named method in Core: read methods
added to the existing repositories (`SolutionsRepository.ReadAll/ReadById/ReadByKey`,
`ExtractRunsRepository.ReadLatestCompleted/ReadById`, `CodeSymbolsRepository.Search/ReadById/ReadMembers/
ReadProjects/ReadOrphans`, `CodeEdgesRepository.ReadOutbound/ReadInbound/ReadReferences/ReadTypeUsages`,
`CodePartsRepository.ReadParts`, `MapIdentityRepository.ReadIdentity`) and one new module in a subfolder,
`Repositories/Registry/CodeMapSolutionsRepository.ReadAll(store)` — the only SQL that names a store table. The
absence of the table is learned from the query's own `no such table` error, translated in Core to a typed
exception; no `sqlite_master` probe (INC1). The
tripwire scans `Core/Repositories/*.vb` (top level): SELECTs add nothing it forbids. The bridge project itself
holds **no SQL literal**; `BridgeSqlGateTests` (new) asserts that, asserts every Core read method's literal is
SELECT-only, and asserts the registry module's literals name `code_map_solutions` and no other table (FR-303,
FR-304).

## R46. Git through LibGit2Sharp, not the `git` executable (Decided)

**Decision**: the bridge takes a direct `PackageReference` to `LibGit2Sharp` 0.32.0 (the version Extraction pins;
the transitive copy is already in the output) and reads, per bound solution: `Repository.Discover(repoRoot)`,
refusing the root as `no_git` when nothing is discovered **or when the discovered working directory is not the
registered root** (the extractor recorded the working directory it discovered from the solution's base directory,
so a match is the expected case and a mismatch means the root moved or was nested); `repo.Head.Tip` (`Nothing` →
`no_git`, "no commit"); `repo.RetrieveStatus()` `IsDirty` for the tree; `repo.Lookup(Of Commit)(recordedSha)`
(`Nothing` → `diverged`, "recorded commit not in this repository"); `repo.ObjectDatabase.FindMergeBase(recorded,
head)` equal to `recorded` → ancestor, count = `repo.Commits.QueryBy(New CommitFilter With
{.IncludeReachableFrom = head, .ExcludeReachableFrom = recorded}).Count()`; equal shas → 0; otherwise `diverged`.

**The dirty definitions differ, and the difference is stated**: the run's `is_dirty` (GitProvenance) is evaluated
over the compiled inputs only, ignoring ignored files and files outside the working directory; `map_status`'s
`treeDirty` is the whole working tree (tracked changes and untracked, non-ignored files) because the bridge does not
know the run's inputs. A README edit therefore reads `dirty` here and would have read clean in the run. The verdict
says "dirty tree", which is true; the alternative — narrowing to `*.vb`/`*.vbproj`/`*.sln` — is a heuristic and
is not built. **STOP 1 (2026-09-15): accepted, not ruled** — the tree-wide definition stands for this feature as
stated; narrowing stays available as a later ruling.

**Alternatives**: the `git` executable (rejected: a PATH dependency, output parsing, one process per solution per
call, and behaviour that can differ from the library the extractor used to write the value being compared);
referencing `CodeMem.Extraction` to reuse `GitProvenance.Read` (rejected for the dirty check — it needs the
compiled inputs — but the bridge does reference Extraction transitively through the Extractor, see R47, and reuses
`SolutionScope` for the prefix rule).

## R47. Launching the extractor (Decided; the `ExtractorProcess` precedent)

**Decision**: `CodeMem.Bridge.vbproj` carries a `ProjectReference` to `CodeMem.Extractor` (CodeMem-internal;
permitted), so `CodeMem.Extractor.dll` and its dependencies land beside the bridge exactly as they land beside the
tests. The default `extractorPath` is `<bridge directory>\CodeMem.Extractor.dll`, launched as
`dotnet "<dll>" --solution "<last_seen_path>" --db "<mapPath>" --solution-key "<key>"` with stdout and stderr
redirected, no window, working directory the bridge's, a **540 s** budget inside the hook's 600 s (TIM1), after which
the child is killed and the result says `timedOut`. The summary line is the last non-empty stdout line (exit 0 and 4); the refusal line is the first stderr
line (other exits). The ten counts are read from the map by the `run_id` the summary line names, after the child
exits, through `ExtractRunsRepository.ReadById` on a fresh read-only connection. An `extractorPath` in the config
overrides the default; a configured path that names a `.dll` is run through `dotnet`, an `.exe` directly.

**Consequences**: the bridge's output directory gains Roslyn, MSBuild and LibGit2Sharp assemblies (as the test
directory has); nothing loads until used — the `serve` entry touches neither Extraction nor Roslyn types before a
call. `SolutionScope.Resolve(root, root).ContainsDirectory(path)` — an additive method on 003's door (COR1): equality or
prefix, both sides normalised as directories with one trailing separator, case-insensitive; `Contains` keeps its
file semantics for the extractor — becomes the one door for the containment `repoPath` needs; no second rule.

**Alternatives**: no reference and a mandatory `extractorPath` (rejected: the spec's Q8 names a default beside the
bridge); referencing Extraction only and rebuilding the command line (rejected: the Extractor's own `Program` and
`CommandLine` are the contract, and the tests already spawn the dll).

## R48. Three origins, one door, one executable (Decided; Q5 (a) confirmed by R42)

The executable has three entries (contracts/cli-config-hook.md): `serve` (default; the stdio server), `extract`
(a human's one-shot: `--solution-key`, `--repo-path` or `--stale`; `--on-green-build` marks the green-build origin
and is what the `hook` entry uses internally, never a human), and `hook` (stdin JSON → `repoPath` → the door with
origin green-build → `additionalContext`). All three and the MCP `extract` tool call `ExtractDoor.Run(request)`,
where `request.Origin` is `Tool`, `GreenBuild` or `Manual`; the door resolves, gates, launches, logs and returns
one `ExtractResult`. The gates: `extract.enabled` refuses every origin; `extract.onGreenBuild` refuses
`GreenBuild` only and is consulted only after `extract.enabled` passed (inert while off).

**The hook entry's parse** (Q6 as ruled): split the command on `&&`, `;` and `|`; the segment that starts with
`dotnet build` or `dotnet test` (after trimming) is the build; its effective directory is the last `cd <dir>`
segment before it (resolved against `cwd`) or `cwd`; the target is decided **by shape, not existence, with positional
targets parsed after options** (COR2): the build segment is tokenised with quoted tokens kept whole and quotes
stripped; the candidates are every token ending in `.sln` or `.vbproj` (case-insensitive) anywhere after the verb —
so `dotnet build -c Debug CodeMem.sln` names the solution — plus the token immediately after the verb when it does
not start with `-` and is not such a file (a positional directory); 0 candidates → the effective directory; 1 → its
directory (a file) or itself (a directory), resolved against the effective directory, never checked for existence;
2 or more → `AmbiguousTarget` naming them, never resolved by fallback; a named path under no registered root is
refused by resolution naming that path and is never replaced by the effective directory. Nothing else in the
command is interpreted.

## R49. Configuration file (Decided; spec Q8 plus one addition)

`bridge.config.json` beside the executable, read on every call, never cached:

```json
{ "mapPath": "C:\\_DB\\codemem.sqlite", "storePath": "C:\\_DB\\memos.sqlite",
  "extractorPath": null, "extract": { "enabled": false, "onGreenBuild": false } }
```

**Addition for STOP 1**: every entry accepts `--config <path>` to name another file. Reason: the tests spawn the
real executable from the test output directory, where a file beside the exe would be shared by every test class;
and a user-scope hook fragment can name a config outside a build directory that `dotnet clean` empties. Without
`--config`, the default is beside the exe, as the spec says; `extract.log` lives beside the executable whatever
`--config` names (INC3). A missing file, or a missing `mapPath`/`storePath`,
is the `Unconfigured` refusal naming the key; `extractorPath` absent means the R47 default.

## R50. The twin fold (Decided; spec Q2 as proposed)

`CodeSymbolsRepository.Search` returns **raw rows**, uncapped, ordered by `name, kind, path, start_line,
project_symbol_id, id` (COR3: two overloads declared on one physical line under one project share the four fields
with distinct doc ids, and a SQL `GROUP BY` would fold them). `TwinFolder` groups in memory by (`solution_id`,
`kind`, `name`, `path`, `start_line`) and folds a group into one declaration **only when its `project_symbol_id`
values are all distinct and more than one**; every other group yields one declaration per row. `total` counts
declarations; the 200 cap applies to declarations. A folded declaration carries N `compiledInto` entries (project id
and name, symbol id, doc-comment id) — the doc-comment ids differ per project (root namespaces), so each entry
carries its own. `symbol_detail(id)` reads the row by id, then the twins by the four fields, and presents
the header with every twin; parts and edges are the requested id's. The fold is one module (`TwinFolder`) both
tools call (Article XII).

## R51. `type_usages` query (Decided; spec Q3 as ruled)

One SQL statement in `CodeEdgesRepository.ReadTypeUsages(db, typeId)`: a `WITH RECURSIVE inside(id)` over
`container_id` from the type (the type, its members, their descendants) for `fromInside`; then a `UNION` **deduplicated by edge id** (DUP1) of
(a) edges with `target_symbol_id = type` and `verb <> 'part_of'`, (b) `calls` edges whose target is an active
constructor with `container_id = type`, (c) `calls`/`uses` edges whose target is an active symbol with
`container_id = type` and kind not constructor, (d) `implements`/`extends` edges with `target_symbol_id = type`
(a subset of (a); folded by the canonical dedup on edge id). Each row joins the source and the target symbol and
carries `source_symbol_id IN inside`. Grouping and counts are computed in memory from the flat list (seven verbs,
zeros included). Type kinds accepted: `class, module, structure, interface, enum, delegate` (FR-319).

## R52. `map_status` computation (Decided; spec Q4 as ruled)

Per active registry row: bound (`codemem_solution_id` not NULL) → `SolutionsRepository.ReadById` (absent →
`map_missing_solution`) → `ExtractRunsRepository.ReadLatestCompleted(solutionId)` (absent → `no_git` with reason
"no completed run"; a solution with no run cannot be compared) → `RepositoryFacts.Read(repo_root, commit_sha)`
(R46) → verdict in the ruled order: `map_missing_solution` → `no_git` → `dirty` → `diverged` → `behind` →
`current`. Unbound and inactive rows are listed with their keys under `unbound`/`inactive` and counted; they get
no verdict. `extract(stale)` selects `behind`, `dirty` and `diverged`.

## R53. Busy behaviour (Decided)

`Default Timeout=3` on both read-only connections sets the driver's busy handler to 3 s (the value MemOS 056
chose). In `journal_mode = DELETE` (the map's mode) a reader is blocked only during the writer's commit
(`PENDING`/`EXCLUSIVE`), not while the extractor holds `RESERVED` under `BEGIN IMMEDIATE`; so a read during a
long extraction normally sees the previous completed run, and a read during the publication instant waits up to
3 s, then refuses `Busy`. A hot journal left by a crashed writer cannot be rolled back by a read-only connection:
that surfaces as `Unopenable` with the driver's text ("attempt to write a readonly database" or "readonly
rollback"), which is the honest answer — the next extractor run repairs it.

## R54. Test design facts (Decided)

- **In-process facts** call the same door the tools call (`BridgeTools` methods take a `BridgeConfig` and return
  the envelope string or the refusal), so the shape and refusal batteries are fast; **production-route facts**
  (Article XIII) spawn `CodeMem.Bridge.exe serve` from the test output directory through a new support class
  `BridgeProcess` speaking JSON-RPC over stdio (the spike probe's four messages, in VB), and `CodeMem.Bridge.exe
  hook` with the spike's recorded payloads piped to stdin.
- **The spike payloads are fixtures**: `tests/CodeMem.Tests/Fixtures/Hooks/posttooluse-green.json` and
  `posttoolusefailure-red.json`, verbatim from the log (session ids included; they are data). `Fixtures/**` is
  excluded from compilation and from the header gate already.
- **Registry fixture**: `RegistryFixture` creates a throwaway store with exactly the `code_map_solutions` DDL of
  migration 029 §1 (transcribed, with the two indexes) and seeds rows the test names; no other MemOS table. A
  store without the table exercises `RegistryAbsent`.
- **Map fixtures**: the committed fixture solution extracted into a `TempMap` (existing support); a version-1 map
  (`V1MapFixture`) for `VersionBelow`; `MapQueries.SetSchemaVersion(db, 3)` for `VersionAbove`;
  `MapQueries.CreateForeignDatabase` for `NotAMap`. `map_status` states come from a `FixtureCopy` inside a
  repository created with LibGit2Sharp (`Repository.Init`, `Commands.Stage`, `Commit` — R39): extract at commit 1
  → `current`; commit 2 → `behind` 1 with HEAD named; edit a file → `dirty`; a fresh branch from commit 1 with its
  own commit → `diverged`; delete `.git` → `no_git`.
- **Launcher seam**: `ExtractDoor` takes an `IExtractorLauncher`; the production launcher spawns the process; the
  test launcher records the request and returns a scripted exit code and lines, so the gate matrix and FR-347 assert
  "0 launches" directly (SC-306) without building the fixture each time. One production-route fact runs the real
  launcher on the fixture (exit 0, counts read back).
- **Live facts** are `SkippableFact`s armed by `CODEMEM_LIVE_MAP` and `CODEMEM_LIVE_STORE` (paths); unset → Skipped,
  as `AcceptanceRunner` is armed by `CODEMEM_ACCEPT_SOLUTION`. They read only; the live extraction of MemOS is an
  operator step in the quickstart.
- **Description phrases**: a fact reads the eight registered descriptions from `BridgeTools` and asserts each
  required phrase (FR-312).

## R55. Constitution: Article IX and the connection gate need an amendment (Decided — for STOP 1)

Article IX, last sentence: *"CodeMem opens no database in any role other than its own map."* The bridge opens
`memos.sqlite` read-only for `code_map_solutions` — by decisions 137077 and 142362 ("read `code_map_solutions`
from `memos.sqlite` (file, read-only)"), and 132115 itself flagged that the compatibility with Article IX
"belongs as an amendment on CodeMem's side". The plan therefore proposes **v1.3.0 (MINOR)**, one sentence added to
Article IX and one gate amended:

> *Article IX addition*: "One exception is ruled (decisions 137077, 142362, 152658): the bridge opens the MemOS
> store read-only, for the `code_map_solutions` registry alone, to construct `--solution-key` and to bind map
> solutions to projects; it writes neither file. The extractor never opens the store."

> *Review Gate amendment*: "New SqliteConnection appears in exactly two production files, `MapDatabase.vb` (the
> map, read-write for the extractor, read-only for the bridge) and `StoreDatabase.vb` (the store, read-only)."

**Ruled and applied 2026-09-15 (STOP 1, item 1)**: both texts adopted as written; the constitution is at v1.3.0
with the dated record, rationale and migration path (none — no code opened the store before the amendment; the
gate test is amended in the task that lands `StoreDatabase.vb`, after its Red, and re-fired). Store code may
proceed.

## R56. What the spike measured (Verified)

| Measure | Value |
|---|---|
| Hand-driven stdio: initialize + list + call | 382 ms |
| Claude Code non-interactive: tool call end to end (three model turns) | 6.0 s |
| Hook entry (stdin → decision → in-process one-shot → answer) | 17 ms |
| `dotnet build` of the fixture copy inside the session | 1.8 s |
| Whole hook session, green / red | 9.7 s / 12.3 s |
| MemOS extraction (2026-09-13, live) | 31 s |

SC-301's 3 s per read call is a bound on the bridge's own work (a query over a 64 MB map); the model's turns are
not counted.

## R57. Two projects, one process (Decided — review ruling CON1)

`CodeMem.Bridge` (exe) holds `Program.vb`, `BridgeCommandLine.vb` and `Mcp/BridgeServer.vb` — the entry point, the
grammar and the MCP host wiring — and nothing else; `CodeMem.Bridging` (class library) holds configuration, refusals,
the map and store doors, scope, the readers and envelopes, the twin fold, status, the extract door, the hook entry
and the tool methods. The Extractor/Extraction shape already in the solution; Article I ("executables only wire")
passes without a justification. Bridging references Core and Extraction; Bridge references Bridging and the
Extractor (so the extractor lands beside the exe). The tests reference both.

## R58. One open, one read transaction per call (Decided — review ruling CON4)

`MapAccess.OpenRead` opens the map read-only, inspects the schema against the pin, then issues a plain `BEGIN`
(`MapDatabase.BeginRead`); the call's scope resolution and reader run inside it; `EndRead` (`COMMIT`) follows
serialisation. In rollback-journal mode the reader's SHARED lock lasts for the transaction, so a writer's commit
(the extractor's publication, which needs EXCLUSIVE) waits or fails busy until the read ends: two SELECTs of one
tool cannot see two snapshots. The fact (B01 (6)) pauses a call between scope resolution and the reader through a
test seam and attempts a rename on a second connection with a 1 s busy timeout: the rename gets busy (5), the
response carries the old name, and the rename succeeds after the call. `extract` ends its read and disposes before
launching, so the extractor is never blocked by the bridge (FR-334).

## R59. Directory containment includes equality (Decided — review ruling COR1)

`SolutionScope.Contains(fullPath)` answers "is this file under the root" — a file path never equals the root, so a
prefix test is complete there. A repository path handed to `extract` is a directory and may **be** the root
(`dotnet build` in the repository root passes the root itself); `ContainsDirectory(directory)` normalises the
argument as `Root` is (full path, separators, one trailing separator) and answers equality or prefix. Additive in
Extraction, never called by the extractor; `Contains` untouched. Exact-root facts in `R01_ScopeRootTests` (the door)
and B05 (the bridge's use).

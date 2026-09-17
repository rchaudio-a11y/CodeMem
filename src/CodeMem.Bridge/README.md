# CodeMem.Bridge — the process document

This is the document contracts/cli-config-hook.md §6 asks for (004 FR-339; 005 FR-432): what the bridge is, how it is
installed, its command line, when each tool is called, what "not in the map" means, the gates, the log, the refusals a
user will meet and their remedies, what the bridge never does, what changed in the extractor, where the retired code
went, and one recorded non-choice.

## What the bridge is — one file

The bridge is Claude Code's source for the CodeMem code map, and the green-compile trigger that keeps the map
current. It is one process, `CodeMem.Bridge.exe`, built from two projects: `CodeMem.Bridge` (the executable: the
command line, the MCP server) and `CodeMem.Bridging` (the tools, the readers, the extract door). It serves eight
MCP tools over stdio — `solutions`, `symbol_search`, `symbol_detail`, `references`, `orphans`, `type_usages`,
`map_status` and `extract` — reading `codemem.sqlite` (the map) and nothing else: one database file, opened read-only
per call and closed before the reply. `memos.sqlite` is not a file the bridge knows; a configuration that names one
is refused by name. The bridge never writes the map: the CodeMem extractor is the sole writer, and `extract` only
launches it as a child process. Every tool is scoped by `solutionKey`, the key of a `solutions` row in the map;
`solutions` lists them.

Decisions 142362 (the bridge is Claude Code's source; a green compile is its trigger), 137077 (serve and extract in
one surface; the extract verb is permissive-gated) and 152658 (stdio; a PostToolUse hook; two gates, both off,
dependent) still bind this document. Four clauses are **superseded** by 152687 and by feature 005: decision 137077's
store-read clause, 142362's "a file, not a dependency", 152658 ruling 1's store clause, and 152672 item 1 (the v1.3.0
amendment to Article IX, which v1.4.0 reverted). The Architect's sentence that rules them: "CodeMem is supposed to be
a standalone project completely independent of MemOS; MemOS is a project that can look into CodeMem if it's there."

## Install

1. **Build**: `dotnet build CodeMem.sln -c Release`. The executable is
   `src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe`; the extractor, `CodeMem.Extractor.dll`, lands
   beside it.
2. **Configure**: copy `src/CodeMem.Bridge/bridge.config.sample.json` beside the executable as
   `bridge.config.json` and set the map path:

   ```json
   {
     "mapPath": "C:\\_DB\\codemem.sqlite",
     "extractorPath": null,
     "extract": { "enabled": false, "onGreenBuild": false }
   }
   ```

   `extractorPath` null means `CodeMem.Extractor.dll` beside the executable. The file is read on every call, so an
   edit takes effect on the next call without a restart; a file that is not JSON, or a missing `mapPath`, is the
   `Unconfigured` refusal naming the file; a file that still carries `storePath` (004's key) is the
   `ConfigKeyRetired` refusal naming the key — remove the line. Other unknown keys are ignored.
3. **Approve the project server**: the repository's `.mcp.json` registers `codemem` (stdio, `CodeMem.Bridge.exe
   serve`) at project scope. The first interactive Claude Code session in `repos\CodeMem` asks for approval;
   `claude mcp list` then shows it connected.
4. **Other repositories**: a session in GameRoom or MemOS needs the user-scope registration with the absolute
   path:

   ```text
   claude mcp add --scope user --transport stdio codemem -- "C:/Users/rchau/source/repos/CodeMem/src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe" serve
   ```

   The same entry written by hand, for when `claude` is not on PATH (it is not on this machine): merge it into the
   top-level `mcpServers` of `~/.claude.json`, the user-scope store, not `~/.claude/settings.json`:

   ```json
   "mcpServers": {
     "codemem": {
       "type": "stdio",
       "command": "C:/Users/rchau/source/repos/CodeMem/src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe",
       "args": ["serve"]
     }
   }
   ```

5. **The hook**: merge `src/CodeMem.Bridge/hooks/settings.fragment.json` into `~/.claude/settings.json` by hand
   (user scope, so a build in any mapped repository's session reaches it). Nothing in this feature installs it.
6. **Flip the gates**: both ship off. The Architect flips `extract.enabled`, then `extract.onGreenBuild`, in
   `bridge.config.json`; see the gates below.

There is no registry step and no bind step: a solution is in the map when the extractor has written its row, and
the way to put one there is the `extract` command line with a key and a path (below).

## Command line

```text
CodeMem.Bridge [serve] [--config <path>]
CodeMem.Bridge extract (--solution-key <key> [--solution <path>] | --repo-path <dir> | --stale) [--on-green-build] [--config <path>]
CodeMem.Bridge hook [--config <path>]
CodeMem.Bridge --help
```

`serve` is the MCP server (nothing but the transport ever writes to its stdout). `extract` prints one JSON
document and exits 0 when the request was accepted and the child ran (the child's own exit code is inside the
JSON), 2 when the request was refused, 1 on a usage error. `hook` reads the PostToolUse event from stdin, prints one
JSON line and always exits 0.

`--solution <path>` is accepted on the `extract` entry only, and only beside `--solution-key`; alone it is a usage
error naming the rule. With it, the key need not be in the map: the extractor is launched with that `.sln` or
`.slnx` and creates the `solutions` row when the key is new, or refreshes `last_seen_path` when it is not. Without
it, the map's last-seen path is used. There is no `--key`; `--solution-key` is the one spelling. The same shape is
the `extract` tool's `solutionKey` with `solutionPath`.

## When each tool is called

- **`map_status`** — at the start of a session, and again before relying on `references`, `orphans` or
  `type_usages`. It says, per map solution, whether the map is `current`, `behind` (commits past the recorded one),
  `dirty` (uncommitted changes) or `no_git`, with the run's commit and HEAD named. A `behind` or `dirty` verdict
  means the map describes an older tree: read it as evidence, not as the truth of the working copy. It also lists,
  under `notInMap`, every directory an `extract` has refused as not in the map (see below), with the command that
  would add it — read from `extract.log`, never remembered, gone the moment a mapped root contains the directory.
- **`type_usages`** — before any delete or rename of a type. `references` on a type's constructor or members is
  not the type's usage: `type_usages` folds constructor calls, member references, implements, extends, and the
  places the type is named, over the whole map.
- **`references`, `symbol_detail`, `symbol_search`, `orphans`, `solutions`** — as the map is read: search first,
  then detail or references by the symbol id the search returned. `orphans` examines the kinds the map records as
  examined; project and namespace rows are roots and are not orphans by construction. `solutions` carries each
  solution's latest run, its ten counts and its `warnings` count.
- **`extract`** — by the hook, on a green `dotnet build` or `dotnet test`, never by Claude Code directly. The tool
  is registered so the hook and the tests can reach the door; a session that wants a fresh map builds the solution
  and lets the hook run. A run's answer carries the child's exit code, its summary line verbatim, the run's ten
  counts and its `warnings` rows.
- **`CodeMem.Bridge extract --solution-key <key> --solution <path>`** — by the Architect, who names the key: this is
  how a solution the map has never seen is added. Same gate, same door, same log line as every other extract; the
  extractor creates the row. The second call with the same key and path is an ordinary re-extraction.
- **`CodeMem.Bridge extract --stale`** — by a human, from the command line: it reads `map_status` and extracts, one
  after another, every mapped solution that is `behind` or `dirty`, lists the ones it left alone, and lists the
  not-in-map directories without ever launching for one.
- **`--on-green-build`** — never by a human. It exists so the `hook` entry and the tests reach the green-build
  origin through the same door; a human passing it only reaches the second gate.

## Not in the map

A directory resolves to a solution when a mapped solution's root contains it — the repository working directory the
run recorded, or, for a solution extracted outside any repository, its solution file's directory. Nothing else
resolves a directory: not a registry, not the session's working directory, not a parent or a child of the directory
given. When no root contains it, the answer is `PathNotInMap`, and after a green build in such a repository the
hook's one line reads, for example:

```text
codemem hook: refused — 'C:\Users\rchau\source\repos\vbCalc' is not in the map: no mapped solution's root contains it. Mapped roots: C:\Users\rchau\source\repos\GameRoom\, …. It holds C:\Users\rchau\source\repos\vbCalc\vbCalc.sln; to add it, run: extract --solution-key vbCalc --solution C:\Users\rchau\source\repos\vbCalc\vbCalc.sln (the extract tool: solutionKey and solutionPath). Nothing ran and nothing was added.
```

The key is suggested from the one `.sln` or `.slnx` the directory itself holds; a directory holding none gets the
command with placeholders; a directory holding more than one is `AmbiguousSolutionFile`, both named, no key
suggested. What to do: run the command it printed, with the key you want, from the command line. The bridge never
adds a solution itself and never chooses a key. Until then `map_status` keeps listing the directory.

## The gates and who flips them

Two booleans in `bridge.config.json`, read on every call:

| Gate | Governs | Off means |
|---|---|---|
| `extract.enabled` | the verb, every origin (tool, manual, green build) | every `extract` is refused naming `extract.enabled`; nothing launches; the map is unchanged |
| `extract.onGreenBuild` | the trigger only | the hook's extract is refused naming `extract.onGreenBuild`; tool and manual extracts still run; inert while `enabled` is off |

The Architect flips them, in that order. The gate is checked before the map is opened, so a refused `extract`
reveals nothing about it. A refusal is an answer, not an error: the tool call the hook observed never fails.

## The extract log

`extract.log` beside the executable (`AppContext.BaseDirectory`, whatever `--config` names), one line per `extract`
invocation, appended, tab-separated, no header. Columns: UTC time; origin (`tool`, `manual`, `green_build`); the
target as given (`solutionKey=<key>`, `solutionKey=<key> solution=<path>`, `repoPath=<dir>` or `stale`); the
resolved key, or the refusal's kind, or `-`; the gate outcome (`passed` or the gate's name); the child's exit code,
`-`, or `timeout`; the child's summary or the refusal text, verbatim, newlines removed. A `--stale` run writes one
line per solution launched. A line that cannot be written never stops the run: the result carries `logged: false`
and `logError`. The log is also what `map_status` reads for `notInMap`: every line whose resolution is
`PathNotInMap` or `AmbiguousSolutionFile`, whatever its age, one entry per directory, the latest line's time and
origin, dropped once a mapped root contains it. The bridge never rewrites or truncates the log.

## Refusals and remedies

Every refusal is a short text naming what was wrong and what to do; the phrases in bold are the ones the tests
assert (contracts/tools.md §6). Twenty-nine kinds:

| Kind | What it says | Remedy |
|---|---|---|
| `Unconfigured` | The bridge is **not configured**: `mapPath` is missing from the config file, the file is missing, or it is not JSON. | Fix `bridge.config.json` beside the executable (or the `--config` file) and call again; nothing was opened. |
| `ConfigKeyRetired` | `storePath` is **no longer a key** of the config file: the bridge opens no store. | Remove the line and call again; nothing was opened. |
| `MapAbsent` | **No CodeMem map exists** at `mapPath`. | Fix `mapPath`, or run the extractor once to create the map. The bridge never creates one. |
| `NotAMap` | The file at `mapPath` is **not a CodeMem map** (no map identity). | Point `mapPath` at a map the CodeMem extractor produced. |
| `VersionBelow` | The map's schema version is below the one this bridge requires (3). | **Run the CodeMem extractor once: it upgrades the map in place**; the bridge is read-only and cannot. |
| `VersionAbove` | The map's schema version is above the one this bridge requires. | **Revise the bridge against the newer contract**; nothing to do on the map's side. |
| `Busy` | **An extraction is in progress**; the map was not readable within the timeout. | Retry when it finishes; normal for a large solution. |
| `Unopenable` | The map **could not be opened**; the driver's text follows. | Check the path and its permissions. |
| `ScopeMissing` | **Supply solutionKey** — one map solution, exact. | `solutions` lists the keys. |
| `ProjectIdRemoved` | **projectId is not an argument** of this bridge. | Scope by `solutionKey`; MemOS's own codemem tools take a project id. |
| `FilterMissing` | Supply a **name, a kind, or both**. | A search with no filter is not run; give a filter. |
| `KindUnknown` | The given kind is **not a symbol kind** in the map. | Use one of the fourteen kinds the text lists. |
| `SymbolIdMissing` | Supply **symbolId**. | Find the id with `symbol_search` and pass it. |
| `KindNotExamined` | The given kind is one `orphans` **does not examine** (project, namespace). | Filter by an examined kind, or omit `kind`. |
| `SolutionKeyUnknown` | The map **holds no solution with key** so-and-so. | Keys are exact; `solutions` lists them. To add a solution: `extract --solution-key <key> --solution <path to its .sln or .slnx>`. |
| `SymbolNotFound` | The map **holds no symbol with id** so-and-so. | Find the id with `symbol_search`. |
| `SymbolOutOfScope` | The symbol belongs to a solution **not in the requested scope**. | Ask with that solution's key. |
| `SymbolRetired` | The symbol is **retired**; last seen in a named run. | Search again for the current symbol, or consult the rename candidates whose retired symbol it is. |
| `NotAProjectRow` | The given `projectSymbolId` is **not a project row**. | Pass the id of a project-kind symbol — the ids the result's `byProject` lists. |
| `NotAType` | The symbol is **not a type**. | `type_usages` takes a class, module, structure, interface, enum or delegate; **use references** for a member. |
| `GateOff` | extract is refused: **the named gate is false** in the config file. | The Architect flips it; nothing ran and the map is unchanged. |
| `TargetMissing` | Supply **exactly one of solutionKey, repoPath or stale**; solutionPath only beside solutionKey. | Give one target. |
| `PathNotInMap` | The directory is **not in the map**: no mapped solution's root contains it; the mapped roots are listed, the command that adds it is printed. | Run the printed command with the key you want; nothing ran and nothing was added. |
| `AmbiguousSolutionFile` | The directory is not in the map and holds **more than one solution file**; both are named, no key is suggested. | Choose one: `extract --solution-key <key> --solution <that file>`. |
| `AmbiguousRoot` | The path lies under a root shared by **more than one mapped solution**. | Name the `solutionKey`. |
| `ExtractionRunning` | **An extraction launched by this bridge is still running** for the named key. | Wait for its result; one extraction at a time per bridge process. |
| `ExtractorNotFound` | The extractor was **not found** at the named path. | Set `extractorPath` in the config, or build `CodeMem.sln` so `CodeMem.Extractor.dll` lands beside the executable. |
| `AmbiguousTarget` | The build names **more than one target**. | The hook resolves none of them and never falls back to the working directory; build one solution or project per command. |
| `ChildTimedOut` | The extractor **exceeded 540 s** and was stopped. | The map holds whatever the child published before and nothing after. Run the extractor by hand to see why. |

Seven kinds of 004 are **retired** with the store read and do not occur: `RegistryAbsent`, `ScopeConflict`,
`KeyNotRegistered`, `KeyUnbound`, `KeyInactive`, `MapMissingSolution` and `PathNotRegistered`; their texts are kept
in `_Archive/004-store/README.md`. The registry-era `no registry row names project N` text left with `projectId`.

## What the bridge never does

- Never opens a second database: the map is the one file it reads; no connection is held across calls, no cache,
  no `-journal` left beside the map; the only file it writes is `extract.log`.
- Never writes the map.
- Never adds a solution and never chooses a key: `PathNotInMap` prints the command and stops; the Architect runs it.
- Never walks up or down from a directory: the suggestion inspects the directory given, and resolution asks only
  whether a mapped root contains it.
- Never uses `LIKE` in an identity query, never takes a doc-comment id as input, never returns a retired symbol,
  never counts `part_of` as a reference, never guesses in `map_status`.
- Never extracts without a key the map holds or a path the Architect gave: a directory resolves to a mapped root or
  is answered not in the map; the hook never falls back to the session directory.
- Never fails the tool call it observed: the hook's every outcome, refusal, child failure or exception, is one line
  of `additionalContext` and exit 0.
- Never installs itself: `.mcp.json` is the repository's; the hook fragment is merged by the Operator's hand; the
  gates are flipped by the Architect.
- Never changes the extractor: it is launched as a black box, its exit code and summary line reported verbatim,
  killed after 540 s and reported as timed out, never as a success.

## The extractor

Two things changed in the extractor beside this feature (version 0.3.0, contracts/extractor.md):

- A `.slnx` is an equal input: `--solution <path.sln|path.slnx|path.vbproj>`. The loader parses the `.slnx` itself
  (a `Solution` root, every `Project`'s `Path` at any depth, folders and configurations not read) and opens each
  project into one workspace; a path that does not exist is refused naming it before any project is opened; the
  same projects named by a `.sln` and a `.slnx` yield the same fact set. The key still defaults to the file name
  without extension; `last_seen_path` records the file passed.
- A NuGet restore **warning** (NU1701 and the like) no longer fails the load: it is classified through the
  project's `obj/project.assets.json`, recorded on the run (`extract_run_warnings`, schema version 3; the summary
  line ends `warnings=<n>`) and the compiler judges the code as always. A restore **error** (NU1101, NU1301) still
  stops the load, naming the code and the project, before the compiler; a compile error still exits 2. The
  extractor reads no environment variable for this and passes no `NoWarn`. One thing it cannot undo: a `NoWarn`
  variable already in the environment reaches MSBuild itself, which then suppresses the warning before the
  extractor sees it — a launch that wants the warnings recorded must not carry one.

A map at schema version 2 is upgraded to 3 in place by the extractor's next run; until then the bridge answers
`VersionBelow`.

## Archive

Everything 004 built to read MemOS's `code_map_solutions` registry — the store database, the registry module and
its exception, the registry record, the bridge's store door, the two registry envelopes and the registry test
fixture — lives under `_Archive/004-store/`, mirrored by path, with a README naming what moved, from where, why
(152687; constitution v1.4.0), the seven retired refusal texts and the retired test facts. Nothing under `_Archive/`
is compiled or scanned; `BridgeStandaloneGateTests` proves the eight names absent from every `.vb` file of the tree.

## A recorded non-choice: the `mcp_tool` hook type

In the 2026-09-15 spike (specs/004-codemem-bridge/spike.md, fact 2) a hook of type `mcp_tool` produced no
observable effect, while a `command` hook on `PostToolUse` with matcher `Bash` ran and its `additionalContext`
reached the session. The fragment therefore uses the command hook, and the `mcp_tool` type is not retried (STOP 1,
ruling 3). `PostToolUseFailure` is not subscribed: a red build never reaches the bridge.

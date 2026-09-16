# CodeMem.Bridge — the process document

This is the document contracts/cli-config-hook.md §6 asks for (FR-339): what the bridge is, how it is installed,
when each tool is called, the gates, the log, the refusals a user will meet and their remedies, what the bridge
never does, and one recorded non-choice.

## What the bridge is

The bridge is Claude Code's source for the CodeMem code map, and the green-compile trigger that keeps the map
current. It is one process, `CodeMem.Bridge.exe`, built from two projects: `CodeMem.Bridge` (the executable: the
command line, the MCP server) and `CodeMem.Bridging` (the tools, the readers, the extract door). It serves eight
MCP tools over stdio — `solutions`, `symbol_search`, `symbol_detail`, `references`, `orphans`, `type_usages`,
`map_status` and `extract` — reading `codemem.sqlite` (the map) and the `code_map_solutions` table of `memos.sqlite`
(the registry), both opened read-only per call and closed before the reply. The bridge never writes the map: the
CodeMem extractor is the sole writer, and `extract` only launches it as a child process. Decisions 142362 (the
bridge is Claude Code's source; a green compile is its trigger), 137077 (serve and extract in one surface; the
extract verb is permissive-gated) and 152658 (stdio; a PostToolUse hook; two gates, both off, dependent) bind this
document.

## Install

1. **Build**: `dotnet build CodeMem.sln -c Release`. The executable is
   `src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe`; the extractor, `CodeMem.Extractor.dll`, lands
   beside it.
2. **Configure**: copy `src/CodeMem.Bridge/bridge.config.sample.json` beside the executable as
   `bridge.config.json` and set the two paths:

   ```json
   {
     "mapPath": "C:\\_DB\\codemem.sqlite",
     "storePath": "C:\\_DB\\memos.sqlite",
     "extractorPath": null,
     "extract": { "enabled": false, "onGreenBuild": false }
   }
   ```

   `extractorPath` null means `CodeMem.Extractor.dll` beside the executable. The file is read on every call, so an
   edit takes effect on the next call without a restart; a file that is not JSON, or a missing key, is the
   `Unconfigured` refusal naming the file.
3. **Approve the project server**: the repository's `.mcp.json` registers `codemem` (stdio, `CodeMem.Bridge.exe
   serve`) at project scope. The first interactive Claude Code session in `repos\CodeMem` asks for approval;
   `claude mcp list` then shows it connected.
4. **Other repositories**: a session in GameRoom or MemOS needs the user-scope registration with the absolute
   path:

   ```text
   claude mcp add --scope user --transport stdio codemem -- "C:/Users/rchau/source/repos/CodeMem/src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe" serve
   ```

5. **The hook**: merge `src/CodeMem.Bridge/hooks/settings.fragment.json` into `~/.claude/settings.json` by hand
   (user scope, so a build in any registered repository's session reaches it). Nothing in this feature installs
   it.
6. **Flip the gates**: both ship off. The Architect flips `extract.enabled`, then `extract.onGreenBuild`, in
   `bridge.config.json`; see the gates below.

Command line, for reference (contracts/cli-config-hook.md §1):

```text
CodeMem.Bridge [serve] [--config <path>]
CodeMem.Bridge extract (--solution-key <key> | --repo-path <dir> | --stale) [--on-green-build] [--config <path>]
CodeMem.Bridge hook [--config <path>]
CodeMem.Bridge --help
```

`serve` is the MCP server (nothing but the transport ever writes to its stdout). `extract` prints one JSON
document and exits 0 when the request was accepted and the child ran (the child's own exit code is inside the
JSON), 2 when the request was refused. `hook` reads the PostToolUse event from stdin, prints one JSON line and
always exits 0.

## When each tool is called

- **`map_status`** — at the start of a session, and again before relying on `references`, `orphans` or
  `type_usages`. It says, per registered solution, whether the map is `current`, `behind` (commits past the
  recorded one), `dirty` (uncommitted changes), `no_git` or `map_missing_solution`, with the run's commit and HEAD
  named. A `behind` or `dirty` verdict means the map describes an older tree: read it as evidence, not as the
  truth of the working copy.
- **`type_usages`** — before any delete or rename of a type. `references` on a type's constructor or members is
  not the type's usage: `type_usages` folds constructor calls, member references, implements, extends, and the
  places the type is named, over the whole map.
- **`references`, `symbol_detail`, `symbol_search`, `orphans`, `solutions`** — as the map is read: search first,
  then detail or references by the symbol id the search returned. `orphans` examines the kinds the map records as
  examined; project and namespace rows are roots and are not orphans by construction.
- **`extract`** — by the hook, on a green `dotnet build` or `dotnet test`, never by Claude Code directly. The tool
  is registered so the hook and the tests can reach the door; a session that wants a fresh map builds the solution
  and lets the hook run.
- **`CodeMem.Bridge extract --stale`** — by a human, from the command line: it reads `map_status` and extracts, one
  after another, every registered solution that is `behind` or `dirty`, and lists the ones it left alone.
- **`--on-green-build`** — never by a human. It exists so the `hook` entry and the tests reach the green-build
  origin through the same door; a human passing it only reaches the second gate.

## The gates and who flips them

Two booleans in `bridge.config.json`, read on every call:

| Gate | Governs | Off means |
|---|---|---|
| `extract.enabled` | the verb, every origin (tool, manual, green build) | every `extract` is refused naming `extract.enabled`; nothing launches; the map is unchanged |
| `extract.onGreenBuild` | the trigger only | the hook's extract is refused naming `extract.onGreenBuild`; tool and manual extracts still run; inert while `enabled` is off |

The Architect flips them, in that order. The gate is checked before the registry or the map is opened, so a
refused `extract` reveals nothing about either. A refusal is an answer, not an error: the tool call the hook
observed never fails.

## The extract log

`extract.log` beside the executable (`AppContext.BaseDirectory`, whatever `--config` names), one line per `extract`
invocation, appended, tab-separated, no header. Columns: UTC time; origin (`tool`, `manual`, `green_build`); the
target as given; the resolved key or `-`; the gate outcome (`passed` or the gate's name); the child's exit code,
`-`, or `timeout`; the child's summary or the refusal text, verbatim, newlines removed. A `--stale` run writes one
line per solution launched. A line that cannot be written never stops the run: the result carries `logged: false`
and `logError`.

## Refusals and remedies

Every refusal is a short text naming what was wrong and what to do; the phrases in bold are the ones the tests
assert (contracts/tools.md §6).

| Kind | What it says | Remedy |
|---|---|---|
| `Unconfigured` | The bridge is **not configured**: a key is missing from the config file, or the file is not JSON. | Fix `bridge.config.json` beside the executable (or the `--config` file) and call again; nothing was opened. |
| `MapAbsent` | **No CodeMem map exists** at `mapPath`. | Fix `mapPath`, or run the extractor once to create the map. The bridge never creates one. |
| `NotAMap` | The file at `mapPath` is **not a CodeMem map** (no map identity). | Point `mapPath` at a map the CodeMem extractor produced. |
| `VersionBelow` | The map's schema version is below the one this bridge requires. | **Run the CodeMem extractor once: it upgrades the map in place**; the bridge is read-only and cannot. |
| `VersionAbove` | The map's schema version is above the one this bridge requires. | **Revise the bridge against the newer contract**; nothing to do on the map's side. |
| `Busy` | **An extraction is in progress**; the map was not readable within the timeout. | Retry when it finishes; normal for a large solution. |
| `Unopenable` | The map **could not be opened**; the driver's text follows. | Check the path and its permissions. |
| `RegistryAbsent` | The store holds **no code_map_solutions table**. | Nothing is wrong with the map; the registry migration has not gone live on that store. Scope by `solutionKey` until it has. |
| `ScopeMissing` | Supply **exactly one of projectId** or solutionKey; **neither** was supplied. | Give one of them. |
| `ScopeConflict` | **Both** projectId and solutionKey were supplied. | Give one of them; they can disagree, so the bridge chooses neither. |
| `FilterMissing` | Supply a **name, a kind, or both**. | A search with no filter is not run; give a filter. |
| `KindUnknown` | The given kind is **not a symbol kind** in the map. | Use one of the fourteen kinds the text lists. |
| `SymbolIdMissing` | Supply **symbolId**. | Find the id with `symbol_search` and pass it. |
| `SolutionKeyUnknown` | The map **holds no solution with key** so-and-so. | Keys are exact; `solutions` lists them. |
| `SymbolNotFound` | The map **holds no symbol with id** so-and-so. | Find the id with `symbol_search`. |
| `SymbolOutOfScope` | The symbol belongs to a solution **not in the requested scope**. | Ask with that solution's key, or with a project bound to it. |
| `SymbolRetired` | The symbol is **retired**; last seen in a named run. | Search again for the current symbol, or consult the rename candidates whose retired symbol it is. |
| `KindNotExamined` | The given kind is one `orphans` **does not examine** (project, namespace). | Filter by an examined kind, or omit `kind`. |
| `NotAProjectRow` | The given `projectSymbolId` is **not a project row**. | Pass the id of a project-kind symbol — the ids the result's `byProject` lists. |
| `NotAType` | The symbol is **not a type**. | `type_usages` takes a class, module, structure, interface, enum or delegate; **use references** for a member. |
| `GateOff` | extract is refused: **the named gate is false** in the config file. | The Architect flips it; nothing ran and the map is unchanged. |
| `TargetMissing` | Supply **exactly one of solutionKey, repoPath or stale**. | Give one target. |
| `PathNotRegistered` | **No registered solution's repository root contains** the path; the registered roots are listed. | Build inside a registered repository, or register the solution in MemOS. The session directory is never tried in the path's place. |
| `AmbiguousRoot` | The path lies under a root bound to **more than one** registered solution. | Name the `solutionKey`. |
| `KeyNotRegistered` | **No code_map_solutions row** has that solution key. | Keys are exact; `solutions` lists the map's, `map_status` the registry's. |
| `KeyUnbound` | The registry row has **no codemem_solution_id**: never published. | Run the extractor by hand once with `--solution-key <key>` and bind the row in MemOS; the bridge does not extract an unbound solution. |
| `KeyInactive` | The registry row is in a state other than active. | Reactivate the row in MemOS, or leave it retired. |
| `MapMissingSolution` | The registry row binds a map solution id the map **holds no solution** for. | Re-bind the row in MemOS to the solution the map holds (`solutions` lists them), or run the extractor for that key. |
| `ExtractionRunning` | **An extraction launched by this bridge is still running** for the named key. | Wait for its result; one extraction at a time per bridge process. |
| `ExtractorNotFound` | The extractor was **not found** at the named path. | Set `extractorPath` in the config, or build `CodeMem.sln` so `CodeMem.Extractor.dll` lands beside the executable. |
| `AmbiguousTarget` | The build names **more than one target**. | The hook resolves none of them and never falls back to the working directory; build one solution or project per command. |
| `ChildTimedOut` | The extractor **exceeded 540 s** and was stopped. | The map holds whatever the child published before and nothing after. Run the extractor by hand to see why. |

## What the bridge never does

- Never writes the map or the store: no connection is held across calls, no cache, no `-journal` left beside
  the map; the only file it writes is `extract.log`.
- Never uses `LIKE` in an identity query, never takes a doc-comment id as input, never returns a retired symbol,
  never counts `part_of` as a reference, never guesses in `map_status`.
- Never extracts without the registry's key: a path resolves to a registered solution's root or is refused; the
  hook never falls back to the session directory; an unbound or inactive registry row is refused by name.
- Never fails the tool call it observed: the hook's every outcome, refusal, child failure or exception, is one line
  of `additionalContext` and exit 0.
- Never installs itself: `.mcp.json` is the repository's; the hook fragment is merged by the Operator's hand; the
  gates are flipped by the Architect.
- Never changes the extractor: it is launched as a black box, its exit code and summary line reported verbatim,
  killed after 540 s and reported as timed out, never as a success.

## A recorded non-choice: the `mcp_tool` hook type

In the 2026-09-15 spike (specs/004-codemem-bridge/spike.md, fact 2) a hook of type `mcp_tool` produced no
observable effect, while a `command` hook on `PostToolUse` with matcher `Bash` ran and its `additionalContext`
reached the session. The fragment therefore uses the command hook, and the `mcp_tool` type is not retried (STOP 1,
ruling 3). `PostToolUseFailure` is not subscribed: a red build never reaches the bridge.

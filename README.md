<div align="center">

# CodeMem

### Your codebase as the compiler sees it — in one SQLite file your AI assistant can actually read.

**Stop letting it grep.** CodeMem compiles your solution, records every symbol and every edge the compiler
resolved, and serves that map to Claude Code over MCP — read-only, and honest about how stale it is.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
![Roslyn](https://img.shields.io/badge/Roslyn-VB.NET-5C2D91)
![SQLite](https://img.shields.io/badge/SQLite-schema%20v3-003B57?logo=sqlite&logoColor=white)
![MCP](https://img.shields.io/badge/MCP-8%20tools-FF6B35)
![tests](https://img.shields.io/badge/tests-197%20passing-2ea44f)

</div>

---

## The problem

Ask an assistant *"is anything still using `AudioBuffer`?"* and it runs a text search. It gets forty-seven hits —
comments, a string literal, a variable that happens to share the name, three overloads it can't tell apart — and
misses the call that goes through an interface. Then it tells you the type is safe to delete.

It can't do better. Text has no idea what the compiler knows.

## The answer

```text
 your solution         CodeMem.Extractor          codemem.sqlite          CodeMem.Bridge         Claude Code
 .sln · .slnx   ──▶    compiles it first   ──▶    symbols · edges   ──▶   read-only, stdio  ──▶  8 MCP tools
 .vbproj               nothing red is kept        runs · renames          never writes
```

One console tool writes the map. One bridge reads it. One file between them — no service, no daemon, no state held
between calls.

| You ask | grep answers | CodeMem answers |
|---|---|---|
| *Who calls `Process()`?* | every file containing the letters | the calls the compiler resolved, per overload, with file, line and verb |
| *Was this renamed, or deleted?* | it can't tell | a **rename candidate**: this symbol retired, that one arrived, same shape |
| *Safe to delete this type?* | you guess | `type_usages`: every constructor call, member use, `implements` and `extends` — with `fromOutside` |
| *Is this answer even current?* | it can't tell | `current`, `behind by 24 commits`, `dirty`, `no_git`, `diverged` — per solution |

---

## See it work

**Put a solution in the map.** One command, and the extractor does the rest:

```console
$ CodeMem.Bridge extract --solution-key DSP_Processor --solution C:\src\DSP_Processor\DSP_Processor.slnx

solution=DSP_Processor run_id=16 observed=2811 matched=2811 reactivated=0 new=0 retired=0
registry_before=2811 notes_orphaned=0 candidates=0 unaccounted_observed=0 unaccounted_registry=0
digest=80199c40… sha=98c81d07… warnings=0
```

Every count on that line has to close. A run whose arithmetic doesn't balance exits non-zero and says which side is
short — no silent drift, ever.

**Your assistant checks the map before it trusts it.** `map_status`, the first call of a session:

```jsonc
{ "solutionKey": "DSP_Processor", "verdict": "current",
  "reason": "HEAD equals the recorded 98c81d07 and the tree is clean." }

{ "solutionKey": "CodeMem", "verdict": "dirty", "head": { "behindBy": 24, "treeDirty": true },
  "reason": "HEAD e284ac53 is 24 commits past the recorded f6488469 and the working tree has
             uncommitted or untracked changes." }
```

A stale map still answers — it just tells you it's describing an older tree. Nothing is guessed, and a repository it
can't read says so.

**A repo it doesn't know yet doesn't get invented.** It gets a refusal with the exact command that fixes it:

```jsonc
"notInMap": [{
  "path": "C:\\src\\vbCalc\\", "suggestedKey": "vbCalc",
  "command": "extract --solution-key vbCalc --solution C:\\src\\vbCalc\\vbCalc.slnx"
}]
```

**And it keeps itself current.** With the hook installed, a green `dotnet build` re-extracts that solution before
your assistant's next question — and stays out of the way when there's nothing to do:

```text
codemem hook: not a dotnet build or test; nothing ran
```

---

## What you get

|  |  |
|---|---|
| 🧠 **The compiler's answer, not a text match** | Eight edge verbs — `part_of`, `calls`, `uses`, `implements`, `extends`, `imports`, `depends_on`, `handles` — resolved by identity. Overloads stay distinct; a call through an interface still counts. |
| 🔗 **Identity that survives a rename** | A symbol's hash covers its token text with its *own* name excluded. Rename a method and the hash holds: you get a rename candidate, not a phantom delete plus a phantom add. |
| ✅ **Never a red build** | Compile errors exit 2 and the map is untouched. What's in the map compiled. |
| 🧮 **Counts that reconcile** | Observed, matched, new, retired, reactivated, prior registry — written on every run and required to balance. A number in the map is evidence or an error, never an estimate. |
| 🕒 **Staleness is a first-class answer** | Every run stamps its commit and dirty state; `map_status` compares to HEAD *now*, per solution, and names the gap. |
| 🔒 **Read-only by construction** | The bridge opens one file, per call, and closes it before replying. No cache, no held connection, no second database, no writes. |
| 🗂️ **Many solutions, one file** | Five repositories and ~23,000 symbols live in a single `codemem.sqlite` here. Add one with a key and a path. |
| 🧾 **Refusals that tell you what to do** | Twenty-nine named kinds. `VersionBelow` names the remedy, `PathNotInMap` prints the command, `Busy` says an extraction is in flight. An error is an answer, not a stack trace. |

---

## What's in the map

One SQLite file, schema version 3, many solutions, exactly one writer.

| Table | What it holds |
|---|---|
| `map_identity` | The file's GUID and schema version — what makes it *a CodeMem map* and not some other database. |
| `solutions` | One row per solution: key, name, repository root, the solution file last seen. |
| `extract_runs` | Every run: outcome, source digest, commit and dirty state, SDK version, timings, the ten reconciliation counts. |
| `code_symbols` | Every source-declared symbol in fourteen kinds — `namespace`, `class`, `module`, `structure`, `interface`, `enum`, `enum_member`, `delegate`, `method`, `constructor`, `property`, `field`, `event`, `project` — with doc-comment id, span, body hash, active flag, first and last run seen. |
| `code_parts` | The pieces a symbol's hash is built from; a partial type has several. |
| `code_edges` | Every occurrence under the eight verbs, with file and position. An edge carries the target's doc-comment id even when the target is external, so a call into the framework is still recorded; a `handles` edge names the `WithEvents` member it travels through. |
| `rename_candidates` | Retired symbol, arrived symbol, and why the run thinks they're the same. Proposed — never applied. |
| `extract_run_warnings` | NuGet restore *warnings* recorded on the run instead of failing it. A restore *error* still stops the load, naming the code and the project. |

Retired symbols are marked inactive, never deleted — so a question asked against an older run still lands on rows.

## The tools your assistant gets

Eight, over stdio, every one scoped to a `solutionKey`.

| Tool | Answers |
|---|---|
| **`map_status`** | Is the map current with the working tree? Plus every directory an extract refused as unmapped, with the command that adds it. |
| **`solutions`** | Every solution with its latest run: outcome, commit, dirty state, the ten counts, the warning count. |
| **`symbol_search`** | Find symbols by name, kind, or both — across a solution or one project. |
| **`symbol_detail`** | One symbol whole: its parts, and its inbound and outbound edges under all eight verbs. |
| **`references`** | Who reaches this symbol, by compiler identity. Containment never counts as a reference. |
| **`type_usages`** | Everything recorded against a *type* — constructors, members, `implements`, `extends`, bare names — with the `fromOutside` number to read before you delete it. |
| **`orphans`** | Active symbols nothing recorded reaches. An orphan is *unreferenced*, not *dead* — and the tool says so, naming the live code it can't see (entry points, tests, reflection, `Overrides`). |
| **`extract`** | Refresh the map: by the green-build hook, or by hand with a key and a path. |

Full reference — installation, gates, the log, all twenty-nine refusals — in the
[process document](src/CodeMem.Bridge/README.md).

---

## Quick start

**You'll need** the .NET SDK (projects target `net8.0`; recorded runs use 10.0.401), a Visual Basic solution, and
Windows — the extractor drives `MSBuildWorkspace` and the bridge ships as an `.exe`.

**1 · Build**

```powershell
dotnet build CodeMem.sln -c Release
```

`CodeMem.Bridge.exe` and `CodeMem.Extractor.dll` both land in `src\CodeMem.Bridge\bin\Release\net8.0\`.

**2 · Create the map.** The bridge never creates one, so the first run goes straight to the extractor:

```powershell
dotnet src\CodeMem.Bridge\bin\Release\net8.0\CodeMem.Extractor.dll `
    --solution C:\src\MyApp\MyApp.slnx --db C:\_DB\codemem.sqlite --solution-key MyApp
```

Exit 0 on a clean compile, 2 on compile errors, 4 if the counts don't reconcile.

**3 · Point the bridge at it.** Copy `bridge.config.sample.json` beside the executable as `bridge.config.json`:

```json
{
  "mapPath": "C:\\_DB\\codemem.sqlite",
  "extractorPath": null,
  "extract": { "enabled": false, "onGreenBuild": false }
}
```

Read on every call — edit it and the next call sees the change, no restart.

**4 · Register it with Claude Code.** This repo's `.mcp.json` covers sessions here; for every other repository:

```powershell
claude mcp add --scope user --transport stdio codemem -- "C:/path/to/CodeMem.Bridge.exe" serve
```

**5 · Keep it fresh (optional).** Merge `src/CodeMem.Bridge/hooks/settings.fragment.json` into
`~/.claude/settings.json`, then flip `extract.enabled` and `extract.onGreenBuild`. Every green build re-extracts that
solution. Both gates ship **off** — nothing here installs itself or turns itself on.

## Command lines

```text
CodeMem.Bridge [serve] [--config <path>]
CodeMem.Bridge extract (--solution-key <key> [--solution <path>] | --repo-path <dir> | --stale)
                       [--on-green-build] [--config <path>]
CodeMem.Bridge hook [--config <path>]

CodeMem.Extractor --solution <path.sln|path.slnx|path.vbproj> --db <path to codemem.sqlite>
                  [--configuration Debug|Release] [--framework <tfm>] [--solution-key <name>]
```

`--solution-key <key> --solution <path>` adds a solution the map has never seen. `--repo-path` resolves a directory
to a solution **from the map alone** — no registry, no lookup anywhere else. `--stale` re-extracts everything
`map_status` reports as behind or dirty.

## Layout

| Path | |
|---|---|
| `src/CodeMem.Core` | The map: schema, DDL, repositories. The only place SQL lives. |
| `src/CodeMem.Extraction` | Workspace loading (`.sln`, `.slnx`, `.vbproj`), the extraction run, reconciliation. |
| `src/CodeMem.Extractor` | The extractor's command line. |
| `src/CodeMem.Bridging` | The bridge's library: readers, tools, refusals, the extract door. |
| `src/CodeMem.Bridge` | The executable — MCP server, CLI, hook entry — and the [process document](src/CodeMem.Bridge/README.md). |
| `tests/CodeMem.Tests` | xUnit: 197 passing, 9 skipped (live-map facts, armed with `CODEMEM_LIVE_MAP`). |
| `specs/` | One folder per feature: spec, plan, research, contracts, tasks, implementation record. |
| `_Archive/` | Retired code, kept where it can be read. Nothing here compiles. |

## How it's built

Spec-driven, and strictly. Every feature gets a numbered folder under `specs/` holding its specification, its
clarification rulings, a plan, per-task breakdowns, and — written as it's built — an implementation record naming
every failing test, its fix, and every deviation from the plan. Tests come first and carry dated RED/GREEN lines
proving they failed before they passed.

The rules the code answers to live in [`.specify/memory/constitution.md`](.specify/memory/constitution.md) — v1.4.0,
fifteen articles: *Compiler Fact Only*, *Green Only, Stamped*, *Reconcile, Never Truncate*, *Counts That Reconcile*,
*One File, Many Solutions, One Writer*, *Archive, Never Delete*. Where a specification and the constitution disagree,
the constitution wins and the specification is the defect.

## What it will never do

- **Write the map from the bridge.** The extractor is the sole writer; `extract` only launches it.
- **Open a second database.** One map file, read-only, per call. The only file the bridge writes is its own log.
- **Add a solution on its own, or invent a key.** An unmapped directory gets the command; a human runs it.
- **Guess.** A repository it can't read says so. A stale map says how stale. A retired symbol is never returned as
  if it were live.
- **Break the thing that triggered it.** The green-build hook's every outcome — refusal, child failure, exception —
  is one line of context and exit 0.

---

<div align="center">

**Status** · Features 001–005 merged · schema v3 · 197 passing / 9 skipped on Debug and Release · five solutions and
~23,000 symbols in the live map.

</div>

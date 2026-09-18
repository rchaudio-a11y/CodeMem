# CodeMem

**A compiler-backed map of a .NET solution — every symbol, every reference, every run that produced them — in one
SQLite file, served read-only to an AI coding assistant over MCP.**

An assistant working in an unfamiliar codebase answers *who calls this?*, *is anything still using it?* and *what
breaks if I delete this type?* by grepping. Grep cannot tell an overload from a coincidence, does not know a method
was renamed rather than deleted, and never says how old its answer is.

CodeMem answers those questions from what the compiler knows. A console extractor loads the solution through Roslyn,
refuses to record anything unless it compiles clean, and writes the symbols, their relationships and the run's own
arithmetic into `codemem.sqlite` — *the map*. A second program, the bridge, serves that file to Claude Code as eight
MCP tools and never writes a byte of it.

Two programs, one file:

| | |
|---|---|
| **`CodeMem.Extractor`** | The map's **only** writer. Loads a `.sln`, `.slnx` or `.vbproj` through `MSBuildWorkspace`, requires zero compile errors, reconciles what it observed against what the map already held, and stamps the run with the commit it saw. |
| **`CodeMem.Bridge`** | A read-only MCP server over stdio, plus the command line that launches the extractor. Opens the map per call and closes it before replying: no cache, no held connection, no second database. |

The map file is the entire interface between them. There is no service to keep running and no state held between
calls.

## Why a map and not a grep

- **The compiler's answer.** Edges come from the semantic model, under eight verbs — `part_of`, `calls`, `uses`,
  `implements`, `extends`, `imports`, `depends_on`, `handles` — so an overload is distinguished from its siblings
  and a reference through an interface is still a reference. Symbols the compiler generates (implicit constructors,
  auto-property backing fields, `WithEvents` pairs) are deliberately not recorded.
- **Identity survives a rename.** A symbol's hash covers its token text with its *own* identifier excluded, so
  renaming a method leaves the hash intact. The run then records a **rename candidate** — this symbol retired, that
  one arrived, same shape — instead of an unrelated deletion and addition.
- **Nothing is recorded from a red build.** Compile errors exit 2 and the map is untouched (*Green Only, Stamped*).
- **Every run reconciles or fails.** Observed, matched, new, retired, reactivated and the registry's prior count are
  written on the run and must close; a run whose counts do not balance exits 4 and says which side is short. A
  number in the map is evidence or an error, never an estimate.
- **The map knows how stale it is.** Each run stamps the commit and the working tree's dirty state. `map_status`
  compares that to HEAD *now* and answers `current`, `behind` (by N commits, HEAD named), `dirty`, `no_git` or
  `diverged` — per solution, guessing nothing. An assistant is told when it is reading an older tree instead of
  quietly trusting it.

## What the map holds

One SQLite file, schema version 3, many solutions, one writer.

| Table | What it is |
|---|---|
| `map_identity` | The file's own GUID and schema version — what makes a `.sqlite` file *a CodeMem map* rather than some other database. |
| `solutions` | One row per solution: key, name, repository root, the solution file last seen. |
| `extract_runs` | Every run: outcome, source digest, commit and dirty state, SDK version, start and finish, and the ten reconciliation counts. |
| `code_symbols` | Every source-declared symbol, in fourteen kinds (`namespace`, `class`, `module`, `structure`, `interface`, `enum`, `enum_member`, `delegate`, `method`, `constructor`, `property`, `field`, `event`, `project`), with its doc-comment id, span, body hash, active flag and the runs that first and last saw it. |
| `code_parts` | The pieces a symbol's hash is built from — a partial type's several declarations included. |
| `code_edges` | Every occurrence, under the eight verbs, with the file and position it was seen at. An edge carries the target's doc-comment id whether or not the map declares the target, so a call into the framework is still recorded; a `handles` edge also names the `WithEvents` member it travels through. |
| `rename_candidates` | Retired symbol, arrived symbol, and why the run thinks they are the same thing. Proposed, never applied. |
| `extract_run_warnings` | NuGet restore warnings (`NU1701` and the like) recorded on the run instead of failing it. A restore *error* still stops the load, naming the code and the project. |

A retired symbol is marked inactive, never deleted (*Reconcile, Never Truncate*), so a question asked against an
older run still has rows to land on.

## The MCP tools

Eight, all scoped by `solutionKey` — the key of a `solutions` row. All read-only except `extract`, which launches
the extractor as a child process.

| Tool | Answers |
|---|---|
| `solutions` | Every solution in the map with its latest run: outcome, commit, dirty state, the ten counts, the warning count. |
| `map_status` | Per solution, whether the map is current with the working tree — and, under `notInMap`, every directory an extract has refused as unmapped, with the exact command that would add it. |
| `symbol_search` | Find symbols by name, kind, or both, within a solution or one of its projects. |
| `symbol_detail` | One symbol by id: kind, container, file and span, activity, the runs that bound it. |
| `references` | Who reaches this symbol. `part_of` never counts as a reference. |
| `type_usages` | Everything that touches a *type* — constructor calls, member references, implements, extends, and the places it is simply named. This is the question to ask before a delete or a rename; `references` on a constructor is not it. |
| `orphans` | Active symbols no recorded reference reaches, counted per project and per kind. An orphan is a symbol with no recorded reference — not a verdict that it is dead, and the tool says so: entry points, test methods, `Overrides` members called through a base and anything reached by reflection all surface here. |
| `extract` | Refresh the map. Called by the green-build hook, or by hand with a key and a solution path. |

Refusals are answers, not errors: twenty-nine named kinds, each a short text saying what was wrong and what to do
about it — `VersionBelow` tells you to run the extractor once, `PathNotInMap` prints the command that would add the
repository, `Busy` tells you an extraction is in flight. The full table is in
[src/CodeMem.Bridge/README.md](src/CodeMem.Bridge/README.md).

## Getting started

**Prerequisites** — the .NET SDK (the projects target `net8.0`; recorded runs use SDK 10.0.401), a Visual Basic
solution to map, and Windows: the extractor drives `MSBuildWorkspace` and the bridge ships as an `.exe`.

**1. Build.**

```powershell
dotnet build CodeMem.sln -c Release
```

`CodeMem.Bridge.exe` and `CodeMem.Extractor.dll` both land in `src\CodeMem.Bridge\bin\Release\net8.0\`.

**2. Create the map.** The bridge never creates one — the first run goes to the extractor directly:

```powershell
dotnet src\CodeMem.Bridge\bin\Release\net8.0\CodeMem.Extractor.dll `
    --solution C:\src\MyApp\MyApp.slnx `
    --db C:\_DB\codemem.sqlite `
    --solution-key MyApp
```

It prints one summary line — `run_id`, the ten counts, the source digest, the commit, `warnings=N` — and exits 0 on
a clean compile, 2 on compile errors, 4 if the counts do not reconcile.

**3. Configure the bridge.** Copy `src/CodeMem.Bridge/bridge.config.sample.json` beside the executable as
`bridge.config.json`:

```json
{
  "mapPath": "C:\\_DB\\codemem.sqlite",
  "extractorPath": null,
  "extract": { "enabled": false, "onGreenBuild": false }
}
```

Read on every call, so an edit takes effect without a restart. `extractorPath: null` means *the DLL beside the
executable*.

**4. Register it with Claude Code.** This repository's `.mcp.json` registers `codemem` at project scope. For
sessions in *other* repositories, register it at user scope:

```powershell
claude mcp add --scope user --transport stdio codemem -- "C:/path/to/CodeMem.Bridge.exe" serve
```

**5. Keep the map current (optional).** Merge `src/CodeMem.Bridge/hooks/settings.fragment.json` into
`~/.claude/settings.json` and flip `extract.enabled`, then `extract.onGreenBuild`, in `bridge.config.json`. A green
`dotnet build` or `dotnet test` in any mapped repository then re-extracts that solution automatically. Both gates
ship **off**; nothing installs itself.

## Command lines

```text
CodeMem.Bridge [serve] [--config <path>]
CodeMem.Bridge extract (--solution-key <key> [--solution <path>] | --repo-path <dir> | --stale)
                       [--on-green-build] [--config <path>]
CodeMem.Bridge hook [--config <path>]

CodeMem.Extractor --solution <path.sln|path.slnx|path.vbproj> --db <path to codemem.sqlite>
                  [--configuration Debug|Release] [--framework <tfm>] [--solution-key <name>]
```

`--solution-key <key> --solution <path>` is how a solution the map has never seen is added: the extractor creates
the row. `--repo-path` resolves a directory to a solution *from the map alone* — no registry, no lookup anywhere
else — and a directory no mapped root contains is refused with the command that would add it. `--stale` extracts
every solution `map_status` reports as behind or dirty.

## Repository layout

| Path | |
|---|---|
| `src/CodeMem.Core` | The map: schema, DDL, repositories. The only place SQL lives. |
| `src/CodeMem.Extraction` | Workspace loading (`.sln`, `.slnx`, `.vbproj`), the extraction run, reconciliation. |
| `src/CodeMem.Extractor` | The extractor's command line. |
| `src/CodeMem.Bridging` | The bridge's library: readers, tools, refusals, the extract door. |
| `src/CodeMem.Bridge` | The executable: MCP server, CLI, hook entry — and the [process document](src/CodeMem.Bridge/README.md), which is the reference for installation, tools, gates, the log and every refusal. |
| `tests/CodeMem.Tests` | xUnit. 197 passing, 9 skipped (live-map facts, armed with `CODEMEM_LIVE_MAP`). |
| `specs/` | One folder per feature: spec, plan, research, contracts, tasks, and the implementation record. |
| `_Archive/` | Code that was retired, kept where it can be read. Nothing here compiles. |

## How this project is built

Spec-driven, and strictly. Each feature gets a numbered folder under `specs/` holding its specification, its
clarification rulings, a plan, per-task breakdowns and — written as it is built — an implementation record naming
every failing test, its fix, and every deviation from the plan. Tests are written first and carry dated RED/GREEN
lines proving they failed before they passed.

The rules the code answers to live in [`.specify/memory/constitution.md`](.specify/memory/constitution.md) (v1.4.0,
fifteen articles): *Compiler Fact Only*, *Green Only, Stamped*, *Reconcile, Never Truncate*, *Counts That
Reconcile*, *One File, Many Solutions, One Writer*, *Archive, Never Delete*, and the rest. Where a specification and
the constitution disagree, the constitution wins and the specification is the defect.

## What it deliberately does not do

- **The bridge never writes the map.** The extractor is the sole writer; `extract` only launches it.
- **The bridge never opens a second database.** One map file, read-only, per call. The only file it writes is its
  own `extract.log`.
- **It never adds a solution on its own and never invents a key.** An unmapped directory is answered with the
  command that would add it, and a human runs it.
- **It never guesses.** A repository it cannot read says so; a stale map says how stale; a symbol it retired is not
  returned as if it were live.
- **It never fails the thing that triggered it.** The green-build hook's every outcome — refusal, child failure,
  exception — is one line of context and exit 0.

## Status

Features 001–005 are merged. The map is at schema version 3; the live map covers five solutions and roughly 23,000
symbols. The suite runs 197 passing / 9 skipped on Debug and Release.

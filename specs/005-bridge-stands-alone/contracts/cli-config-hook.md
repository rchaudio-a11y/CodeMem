# Contract: command line, configuration, hook and log after 005

**Date**: 2026-09-16 | **Spec**: [../spec.md](../spec.md) | Supersedes 004's `contracts/cli-config-hook.md` where the two differ;
§3 (`.mcp.json`) and §4 (the settings fragment) are 004's, byte for byte.

## 1. Command line

```text
CodeMem.Bridge [serve] [--config <path>]
CodeMem.Bridge extract (--solution-key <key> [--solution <path>] | --repo-path <dir> | --stale) [--on-green-build] [--config <path>]
CodeMem.Bridge hook [--config <path>]
CodeMem.Bridge --help
```

`--solution <path>` is accepted on the `extract` entry only, and only beside `--solution-key`; alone it is a usage error naming the
rule (exit 1, before the door). With it, the key need not be in the map: the extractor is launched with that file and creates the
`solutions` row when the key is new, or refreshes `last_seen_path` when it is not (Q3 as ruled). Without it, the map's last-seen path is
used, as in 004. Entries, origins, streams and exit codes are 004's (§1 table). The usage text names the shapes and says "there is no
--key; --solution-key is the one spelling".

**The `hook` entry's algorithm**: 004's six steps, with two changes. Step 4: the candidates are every token ending in `.sln`, `.slnx` or
`.vbproj` (case-insensitive). Step 5: the door resolves through the map alone (R65); a directory under no mapped root gets the not-in-map
answer, and the answer line carries its text — command included — so the session reads, in one line, what would add the solution.
The hook still never falls back to the working directory, still never launches the extractor itself, still exits 0.

## 2. `bridge.config.json`

```json
{ "mapPath": "C:\\_DB\\codemem.sqlite", "extractorPath": null, "extract": { "enabled": false, "onGreenBuild": false } }
```

Read on every call from `--config <path>` or `<exe directory>\bridge.config.json`, never cached. A missing file or a missing `mapPath` →
`Unconfigured` naming it; a file carrying `storePath` → `ConfigKeyRetired` naming the key, before anything is opened (FR-403); other
unknown keys are ignored. The shipped sample drops `storePath`; the Operator removes it from the file beside the Release executable.

## 5. `extract.log`

Unchanged in shape (seven tab-separated columns, appended, no header, beside the executable). The target-as-given column gains the
form `solutionKey=<key> solution=<path>`; the resolution column carries the new kinds `PathNotInMap` and `AmbiguousSolutionFile` on a
refusal. Example lines:

```text
2026-09-17T03:10:00.0Z	green_build	repoPath=C:\Users\rchau\source\repos\vbCalc	PathNotInMap	passed	-	refused: 'C:\Users\rchau\source\repos\vbCalc' is not in the map: … extract --solution-key vbCalc --solution C:\…\vbCalc.sln …
2026-09-17T03:12:41.7Z	manual	solutionKey=vbCalc solution=C:\Users\rchau\source\repos\vbCalc\vbCalc.sln	vbCalc	passed	0	solution=vbCalc run_id=12 observed=… sha=… warnings=0
```

The log is also what `map_status` reads for `notInMap` (R64): every line whose resolution is one of the two kinds, whatever its age,
one entry per directory, dropped once a mapped root contains it. The log is never rewritten and never truncated by the bridge.

## 6. The process document (`src/CodeMem.Bridge/README.md`)

Revised sections: **One file** (the bridge opens the map and nothing else; `memos.sqlite` is not a file it knows); **Install** (steps 1–6
minus the registry and bind steps; the config without `storePath`); **Command line** (§1 above); **When each tool is called** (`map_status`
now also lists `not_in_map`; `extract` with `--solution-key` and `--solution` is how a solution is added — by the Architect, who names
the key); **Not in the map** (what the hook's line says after a green build in an unmapped repository, and what to do); **The gates**
(unchanged); **The extract log** (§5); **Refusals and remedies** (the 29 kinds; the seven retired named as retired with a pointer to the
archive); **What the bridge never does** (gains: never opens a second database; never adds a solution; never chooses a key; never walks
up or down from a directory); **The extractor** (a `.slnx` is an equal input; a restore warning is recorded on the run at schema
version 3, a restore error still stops the load; `warnings=N` on the summary line); **Archive** (`_Archive/004-store/`, what and why).

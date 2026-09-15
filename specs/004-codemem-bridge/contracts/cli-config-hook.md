# Contract: the bridge executable, its configuration, `.mcp.json`, the hook fragment and the extract log

**Feature**: `004-codemem-bridge` | **Date**: 2026-09-15 | **Spec**: [../spec.md](../spec.md) | **Research**: [../research.md](../research.md) R42, R47–R49

## 1. Command line

```text
CodeMem.Bridge [serve] [--config <path>]
CodeMem.Bridge extract (--solution-key <key> | --repo-path <dir> | --stale) [--on-green-build] [--config <path>]
CodeMem.Bridge hook [--config <path>]
CodeMem.Bridge --help
```

| Entry | Origin | stdin | stdout | Exit code |
|---|---|---|---|---|
| `serve` (default) | tool (per MCP call) | the MCP client's JSON-RPC | the MCP transport only — nothing else ever writes to stdout | 0 when the client closes the pipe; 1 on a startup failure (stderr names it) |
| `extract …` | manual; `--on-green-build` → green-build | none | the extract result JSON (contracts/tools.md §3.8), one document | 0 when the request was accepted and the child ran (whatever the child's code — the code is in the JSON); 2 on a refusal (the JSON carries the kind); 1 on a usage error |
| `hook` | green-build | the PostToolUse event JSON from Claude Code | exactly one line: `{"hookSpecificOutput":{"hookEventName":"PostToolUse","additionalContext":"<one line>"}}` | **always 0** (FR-335: never fail the tool call it observed) |

Argument names are case-insensitive; values follow as the next argument (the extractor's `CommandLine` shape).
`--config` names the configuration file; without it the file is `bridge.config.json` beside the executable.
`--on-green-build` exists so the `hook` entry and the tests can reach the green-build origin through `extract`'s
door; the process document tells a human not to pass it.

**The `hook` entry's algorithm** (R42, R48; every step is a named refusal in the answer line, never an exception):

1. Read stdin to end; parse JSON. Unparseable → answer `codemem hook: payload not JSON; nothing ran.`
2. `hook_event_name` ≠ `PostToolUse` or `tool_name` ≠ `Bash` → answer `codemem hook: <event>/<tool> is not a
   Bash PostToolUse; nothing ran.` `tool_response.interrupted` true → `… interrupted; nothing ran.`
3. Split `tool_input.command` on `&&`, `;`, `|`; find the first segment beginning (after trim) with `dotnet build`
   or `dotnet test` (case-insensitive). None → answer `codemem hook: not a dotnet build or test; nothing ran.`
4. Effective directory: the `<dir>` of the last `cd <dir>` segment before the build segment, resolved against
   `cwd`; else `cwd`. Target, **by shape, not existence, with positional targets parsed after options** (COR2):
   tokenise the build segment, quoted tokens kept whole and quotes stripped; the candidates are every token ending in
   `.sln` or `.vbproj` (case-insensitive) anywhere after the verb — `dotnet build -c Debug CodeMem.sln` names the
   solution — plus the token immediately after the verb when it does not start with `-` and is not such a file (a
   positional directory). No candidate → the effective directory is the target (`dotnet build`, `dotnet build -c
   Debug`). One candidate → its directory (a file) or itself (a directory), resolved against the effective directory,
   never checked for existence. Two or more → `AmbiguousTarget` naming them; never resolved by fallback. A named path
   under no registered root is refused in step 5 naming that path; the effective directory is never tried in its
   place (Q6 as ruled).
5. Call the door with `origin = green_build`, `repoPath = target`. Answer with one line:
   `codemem extract (green build, <target>): <resolvedKey|refusal>: exit <code> — <summary or refusal line>` or
   `codemem extract (green build, <target>): refused — <refusal text>`.
6. Print the JSON, exit 0. The log line (§5) was written by the door in step 5.

## 2. `bridge.config.json` (Q8, R49)

```json
{
  "mapPath": "C:\\_DB\\codemem.sqlite",
  "storePath": "C:\\_DB\\memos.sqlite",
  "extractorPath": null,
  "extract": { "enabled": false, "onGreenBuild": false }
}
```

| Key | Type | Required | Meaning |
|---|---|---|---|
| `mapPath` | string | yes | the map; opened read-only per call; the extractor's `--db` on `extract` |
| `storePath` | string | yes | `memos.sqlite`; opened read-only per call; `code_map_solutions` only |
| `extractorPath` | string or null | no | `CodeMem.Extractor.dll` (run through `dotnet`) or `.exe`; default `<bridge directory>\CodeMem.Extractor.dll` |
| `extract.enabled` | boolean | no (default `false`) | the verb's gate, every origin |
| `extract.onGreenBuild` | boolean | no (default `false`) | the trigger's gate; inert while `enabled` is false |

Read on every call (`serve` never caches it); unknown keys are ignored; a file that is not JSON is `Unconfigured`
naming the file. `src/CodeMem.Bridge/bridge.config.sample.json` ships this text with the two live paths; the
Operator copies it beside the executable.

## 3. `.mcp.json` (repository root; R41)

```json
{
  "mcpServers": {
    "codemem": {
      "type": "stdio",
      "command": "src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe",
      "args": ["serve"]
    }
  }
}
```

Project scope: Claude Code asks the user to approve it on first interactive use in this repository (the spike saw
the `Pending approval` status) and launches the command with the repository root as its working directory, so the
relative path resolves (spike 1d) and the file is portable across checkouts. For a session in another repository,
the process document gives the user-scope registration with the absolute path:
`claude mcp add --scope user --transport stdio codemem -- "C:/Users/rchau/source/repos/CodeMem/src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe" serve`
— that line and the fragment's command (§4) are the two this machine's paths appear in.

## 4. The settings fragment (`src/CodeMem.Bridge/hooks/settings.fragment.json`; R42)

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Bash",
        "hooks": [
          {
            "type": "command",
            "command": "\"C:/Users/rchau/source/repos/CodeMem/src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe\" hook",
            "timeout": 600
          }
        ]
      }
    ]
  }
}
```

To be merged by hand into the user's `~/.claude/settings.json` (user scope: a build of GameRoom or MemOS happens in
that repository's session). Not installed by anything in this feature (FR-336). `timeout` 600 is the documented
default, stated so the largest registered solution (MemOS, 31 s) has room; a timeout is reported by Claude Code as
such. `PostToolUseFailure` is not subscribed: a red build never reaches the bridge (spike fact 2).

## 5. `extract.log` (FR-351)

Beside the executable — `AppContext.BaseDirectory`, whatever `--config` names (INC3); one line per `extract`
invocation, appended, tab-separated, no header:

```text
2026-09-15T21:04:12.4Z	green_build	repoPath=C:\Users\rchau\source\repos\CodeMem\	CodeMem	passed	0	solution=CodeMem run_id=8 observed=… sha=…
2026-09-15T21:05:01.0Z	tool	solutionKey=MemOS	-	extract.enabled	-	refused: extract.enabled is false in C:\…\bridge.config.json
2026-09-15T21:06:30.2Z	manual	stale	MemOS	passed	0	solution=MemOS run_id=7 observed=… sha=…
2026-09-15T21:07:02.9Z	manual	stale	GameRoom	passed	4	RESIDUAL MISMATCH solution=GameRoom … (exit 4, reported verbatim)
2026-09-15T21:20:00.0Z	tool	solutionKey=CodeMem	CodeMem	passed	timeout	extractor killed after 540 s
```

Columns: UTC time; origin; target as given; resolved key (or `-`); gate outcome (`passed` or the gate); child exit
code, `-`, or `timeout` (TIM1); the summary or refusal line verbatim with newlines removed. A `stale` run writes one
line per solution launched and no header line (INC3). A line that cannot be written sets `logged: false` and
`logError` in the result and changes nothing else.

## 6. The process document (`src/CodeMem.Bridge/README.md`; FR-339)

Sections, in order: what the bridge is (one paragraph; the two decisions by number); install (build `CodeMem.sln`
Release; copy the sample config beside the exe and set the two paths; approve the project server in Claude Code;
user-scope registration for other repositories; merge the fragment; flip the gates); **when each tool is called** —
`map_status` at session start and before relying on `references`, `orphans` or `type_usages`; `type_usages` before
any delete or rename; `extract` by the hook, never by Claude Code directly; `extract --stale` by a human from the
command line — the gates and who flips them; the extract log and where it is; the refusals a user will meet and
their remedies (a table generated from contracts/tools.md §6); what the bridge never does; and one recorded
non-choice (STOP 1 ruling 3): the `mcp_tool` hook type produced no observable effect in the 2026-09-15 spike
(spike.md §Fact 2), so the fragment uses the command hook and the `mcp_tool` type is not retried.

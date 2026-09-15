# Spike: CodeMem 004 — the two facts the bridge rests on

**Feature**: `004-codemem-bridge` | **Date**: 2026-09-15 | **Spec**: [spec.md](spec.md) §Spike |
**Ceremony**: STANDARD, one spike (description). Run before `/speckit-plan`; the plan's research cites it as R41–R42.

Every artefact lived in the session scratchpad (`…\scratchpad\spike\`) and is not part of the repository. The one
file written inside the repository, `.mcp.json`, was removed after each run; `git status` was clean (the spec
folder only) at the end. Nothing was written to either live database or to the user's `.claude` settings; the
non-interactive sessions left three transcripts under the user's Claude project directory, which is normal.

## Artefacts

- **SpikeServer** — a VB.NET net8.0 console (`Option Strict On`), package `ModelContextProtocol.Core` 1.4.0 (the
  package MemOS's own server uses; already in the NuGet cache), 150 lines. Three entries: `serve` (default) — a
  stdio MCP server named `spike` with one tool `ping(text)`; `once <text>` — a one-shot entry printing one line;
  `hook --log <path>` — reads the hook JSON from stdin, appends it verbatim to the log, decides whether it was a
  green `dotnet build`/`dotnet test`, calls the one-shot path in-process, and answers Claude Code with
  `hookSpecificOutput.additionalContext`.
- **stdio_probe.py** — drives the server by hand: `initialize`, `notifications/initialized`, `tools/list`,
  `tools/call`.
- **SampleCopy** / **SampleCopyRed** — copies of the committed fixture solution; the red copy has one file with an
  undeclared symbol (2 errors, exit 1 when built by hand).
- **hook-settings-command.json** — `PostToolUse` and `PostToolUseFailure`, matcher `Bash`, one `command` hook each
  (the server's `hook` entry), `timeout` 120.
- **hook-settings-mcptool.json** — `PostToolUse`, matcher `Bash`, one hook of type `mcp_tool` calling
  `spike.ping`.
- The Claude Code used: the binary bundled with the VS Code extension, **2.1.272**, run with `CLAUDECODE` and
  `CLAUDE_CODE_ENTRYPOINT` unset so it starts a fresh session.

## Fact 1 — Claude Code loads a stdio server from `.mcp.json` in `repos\CodeMem`: PROVEN

**1a. The server speaks MCP (hand-driven, 382 ms for four messages).**

```text
initialize -> {"result": {"protocolVersion": "2025-06-18", "capabilities": {"logging": {}, "tools": {"listChanged": true}}, "serverInfo": {"name": "spike", "version": "0.0.1"}}, "id": 1, "jsonrpc": "2.0"}
tools/list -> {"result": {"tools": [{"name": "ping", "description": "Spike tool: echoes text with the server's pid, cwd and time.", "inputSchema": {"type": "object", "properties": {"text": {"type": "string"}}, "required": ["text"]}, "annotations": {"readOnlyHint": true}}]}, ...}
tools/call -> {"result": {"content": [{"type": "text", "text": "pong:probe pid=30260 cwd=...\\scratchpad\\spike utc=2026-09-15T20:17:42.0334302Z"}]}, ...}
server exit: 0
```

The SDK derived the input schema from the VB delegate's parameter name and marked the tool read-only from
`McpServerToolCreateOptions.ReadOnly`.

**1b. `.mcp.json` at the repository root is read as project scope.** File used:

```json
{ "mcpServers": { "spike": { "type": "stdio",
    "command": "C:/Users/rchau/AppData/Local/Temp/claude/…/scratchpad/spike/SpikeServer/bin/Release/net8.0/SpikeServer.exe",
    "args": ["serve"] } } }
```

`claude mcp list` from the repository root:

```text
spike: C:/…/SpikeServer.exe serve - ⏸ Pending approval (run `claude` to approve)
```

`claude mcp get spike`: `Scope: Project config (shared via .mcp.json)`, `Type: stdio`. An interactive session
asks the user to approve the project server on first use (the documented behaviour; the status line names it).

**1c. A session in the repository calls the tool.** `claude -p "Call the MCP tool mcp__spike__ping with the
argument text set to hello-from-print …" --allowedTools mcp__spike__ping` (non-interactive; the documentation says
project servers load without the approval prompt here, and they did — `enabledMcpjsonServers` in the user config
stayed empty afterwards, so no approval was recorded either):

```text
tool_use: "name":"mcp__spike__ping","input":{"text":"hello-from-print"}
result:   pong:hello-from-print pid=50736 cwd=C:\Users\rchau\source\repos\CodeMem utc=2026-09-15T20:17:57.8790850Z
subtype success, 3 turns, 5 976 ms
```

Two facts a design needs: the tool is named `mcp__<server>__<tool>` to the model, and **Claude Code launches the
server with its working directory set to the repository root** (the server's `cwd` above).

**1d. A repository-relative `command` resolves against the repository root.** `.mcp.json` rewritten with
`"command": "../../../AppData/Local/Temp/claude/…/SpikeServer.exe"` (relative to `repos\CodeMem`), same prompt:

```text
subtype: success | result: pong:relative-path pid=29004 cwd=C:\Users\rchau\source\repos\CodeMem utc=2026-09-15T20:24:05.1854399Z
```

So the shipped `.mcp.json` names the bridge as `src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe`, portable
across checkouts; only the user-scope registration for other repositories and the hook fragment need the absolute
path (contracts/cli-config-hook.md §3–4).

## Fact 2 — a PostToolUse hook on Bash can observe a `dotnet build` exit code and call a tool: PROVEN, with two corrections to the assumed mechanism

**2a. Green build.** `claude -p --settings hook-settings-command.json --allowedTools "Bash(dotnet build:*)"`,
prompt: run `dotnet build "<SampleCopy>\Sample.sln" -c Debug --nologo -v q` and report the exit status and any
`SPIKE-HOOK` text given as additional context. The session answered:

```text
EXIT 0
SPIKE-HOOK event=PostToolUse tool=Bash cwd=C:\Users\rchau\source\repos\CodeMem command=dotnet build "C:/…/SampleCopy/Sample.sln" -c Debug --nologo -v q tool_response.kind=Object tr.stdout=Build succeeded. 0 Warning(s) 0 Error(s) Time Elapsed 00:… tr.stderr= tr.interrupted=False tr.isImage=False tr.noOutputExpected=False other_keys=session_id,transcript_path,prompt_id,permission_mode,effort,tool_use_id,duration_ms, decision=green once=once:green-build pid=50992 utc=2026-09-15T20:18:07.6280227Z elapsed_ms=17
```

The payload the hook received on stdin, verbatim (the log):

```json
{"session_id":"3ec81c80-…","transcript_path":"C:\\Users\\rchau\\.claude\\projects\\C--Users-rchau-source-repos-CodeMem\\3ec81c80-….jsonl","cwd":"C:\\Users\\rchau\\source\\repos\\CodeMem","prompt_id":"13f4b0a1-…","permission_mode":"default","effort":{"level":"xhigh"},"hook_event_name":"PostToolUse","tool_name":"Bash","tool_input":{"command":"dotnet build \"C:/…/SampleCopy/Sample.sln\" -c Debug --nologo -v q","timeout":600000,"description":"Build the spike Sample solution in Debug"},"tool_response":{"stdout":"Build succeeded.\r\n    0 Warning(s)\r\n    0 Error(s)\r\n\r\nTime Elapsed 00:00:00.76","stderr":"","interrupted":false,"isImage":false,"noOutputExpected":false},"tool_use_id":"toolu_017hAgZVGi7SKFkuj7FKZ4a7","duration_ms":1811}
```

**2b. Red build** (same settings, `SampleCopyRed`): `PostToolUse` did **not** fire; `PostToolUseFailure` fired,
with no `tool_response` and an `error` field whose first line is the exit code:

```json
{"session_id":"1ef4bb01-…", "cwd":"C:\\Users\\rchau\\source\\repos\\CodeMem", "hook_event_name":"PostToolUseFailure","tool_name":"Bash","tool_input":{"command":"dotnet build \"C:/…/SampleCopyRed/Sample.sln\" -c Debug --nologo -v q", …},"tool_use_id":"toolu_01XCxr…","error":"Exit code 1\nC:\\…\\Broken.vb(3,9): error BC30451: 'UndefinedSymbolForTheSpike' is not declared. …\r\n\r\nBuild FAILED. …","is_interrupt":false,"duration_ms":…}
```

The session reported `EXIT 1` and the hook's text for the failure event (`decision=no`, nothing called).

**2c. What this proves and corrects.**

| Assumed in the spec | Observed | Consequence for the design |
|---|---|---|
| The payload may carry an exit-code field | `tool_response` for Bash has `stdout`, `stderr`, `interrupted`, `isImage`, `noOutputExpected` — **no exit code** | "Exited 0" is known by the event: `PostToolUse` fires only for a successful command; a non-zero exit routes to `PostToolUseFailure`, whose `error` starts with `Exit code N`. The hook entry subscribes to `PostToolUse` only and still checks `interrupted` is false and that the event name is `PostToolUse` (belt and braces). |
| `cwd` is the build's directory | `cwd` is the **session** directory; the command named a solution elsewhere | Q6 as ruled: the path named in the command decides; `cwd` is the fallback. |
| `hookSpecificOutput.additionalContext` reaches the model | It did, verbatim, on both events | The hook entry's result line is what the session sees (SC-310). |
| A one-shot entry can be the hook command (Q5 (a)) | The exe was the hook command, read stdin, called its own one-shot path, answered in **17 ms** | Q5 (a) stands; the fragment's command is the bridge executable plus `hook`, no script. |
| A hook of type `mcp_tool` could call the tool directly (Q5 (b)) | Run with `hook-settings-mcptool.json`: the session answered `EXIT 0 / NONE`; no `pong` text and no hook event anywhere in the stream, no error on stderr | **No observable effect** on 2.1.272 in print mode; not investigated further because (a) is proven, simpler, and keeps the origin out of the public tool's arguments. (b) is dropped. |
| Timing | Build 1.8 s, hook 17 ms, whole session 9.7 s (green), 12.3 s (red) | The hook's cost is the extraction itself (MemOS 31 s on 2026-09-13); the fragment sets `timeout` 600, the documented default. |

## Decisions the spike settles (carried into research R41–R42 and the contracts)

1. Transport and registration as ruled: stdio, `.mcp.json` at the root, project scope, user approval on first
   interactive use.
2. The hook fragment subscribes to `PostToolUse` with matcher `Bash`; its command is the bridge executable's
   `hook` entry; the entry reads stdin, matches `dotnet build` / `dotnet test`, takes the path named in the
   command (else `cwd`), walks the extract door with origin green-build, and answers with `additionalContext`;
   exit code 0 always.
3. A red build never reaches the hook entry (different event); the entry additionally refuses any payload whose
   event is not `PostToolUse` or whose `interrupted` is true.
4. The `mcp_tool` hook type is not used.
5. `ModelContextProtocol.Core` 1.4.0 with `McpServer.Create` + `StdioServerTransport` + `McpServerTool.Create`
   works from VB.NET with `Option Strict On`; the hosting package is not needed. One VB rule met on the way:
   `Await` is not allowed in a `Finally` block — dispose after the run returns.

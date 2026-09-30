\# CodeMem — working in this repository



\## Closing a session that changed the bridge

Every Claude Code session runs `src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe`

from this repository, and a running bridge locks that folder. So the last build of a session

that changed the bridge is done like this, after the final commit:



1\. Stop every running bridge:

&#x20;  `Get-Process CodeMem.Bridge -ErrorAction SilentlyContinue | Stop-Process`

&#x20;  This cuts the `codemem` tools in every open session, this one included, until step 5.

2\. Build Release: `dotnet build CodeMem.sln -c Release --nologo -v q`. It must be green.

&#x20;  Confirm `bridge.config.json` beside the exe is unchanged, so the extract gates are unchanged.

3\. Bring the map current from the new build:

&#x20;  `src\\CodeMem.Bridge\\bin\\Release\\net8.0\\CodeMem.Bridge.exe extract --solution-key CodeMem`

4\. Check the new build before handing over: run it as `serve`, confirm `initialize` reports the

&#x20;  new version and the instructions, and `tools/list` the expected tools. Leave no bridge running.

5\. Ask the Architect to reconnect: `/mcp` → `codemem` → Reconnect, in every open session.

&#x20;  A session cannot reconnect its own MCP server.


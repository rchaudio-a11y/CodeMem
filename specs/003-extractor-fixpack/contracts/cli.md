# Contract: CodeMem.Extractor command line (fixpack 003)

**Feature**: `003-extractor-fixpack` | **Amends**: nothing in
[../../002-stage-a-fixpack/contracts/cli.md](../../002-stage-a-fixpack/contracts/cli.md); restated for the
one line whose meaning this feature relies on.

Invocation, arguments, defaults, the summary line, the exit codes, the environment variables and `--help`
are unchanged. No argument is added.

## The duplicate-id refusal (exit 1), unchanged and now proven through the executable

```text
duplicate doc-comment id <id> at <path>:<line>,<column> and <path>:<line>,<column>
```

One stderr line, nothing on stdout, nothing written (a fresh `--db` path holds no user table afterwards).
Paths are solution-relative with forward slashes; a location outside the solution directory carries `..`
segments. Under this feature the two locations are always **inside the scope root**: an out-of-scope
declaration is not a solution symbol and cannot collide.

## Stamp

`extract_runs.extractor_version` reads `0.2.0` on every run of this executable.

## Operator steps this feature adds (quickstart)

```powershell
dotnet src/CodeMem.Extractor/bin/Debug/net8.0/CodeMem.Extractor.dll --solution <repo>\GameRoom\GameRoom.sln --db C:\_DB\codemem.sqlite --solution-key GameRoom
dotnet src/CodeMem.Extractor/bin/Debug/net8.0/CodeMem.Extractor.dll --solution <repo>\CodeMem\CodeMem.sln   --db C:\_DB\codemem.sqlite --solution-key CodeMem
dotnet src/CodeMem.Extractor/bin/Debug/net8.0/CodeMem.Extractor.dll --solution <repo>\rchaudio-a11y\MemOS\MemOS.sln --db C:\_DB\codemem.sqlite --solution-key MemOS
```

Each prints one summary line on exit 0 or one refusal line on exit 1; both are recorded verbatim.

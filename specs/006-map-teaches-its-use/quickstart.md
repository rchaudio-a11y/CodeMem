# Quickstart: CodeMem 006 — The Map Teaches Its Own Use

**Date**: 2026-09-29 | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | Contracts:
[tools.md](contracts/tools.md), [usage-texts.md](contracts/usage-texts.md)

A run-and-record guide. Each section says what to run, what must come back, and where the figure is recorded.
Nothing here is implementation.

## Prerequisites

- .NET SDK 10.0.401 (the recorded runs' SDK); the repository at the feature's head.
- PowerShell 5.1 (Windows), for the verbatim check and the copy.
- The map at `C:\_DB\codemem.sqlite` for the acceptance on a copy only. The suite never opens it.
- Read access to the PM store, for §Verbatim's comparison against 191490 only.

## Build and test

```powershell
dotnet build CodeMem.sln -c Debug
dotnet test CodeMem.sln -c Debug --nologo
dotnet build CodeMem.sln -c Release
dotnet test CodeMem.sln -c Release --nologo
```

Expected: green on both, under 5 minutes each (SC-509), with the new facts of plan §Test design among the passed and
the live facts skipped (`CODEMEM_LIVE_MAP` unset). Record passed / failed / skipped and the duration for each build
against the plan's baseline. A Release build while a Release `CodeMem.Bridge.exe serve` is running fails to copy the
executable, which is the F10 caveat: stop that process first, or build Debug.

## Fixture validation (what the new facts show; each is a named fact)

| Story | Fact | Must show |
|---|---|---|
| US1 | B12 (1) | unfiltered on `RenameScenario`: three candidates in run 2 (`Describe`→`Explain`, and `Name`→`Label` once per twin), each twin side naming its project; run 2 checked (3 = 3); run 3 listed as failed, not checked |
| US1 | B12 (2) | `retiredSymbolId` = the retired `Describe` id: one candidate, runs `[2]`, `retiredInRunId` 2 |
| US1 | B12 (3) | `symbol_detail` on the retired id: `SymbolRetired` text contains "call rename_candidates with retiredSymbolId" and the id; the same id to `rename_candidates` answers |
| US1 | B12 (4) | one candidate row removed: `runId` 2 refused `CandidateCountMismatch` naming 3 and 2 |
| US1 | B12 (5) | `RunNotFound`, `RunOutOfScope` (both keys), `SymbolNotFound`, `SymbolOutOfScope`, each through the executable |
| US1 | B12 (6) | map SHA-256 equal before and after every call; `tools/list` annotates `rename_candidates` read-only |
| US1 | B12 (7), (8) | an active id answered, not refused; `Other` (no candidates) → total 0, the run checked 0 = 0, the note |
| US1 | B12 (9) | both filters: run 2 + the retired `Describe` → `[run2]`, one candidate, not checked; run 1 + the same → `[run1]`, total 0, the note |
| US2 | UsageTexts (1)–(3) | `initialize` carries the instructions, no CR; nine names; four phrases |
| US2 | B01 (5) | the literal nine names; version 0.3.0; instructions equal the constant |
| US3 | UsageTexts (4)–(7) | README table = registered tools; skill and snippet phrase; no "Rick"/"153204" under `docs/`; README says nine |
| US3 | B05 (18) | the fragment's command is `"C:/path/to/CodeMem.Bridge.exe" hook` |

## Verbatim (Q3): hash each shipped text back to the spec's table

Run from the repository root after the texts land, and again at the close. Every line must print `OK`.

```powershell
function Lf([string]$t) { $t = $t -replace "`r`n", "`n" -replace "`r", "`n"; if ($t.EndsWith("`n")) { $t.Substring(0, $t.Length - 1) } else { $t } }
function Sha([string]$t) { (([Security.Cryptography.SHA256]::Create().ComputeHash([Text.Encoding]::UTF8.GetBytes($t))) | ForEach-Object { $_.ToString('x2') }) -join '' }
function Check($name, $text, $want) { $got = Sha $text; if ($got -eq $want) { "OK   $name" } else { "DIFF $name $got" } }

$readme = @(Get-Content README.md -Encoding UTF8)
$i6 = 0; while (-not $readme[$i6].StartsWith('**6 · Teach')) { $i6++ }
$j6 = $i6; while ($readme[$j6] -ne 'installs itself.') { $j6++ }
$src = Get-Content src\CodeMem.Bridging\Mcp\BridgeServerInstructions.vb -Raw -Encoding UTF8
$literal = [regex]::Match($src, 'Text As String = "((?:[^"]|"")*)"').Groups[1].Value -replace '""', '"'
$row = $readme | Where-Object { $_.StartsWith('| **`rename_candidates`**') }

Check 'instructions' (Lf $literal)                                                                 'd8a173d3ca85e6449f474053cfa9de12a05cb52858ab4671782dcd7b99a010ca'
Check 'snippet'      (Lf (Get-Content docs\claude-code\CLAUDE.snippet.md -Raw -Encoding UTF8))        '0cb688263573f0387bec07ddc24fe612da882b505e171930444679728338b970'
Check 'skill'        (Lf (Get-Content docs\claude-code\skills\codemem\SKILL.md -Raw -Encoding UTF8))  '509557babd307faa9373844062e6565edcaec7199690dde9b8c3a66cbbf5dd26'
Check 'step 6'       ($readme[$i6..$j6] -join "`n")                                                'f73f70295130bb825fbe07712ce91cface5561a20b31563d1a137a5eb1d58f82'
Check 'step 5 line'  (@($readme | Where-Object { $_.StartsWith('Replace `C:/path/to/') }) -join "`n")  '1d6946b3842a5fb4281278cc84af0f929645e7e03613db9f28031ac250814bf6'
Check 'table row'    (($row -split '\|')[2].Trim())                                                '3ec2c4deae08e1ec67c5e57f35f31b351f61ff89aab7c95c42682c1c34a4e174'
```

The step 6 slice runs from the line that opens it to its closing line `installs itself.`. Step 5's own last words are
"…turns itself on.", so the slice cannot start early. Record the six lines. A `DIFF` stops the work for a ruling: the text is re-taken from the store, never edited to fit.

## Acceptance on a copy of the live map (Q13, FR-527; research R84)

1. With no `dotnet build` or `dotnet test` running in any mapped repository (the green-build hook would extract and
   hold the map), record `Get-FileHash C:\_DB\codemem.sqlite -Algorithm SHA256`.
2. `Copy-Item C:\_DB\codemem.sqlite $env:TEMP\codemem-006-acceptance.sqlite`.
3. In one command, so that a hook extraction after the command cannot fall between the two readings:

   ```powershell
   $before = (Get-FileHash C:\_DB\codemem.sqlite).Hash
   $env:CODEMEM_LIVE_MAP = "$env:TEMP\codemem-006-acceptance.sqlite"
   dotnet test CodeMem.sln -c Debug --nologo --filter "FullyQualifiedName~B08_LiveMapTests"
   $after = (Get-FileHash C:\_DB\codemem.sqlite).Hash
   "live map unchanged by the suite: $($before -eq $after)"
   Remove-Item Env:CODEMEM_LIVE_MAP
   ```

4. Record:
   - B08 (9): run 13, one candidate 5339 → 23219, `samePath` true, `offsetDistance` 284, rank 1, `countChecked`
     true.
   - B08 (10): 6754, no candidates, runs `[53]`, `retiredInRunId` 53.
   - B08 (11): MemOS unfiltered answered, every completed run checked, `total` = the sum of recorded counts.
   - Each of the three calls' measured duration, as recorded, not asserted (analyze G3).
   - B08's other facts: their existing figures.
   - The line `live map unchanged by the suite: True`.
5. Delete the copy.

## What must not happen

- Any file under `src/CodeMem.Extraction/`, `src/CodeMem.Extractor/` or `src/CodeMem.Core/Schema/SchemaRepository.vb`
  in the feature's diff (`git diff --stat main...HEAD -- <those paths>` prints nothing).
- The extractor's version other than 0.3.0 or the schema's other than 3.
- A change in the MemOS repository. Record its `git status --porcelain` before the first task and at the close; both
  are empty or identical.
- Anything written under `~/.claude` by this feature. The kit is copied by a person.
- §2's private block, or its heading, in any file of this repository (`git grep -n "153204" -- docs` prints nothing).
- A machine path in an install text: `git grep -n "C:/Users/" -- src/CodeMem.Bridge/README.md
  src/CodeMem.Bridge/hooks/settings.fragment.json README.md` prints nothing (STOP 1 decision 11; FR-522).
- `BridgeStandaloneGateTests` (4) in the suite. It is retired to `_Archive/006-map-teaches-its-use/` (STOP 1 decision
  1); the suite lists three facts for that class.

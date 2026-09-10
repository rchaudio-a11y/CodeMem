# Quickstart: CodeMem Stage A

Validation guide — how to prove the feature works end to end. Implementation detail belongs in
`tasks.md`; the schema is [contracts/schema.sql](contracts/schema.sql); the command line is
[contracts/cli.md](contracts/cli.md).

## Prerequisites

- Windows 11 with .NET SDK 9 or 10 installed and the .NET 8 runtime (`Microsoft.NETCore.App 8.0.x`
  and `Microsoft.WindowsDesktop.App 8.0.x`). Verified present on the development machine.
- No other tooling: no Python, Node, or external git executable at runtime (Article XV).

## Build

```powershell
dotnet build CodeMem.sln -c Debug
```

Expected: 0 errors. Warnings about missing XML comments will **not** appear — the VB compiler does not
emit them; the Review Gate does.

## Run the test suite (all invariants)

```powershell
dotnet test tests/CodeMem.Tests/CodeMem.Tests.vbproj
```

Expected on a finished implementation: I1–I15 pass, the tripwire and `CountAuditor` tests pass, and
`AcceptanceRunner` is reported **Skipped** (env var unset). The first run restores the fixture once
(`dotnet restore` inside the collection fixture), then loads it once; the whole collection should
finish inside the SC-010 budget plus restore time.

Observed 2026-09-09 (T114, SDK 10.0.303, runtime 8.0.31):

```text
Passed!  - Failed:     0, Passed:    37, Skipped:     1, Total:    38, Duration: 54 s - CodeMem.Tests.dll (net8.0)
  Skipped CodeMem.Tests.AcceptanceRunner.ReportOnTheNamedSolution
```

38 tests: I1–I15 (one file each; I8 and I10 carry two facts), the US1/US3/US4 fact tests, `CountAuditor` (4),
refusals (4), schema constraints (2), tripwire, SQL-location gate (2), header gate (2), project-file gate,
and the Skip-armed runner. A single fixture extraction into a fresh map takes about 2 s in-process.

During Article II Red phases, expect the invariant under construction to fail for the reason the test
names — not for a missing type or a vacuous assertion.

## Extract the fixture by hand

```powershell
dotnet run --project src/CodeMem.Extractor -- `
  --solution tests/CodeMem.Tests/Fixtures/Sample/Sample.sln `
  --db $env:TEMP\sample-map.sqlite
```

Expected: exit 0 and one stdout line. Observed 2026-09-09 (38 symbols: 2 projects, 1 merged namespace,
types and members of the fixture):

```text
solution=Sample run_id=1 observed=38 matched=0 reactivated=0 new=38 retired=0 registry_before=0 notes_orphaned=0 candidates=0 unaccounted_observed=0 unaccounted_registry=0 digest=<64 hex> sha=<40 hex>
```

Run it again unchanged: `run_id=2 observed=38 matched=38 reactivated=0 new=0 retired=0 registry_before=38`,
same `digest` (observed).

Inspect with any SQLite client — the file is plain SQLite, journal mode DELETE, no sidecar files:

```sql
SELECT verb, COUNT(*) FROM code_edges GROUP BY verb;
SELECT COUNT(*) FROM code_edges WHERE verb = 'handles';        -- equals Handles items + AddHandler statements in the fixture
SELECT map_guid FROM map_identity;                             -- unchanged across runs
SELECT * FROM extract_runs ORDER BY id;                        -- one row per run, residuals 0
```

## Prove the refusals

| Scenario | Command | Expected |
|----------|---------|----------|
| Compile error | copy the fixture, break a line, extract | exit 2, `error BC...` lines then `errors=<n>` on stderr, map byte-identical (I1) |
| Second extractor | hold a `BEGIN IMMEDIATE` on the map (or run one extractor with `CODEMEM_TEST_ABORT_AT` unset and a breakpoint), start another | exit 3 within 2 s including start-up (in-process refusal under 1 s), `lock held` on stderr, nothing written (I10) |
| Residual mismatch | run via the test seam (I8) | exit 4, `outcome=failed ...` summary on stderr, a `failed` row in `extract_runs`, no other change |
| Abort mid-publish | `$env:CODEMEM_TEST_ABORT_AT='DuringPublish'`, extract | `Process terminated. CODEMEM_TEST_ABORT_AT=DuringPublish` on stderr, non-zero exit; published tables equal the previous run; the next run continues normally (observed: `run_id=3 matched=38`) (I9) |

## Acceptance against a real solution (Skip-armed)

```powershell
$env:CODEMEM_ACCEPT_SOLUTION = 'C:\path\to\Real.sln'
dotnet test tests/CodeMem.Tests --filter AcceptanceRunner
```

Prints the summary line, `handles_written` vs `handles_in_source`, `partial_types`, both residuals,
and `elapsed_ms`. Reported, never gated. Unset the variable to see it Skipped again.

## Conventions to carry into implementation

- Every `.vb` file: header block (filename, project, description, author RCH Automation LLC, created
  date) and XML docs on every public member.
- Retired code goes to `_Archive/`; deprecated members get `<Obsolete>` (Article XIV).
- One class/module/interface/enum per file; all SQL inside `CodeMem.Core/Repositories/`.

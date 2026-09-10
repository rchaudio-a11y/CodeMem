# Contract: CodeMem.Extractor command line

**Feature**: `001-extractor-codemem-sqlite` | **Spec**: FR-001, FR-033, FR-034, FR-039

## Invocation

```text
CodeMem.Extractor --solution <path.sln|path.vbproj> --db <path to codemem.sqlite>
                  [--configuration Debug|Release]      default: Debug
                  [--framework <tfm>]                   default: the project's first target framework
                  [--solution-key <name>]               default: solution file name without extension
CodeMem.Extractor --help
```

Arguments are case-insensitive names with `--`; values follow as the next argument. Unknown or missing
required arguments → usage text on stderr, exit 1. `--help` prints usage on stdout and exits 0; the
usage text lists `CODEMEM_TEST_ABORT_AT` under a "test-only" heading so no unwired surface is
presented as real.

## Exit codes (FR-034)

| Code | Meaning | Writes |
|------|---------|--------|
| 0 | Completed and published | full publication |
| 1 | Any other failure: usage error, unreadable solution, workspace failure, schema-version mismatch, duplicate doc-comment id, database error | nothing |
| 2 | Compilation has one or more `Error` diagnostics | nothing (diagnostics on stderr) |
| 3 | Lock held by another extractor, or lock unobtainable (read-only medium) | nothing |
| 4 | A residual is non-zero | one `extract_runs` row with `outcome = 'failed'`; nothing else |

No failure exits 0.

## Summary line (FR-033) — stdout, exactly one line on exit 0

Space-separated `key=value` pairs, fixed order, no spaces inside values:

```text
solution=<key> run_id=<n> observed=<n> matched=<n> reactivated=<n> new=<n> retired=<n> registry_before=<n> notes_orphaned=<n> candidates=<n> unaccounted_observed=<n> unaccounted_registry=<n> digest=<64 hex> sha=<40 hex|null>
```

On exit 4 the same line is printed to stderr with `outcome=failed` prepended. Nothing else is printed
on stdout on any exit.

## Diagnostics (exit 2) — stderr

One line per `Error` diagnostic in the compiler's own format (`path(line,col): error BCxxxx: message`),
then a final line `errors=<n>`.

## Environment variables

| Variable | Read by | Effect |
|----------|---------|--------|
| `CODEMEM_ACCEPT_SOLUTION` | `CodeMem.Tests` acceptance runner | When set, the runner extracts that solution into a temp map and prints: the summary line; `handles_written=<n> handles_in_source=<n>` (the latter from an independent syntax walk of `HandlesClauseItem` + `AddHandlerStatement` nodes); `partial_types=<n>`; both residuals; `elapsed_ms=<n>`. When unset the runner is reported as **Skipped**. Reported, never gated. |
| `CODEMEM_TEST_ABORT_AT` | `CodeMem.Extraction.ExtractionRun` | `AfterStaging` or `DuringPublish`: the process calls `Environment.FailFast` at that point (I9). Any other value → ignored. Test-only. |

## Preconditions the extractor assumes (documented, not detected)

- The solution has been restored (`obj/project.assets.json` present) so the design-time build can
  resolve references; an unrestored solution surfaces as compile errors → exit 2.
- The `.NET 8` runtime is installed; an SDK ≥ 8 is installed for the build host.

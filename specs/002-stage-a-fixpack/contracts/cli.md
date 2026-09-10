# Contract: CodeMem.Extractor command line (fixpack 002 amendments)

**Feature**: `002-stage-a-fixpack` | **Amends**: [../../001-extractor-codemem-sqlite/contracts/cli.md](../../001-extractor-codemem-sqlite/contracts/cli.md)

Invocation, arguments, the summary line and the exit-2 diagnostics format are unchanged. No argument is
added.

## Exit codes (FR-034 as amended by FR-105, FR-112, FR-115)

| Code | Meaning | Writes |
|------|---------|--------|
| 0 | Completed and published; a version-1 map was upgraded in place first if needed | full publication |
| 1 | Any other failure: usage error, invalid or unusable `--db` path (illegal characters, whitespace-only, a directory, a path that cannot be opened), **a SQLite file that is not a map**, schema version other than 1 or 2, workspace failure, duplicate doc-comment id, **SDK version unresolvable**, database error. **Exactly one stderr line**, nothing on stdout | nothing (a fresh-map creation or an upgrade in progress is rolled back) |
| 2 | Compilation has one or more `Error` diagnostics | nothing (diagnostics on stderr, then `errors=<n>`) |
| 3 | Lock held by another extractor, or the medium is read-only at `BEGIN IMMEDIATE` (SQLite 8). **SQLite 14 (cannot open) is exit 1, not 3** | nothing |
| 4 | A residual is non-zero | one `extract_runs` row with `outcome = 'failed'` (carrying `sdk_version`); nothing else |

No failure exits 0. Every exit-1 message is one line; multi-line causes are folded with ` | `.

## Fresh, upgrade and refusal at `--db`

| File at `--db` | Behaviour |
|----------------|-----------|
| absent, 0 bytes, or SQLite with no user tables | created as a version-2 map inside the run's transaction |
| version-1 map | upgraded to version 2 inside the run's transaction, then extracted |
| version-2 map | extracted |
| SQLite with tables but no `map_identity` (or an empty one) | exit 1: `not a map: <path>`; file byte-identical |
| version > 2 | exit 1: schema version mismatch; nothing written |
| not SQLite | exit 1: one line with SQLite's message |

## Environment variables

| Variable | Read by | Effect |
|----------|---------|--------|
| `CODEMEM_ACCEPT_SOLUTION` | `CodeMem.Tests` acceptance runner | unchanged |
| `CODEMEM_TEST_ABORT_AT` | `CodeMem.Extraction.ExtractionRun` | `<phase>:<nonce>` where phase is `DuringInitialize`, `DuringUpgrade`, `AfterStaging` or `DuringPublish`. **Honoured only when `CODEMEM_TEST_NONCE` is also set and equals `<nonce>` exactly.** Then the process calls `Environment.FailFast` at that point. Otherwise inert. Test-only. |
| `CODEMEM_TEST_NONCE` | `CodeMem.Extraction.ExtractionRun` | The per-run nonce a test mints (a GUID). Alone it does nothing. Test-only. |

`--help` prints both variables under its "test-only environment variables" heading with the
both-required rule. The nonce is never stored in the map.

## Preconditions (unchanged, plus one)

- The solution has been restored; an SDK ≥ 8 is installed for the build host.
- **An SDK resolvable for the solution directory** (honouring `global.json`): the run stamps its
  version and refuses with exit 1 when none resolves, exactly as `dotnet --version` would fail there.

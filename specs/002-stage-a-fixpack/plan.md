# Implementation Plan: CodeMem Fixpack 002 — Stage A Review Corrections

**Branch**: `002-stage-a-fixpack` | **Date**: 2026-09-10 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/002-stage-a-fixpack/spec.md`

**Governing document**: `.specify/memory/constitution.md` v1.2.1, unamended. Parent plan:
[../001-extractor-codemem-sqlite/plan.md](../001-extractor-codemem-sqlite/plan.md); this plan changes
Stage A in place and lists only what changes.

## Summary

Eight review corrections and one additive schema change. Reconciliation refreshes `kind` on a match;
the map opens through a typed connection string; fresh-map creation, the 1→2 upgrade and publication all
happen inside the one `BEGIN IMMEDIATE` transaction, with a foreign SQLite file refused; every preflight
failure maps to exit 1 with one stderr line; the abort seam needs a matching per-run nonce; two transitive
packages are pinned to audited versions; the compiled-inputs enumeration gains the four well-known build
files walked upward and every run stamps the resolved SDK version (read in-process from the host's SDK
resolver, [research.md](research.md) R21); the two source-policy gates match case-insensitively with
whitespace normalised. Schema version becomes 2 with `sdk_version` nullable and a schema-owned trigger,
one shape on fresh and upgraded maps (R22).

## Technical Context

**Language/Version**: VB.NET on .NET 8 (`net8.0`), unchanged. Option Strict/Explicit On, Infer Off.

**Primary Dependencies** (changes only):

| Package | Version | Project | Why |
|---------|---------|---------|-----|
| Microsoft.Build.Tasks.Core | **17.8.43** (pin) | Extraction | clears GHSA-h4j7-5rxr-p4wc and GHSA-w3q9-fxm7-j8fq (R25) |
| System.Formats.Asn1 | **8.0.1** (pin) | Extraction | clears GHSA-447r-wph3-92pm (R25) |
| hostfxr (the .NET host, already in-process) | 10.0.12 here | Extraction | `hostfxr_resolve_sdk2` / `hostfxr_set_error_writer` via P/Invoke (R21); no package |

All Stage A pins (Roslyn 4.14.0, Microsoft.Data.Sqlite 8.0.31, LibGit2Sharp 0.32.0, xUnit 2.9.3) unchanged.

**Storage**: one SQLite file; schema version **2**; DDL in [contracts/schema.sql](contracts/schema.sql)
= version-1 DDL + migration 1→2, applied on fresh and v1 maps alike.

**Testing**: xUnit, real SQLite, real compiled fixture, no mocks (Article III). New tests under
`tests/CodeMem.Tests/Fixpack/`, one file per review item; each Red-first with the reason recorded
(FR-123). A committed version-1 map fixture produced by the Stage A extractor (R30).

**Target Platform**: Windows 11 developer workstation; extractor cross-platform .NET 8 (the hostfxr
module name differs per OS; the lookup is by module name `hostfxr` prefix).

**Project Type**: CLI over class libraries, unchanged. No new project.

**Performance Goals**: SC-010 of Stage A still holds (fixture extraction under 60 s; observed ~2 s).
The SDK resolution adds one in-process call (< 50 ms observed).

**Constraints**: Article XV (no process launch for the SDK version); Article IX (a foreign database is
refused, never repurposed); Article XIV (the upgrade adds, never drops or rebuilds); no new CLI argument.

**Scale/Scope**: unchanged.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Article / Gate | Status | How this plan satisfies it |
|----------------|--------|----------------------------|
| I. Library-First | PASS | No new project. New types: `SchemaState` (enum), `NotAMapException`, `SdkResolutionException`, `SdkVersion` (module), `SdkResolverNative` (module holding the P/Invoke delegates); one per file. `Program.vb` unchanged except nothing. |
| II. Test-First | PASS | Every behaviour change is a Red-first test in `Fixpack/`; the two re-fired gates and every new guard record a FIRE line. The trigger's fire is a raw insert (spec Q2 rider). |
| III. Integration-First | PASS | Real SQLite, real fixture; the v1 map fixture is a real Stage A artefact (R30). |
| IV. Compiler Fact Only | PASS | `kind` refreshed from the compiler; `sdk_version` from the host resolver; build files are a path rule, not a heuristic. |
| V. Green Only, Stamped | PASS | Stamp gains `sdk_version`; the run row is still the first fact-table write; creation and upgrade are identity/schema setup under the same lock, before the stamp. |
| VI. Reconcile, Never Truncate | PASS | Identity unchanged; `kind` joins the refreshed observation columns. No DELETE/DROP reaches `code_symbols`; the upgrade drops and rebuilds nothing (I13 unchanged). |
| VII. Evidence on Every Row | PASS | Unchanged. |
| VIII. Counts That Reconcile | PASS | Unchanged; a kind change is a match (SC-101). |
| IX. One File, Many Solutions, One Writer | PASS | Typed connection string (F5); a SQLite file that is not a map is refused, not repurposed (FR-105); creation now happens under the writer lock (R23). |
| X. Absence Must Be Representable | PASS | `sdk_version` nullable; NULL means "written before the column existed"; no sentinel. |
| XI. Anti-Abstraction | PASS | Direct SQL in repositories; the P/Invoke module is the driver's own host API, no wrapper package. |
| XII. One Door for Every Rule | PASS | The v2 obligation is a trigger in the schema, identical on both paths (Q2); fresh creation is "v1 then migrate", so there is one migration door. Detection of fresh/v1/current/foreign is one method (`InspectSchema`). Build-file selection lives in `CompiledInputs` beside the obj/ rule. |
| XIII. Production-Route Reachability | PASS | Every refusal is proven through `ExtractionRun.Execute` and the executable; the trigger through a raw insert (its own door). |
| XIV. Archive, Never Delete | PASS | Upgrade is ADD COLUMN + CREATE TRIGGER + UPDATE of one value. `Program.vb`'s Stage A stub history is git's. |
| XV. Compiled .NET, No Foreign Runtime | PASS | hostfxr is the .NET host itself; no process launched (R21). |
| Gate: Option settings in every touched project file | PASS | Project files gain package pins only; the gate test still scans them. |
| Gate: no SQL outside a named repository method | PASS | `InspectSchema`, `UpgradeToVersion2`, `SetSchemaVersion` are repository/schema methods; test SQL stays in `MapQueries`. |
| Gate: header block + XML docs | PASS | Existing gate covers new files. |
| Gate: three call sites for a new abstraction | PASS | None introduced. |

**Post-design re-check**: unchanged, no violations, no Complexity Tracking entries.

## Project Structure

### Documentation (this feature)

```text
specs/002-stage-a-fixpack/
├── plan.md              # This file
├── research.md          # R21–R30, verified on this machine
├── data-model.md        # schema v2, migration, run order changes, refreshed columns
├── quickstart.md        # validation: upgrade a v1 copy, audit output, refusals, nonce
├── contracts/
│   ├── schema.sql       # v1 DDL + migration 1->2 (one shape)
│   └── cli.md           # exit-code mapping corrections, test-only variables with nonce
└── tasks.md             # /speckit-tasks output (not created here)
```

### Source Code (repository root) — files that change or appear

```text
src/
├── CodeMem.Core/
│   ├── Schema/
│   │   ├── SchemaVersion.vb              # Current = 2
│   │   ├── SchemaRepository.vb           # CreateVersion1, UpgradeToVersion2, SetSchemaVersion, ReadSchemaVersion; DDL text per contracts/schema.sql
│   │   └── SchemaState.vb                # NEW enum: Fresh, Version1, Current, Foreign, Newer
│   └── Repositories/
│       ├── MapDatabase.vb                # Open(path) via SqliteConnectionStringBuilder; InspectSchema; no schema work in Open
│       ├── NotAMapException.vb           # NEW: a SQLite file that is not a map (FR-105)
│       ├── CodeSymbolsRepository.vb      # RefreshMatched/Reactivate set kind
│       ├── ExtractRunsRepository.vb      # binds sdk_version
│       └── (MapIdentityRepository.vb)    # Insert takes a MapDatabase (creation now runs under the lock)
├── CodeMem.Extraction/
│   ├── Provenance/
│   │   ├── SdkVersion.vb                 # NEW: Resolve(workingDirectory) via hostfxr (R21)
│   │   ├── SdkResolverNative.vb          # NEW: delegates + host lookup for hostfxr_resolve_sdk2 / set_error_writer
│   │   └── SdkResolutionException.vb     # NEW: no SDK / no host -> exit 1
│   ├── Workspace/
│   │   └── CompiledInputs.vb             # + BuildFiles(basePath): the four names walked upward (R28)
│   └── Run/
│       ├── RunPhase.vb                   # + DuringInitialize, DuringUpgrade
│       ├── ExtractionRun.vb              # preflight inside the boundary; open -> lock -> InspectSchema -> create/upgrade -> ...; nonce; sdk stamp
│       └── (RunSeams.vb unchanged)
├── CodeMem.Extractor/
│   └── CommandLine.vb                    # --help: both test-only variables and the both-required rule
tests/CodeMem.Tests/
├── Fixtures/Maps/sample-v1.sqlite        # NEW binary: produced by the Stage A extractor before any code change (R30)
├── Support/
│   ├── FixtureCopy.vb                    # copies to fixture-<guid>/Sample; exposes ParentDirectory
│   ├── TempMap.vb                        # optional name suffix (a ';' path)
│   ├── MapQueries.vb                     # + ReadSchemaObjects, ReadRuns gains SdkVersion, CopyV1Fixture helper lives in Support (no SQL)
│   └── RunRow.vb                         # + SdkVersion (nullable)
├── Fixpack/                              # NEW folder, one file per review item
│   ├── F02_KindRefreshTests.vb
│   ├── F05_ConnectionStringTests.vb
│   ├── F06_FreshMapTests.vb
│   ├── F09_PreflightTests.vb
│   ├── F08_NonceTests.vb
│   ├── F04_BuildInputsTests.vb
│   └── S02_SchemaUpgradeTests.vb         # v1 fixture upgrades; sqlite_master equality; trigger fire; abort during upgrade
├── Invariants/I09_AtomicPublishTests.vb  # passes the nonce
├── Guards/TripwireTests.vb               # case-insensitive, whitespace-normalised; FIRE re-recorded
├── Guards/SqlLocationGateTests.vb        # same
└── Guards/SchemaConstraintTests.vb       # + trigger fire on a fresh v2 map
```

**Structure Decision**: unchanged four projects. New behaviour lands in the files that own the concern
(Core for schema and repositories, Extraction for provenance and the run, Extractor for help text).

## Run Order (revised; changes in bold)

Executed by `ExtractionRun.Execute(options, seams)`:

1. **Inside the top-level error boundary from the first statement** (FR-115): normalise `--solution`
   and `--db`, derive the key, check the solution file exists and the db directory exists.
2. `MapDatabase.Open(dbPath)`: typed connection string (F5), `PRAGMA foreign_keys = ON`,
   `PRAGMA journal_mode = DELETE`. **No schema work here.** SQLite 14 → exit 1; 8 → exit 3.
3. `BeginImmediate()` (unchanged: native handle, busy timeout 0). Busy or read-only → exit 3.
4. **`InspectSchema()`** → `Fresh`: `CreateVersion1`, identity row at version 1, seam
   `DuringInitialize`, `UpgradeToVersion2`, seam `DuringUpgrade`, `SetSchemaVersion(2)`.
   `Version1`: `UpgradeToVersion2`, seam `DuringUpgrade`, `SetSchemaVersion(2)`. `Current`: nothing.
   `Foreign` → `NotAMapException` → exit 1, rollback. `Newer` → `SchemaVersionMismatchException` → exit 1.
5. `EnsureByKey` (identity setup, unchanged).
6. Load and compile; exit 2 on errors (unchanged).
7. Enumerate compiled inputs (**+ build files**, R28) → digest, git provenance; **`SdkVersion.Resolve(basePath)`**
   → stamp; failure → `SdkResolutionException` → exit 1.
8. Stage symbols and edges (unchanged); `MutateStaged` seam.
9. Reconcile; `CorruptStagedCounts` seam; **abort seam requires phase + matching nonce** (R26).
10. Audit; exit 4 path unchanged (the failed row also carries `sdk_version`).
11. Publish: run row (**with `sdk_version`**) → inserts → **refreshes now set `kind`** → reactivations
    (**set `kind`**) → retirements → parts → seam `DuringPublish` → edges → candidates → labels.
12. Commit; summary line unchanged.

Every exception on steps 1–11 is caught by one boundary: `MapLockHeldException` → 3;
`DuplicateDocCommentIdException`, `WorkspaceLoadException`, `SchemaVersionMismatchException`,
`NotAMapException`, `SdkResolutionException`, `SqliteException`, `ArgumentException`, `IOException`,
`UnauthorizedAccessException`, any other → 1. Exit-1 messages are folded to one line (newlines → ` | `).

## Kind refresh (F2) — the reconciliation change

`Reconciler` is untouched: identity is still `doc_comment_id`, and (B) still requires equal kind. The
publication step passes `symbol.Kind` to `RefreshMatched` and `Reactivate`, which set `kind = @kind`.
`RegistryRow.Kind` read before publication is the old kind; (B)'s comparison uses the retired row's old
kind against the new symbol's kind, as before.

## Abort seam nonce (F8) — Carve-Out Register (supersedes the Stage A rows)

| Item | Kind | Armed by | Behaviour when unarmed |
|------|------|----------|------------------------|
| `AcceptanceRunner` | Skip-armed runner (FR-039) | `CODEMEM_ACCEPT_SOLUTION=<path>` | Reported as **Skipped** |
| `CODEMEM_TEST_ABORT_AT` + `CODEMEM_TEST_NONCE` | Test-only process-abort seam (I9, F6, upgrade) | **both** variables set, `ABORT_AT` = `<phase>:<nonce>`, `NONCE` = `<nonce>`, nonces equal | Inert: either alone, a phase without a nonce, or unequal nonces → the run completes normally; both labelled test-only in `--help` |
| `RunSeams.CorruptStagedCounts` / `RunSeams.MutateStaged` | Test-only in-process seams | passing a `RunSeams` to `Execute` | `Main` passes Nothing |

## Complexity Tracking

No constitution violations; nothing to justify. The one Stage A justification that changes shape:
`RunSeams` is untouched; the abort seam's arming rule moved from "variable present" to "variable plus
nonce", which removes a production exposure rather than adding surface.

## Known limits recorded (out of scope, from the review)

F1 project-scoped identity; F7 per-project target framework; F4 full evaluated build manifest; F11
profiling. Listed in the spec's Out of Scope; nothing here touches them.

## Phase 0 / Phase 1 outputs

- [research.md](research.md) — R21–R30; the two spec research items (in-process SDK read; one shape) verified.
- [data-model.md](data-model.md) — schema v2, migration, refreshed columns, detection states.
- [contracts/schema.sql](contracts/schema.sql), [contracts/cli.md](contracts/cli.md).
- [quickstart.md](quickstart.md).

## Implementation record (2026-09-10)

**Environment**: Windows 11; `dotnet --version` 10.0.401 (SDKs 9.0.308, 10.0.401); runtime 8.0.31 runs the
net8.0 projects; hostfxr 10.0.12 loaded in every .NET process, including the xUnit host. Starting point
commit `63d62fa` (Stage A code unchanged since `a0924fe`), 38 tests green, 0 warnings.

**Order followed**: tasks.md T001–T049 in order; the v1 map fixture was produced by the Stage A extractor
before any source change (T001); every behaviour change was Red-first with the reason recorded in the test
file header; every guard carries its FIRE line. Totals: see quickstart "Build and test".

**Review pass (T048), v1.2.1 Review Gates, every touched file**:

- Option settings: no project file lost a setting; `ProjectFileGateTests` green. `CodeMem.Extraction.vbproj`
  gained the two pins only; `CodeMem.Tests.vbproj` gained the fixture copy item.
- SQL location: all new SQL is in `SchemaRepository` (Schema), `MapDatabase`, `MapIdentityRepository`,
  `ExtractRunsRepository`, `CodeSymbolsRepository` (Repositories) and `MapQueries` (test support); the gate
  is now case-insensitive and whitespace-normalised and was re-fired (T044).
- FIRE lines: every file under `tests/CodeMem.Tests/Fixpack/` and every changed guard under `Guards/` and
  `Invariants/` carries a dated `' FIRE:` line with the injection and the observed Red.
- One door: the `sdk_version` obligation is owned by the two triggers alone; `ExtractRunsRepository` binds
  NULL when the stamp has none and pre-checks nothing. Fresh/v1/current/foreign/newer is decided in one
  method, `MapDatabase.InspectSchema`. Build-file selection lives in `CompiledInputs.BuildFiles` beside the
  obj/ rule. The nonce rule lives in `ExtractionRun.ReadAbortPhase` only.
- Article V fields: `sdk_version` is written on completed and failed rows and tested on both (F04).
- Tripwire: still counts positive writes first (now case-insensitively).
- No new abstraction; no new project; no new CLI argument; no constitution amendment.

**Deviations from the design documents, with reasons**:

1. **The version-1 CREATE text is frozen by existing maps.** SQLite stores each CREATE statement's text,
   inline comments included, in `sqlite_master.sql`. The two comments contracts/schema.sql had reworded
   inside `CREATE TABLE extract_runs` (`source_digest`) and `CREATE TABLE code_symbols` (`kind`) would have
   given a fresh map a different `sql` text from every Stage A map, breaking one shape (S02 fact 2).
   `SchemaRepository.CreateVersion1` therefore executes Stage A's text byte for byte, and the contract's
   remarks moved to comments between statements, where SQLite stores nothing. The 001 schema contract
   carries the FR-108 rewording the same way.
2. **`V1MapFixture.CopyToTemp(map As TempMap)`** takes the `TempMap` the caller owns instead of minting one,
   so disposal stays with the test (T011 wrote `CopyToTemp() As String`).
3. **`<None Update=…>` rather than `<None Include=…>`** for the fixture copy item: the SDK's default globbing
   already includes the file as a None item, and a second Include raises NETSDK1022 (duplicate items).
4. **T027's FIRE as specified could not go Red.** Moving `AbortIf(DuringUpgrade)` after `SetSchemaVersion(2)`
   changes nothing observable: both statements are inside the one `BEGIN IMMEDIATE` the `FailFast` discards,
   so the map reads 1 either way. That is FR-113 holding, not a gap. The recorded substitute fire commits the
   upgrade on its own before the abort, which the test catches (map reads 2). Recorded in S02's header.
5. **"Nothing written" after a refused or aborted first run is now "the file has no user table"**
   (research R23): creation rolls back with the run, so I10 (b), `SchemaConstraintTests`' propagation fact
   and F08 (d) assert `CountUserTables = 0` instead of an empty `extract_runs`.
6. **`ReadRuns` selecting `sdk_version`** was deferred from T009 to T016/T017 so that T012's "38 green"
   checkpoint was real; "no such column: sdk_version" is the Red both F04 facts recorded.
7. **`DotnetCli.Output`** (test support) was added for the F04 comparison with `dotnet --version`; no task
   named it.
8. **`MapQueries.Open`** also builds its connection string with `SqliteConnectionStringBuilder`; without it
   F05 could not read the `;` map it had just proven the extractor wrote.

**Known limit found at implementation**: when no SDK resolves for the solution directory, hostfxr itself
prints the installed-SDK list to stdout (its `trace::println`, not the error writer) before returning
0x8000809B; the extractor's own exit-1 line still goes to stderr and the captured host text is folded into
it, but stdout is not empty on that one path. Not observed with any resolvable SDK; not forced.

**Not done here**: commits (left to the Architect per T048).

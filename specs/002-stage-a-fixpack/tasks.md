---

description: "Task list for CodeMem Fixpack 002 — Stage A review corrections"
---

# Tasks: CodeMem Fixpack 002 — Stage A Review Corrections

**Input**: Design documents from `/specs/002-stage-a-fixpack/`

**Prerequisites**: plan.md, spec.md, research.md (R21–R30), data-model.md, contracts/schema.sql,
contracts/cli.md, quickstart.md — all present. Constitution v1.2.1 governs, unamended. Stage A's code and
its 38 tests are the starting point (commit `3ee22c6`).

**Tests**: REQUIRED. FR-123 and Article II make every behaviour change a Red-first test against real
SQLite and the real compiled fixture: write → run → confirm Red *for the stated reason* → record it in the
test file header → implement → confirm Green → record. Every guard carries a `' FIRE:` line (inject the
defect → Red → revert → Green). Tests for this feature live in `tests/CodeMem.Tests/Fixpack/`, one file
per review item, `<Collection("Fixture")>` unless noted.

**Organization**: Phase 1 setup (the v1 map artefact must be produced *before* any source change), Phase 2
foundational (support types, plus the abort-seam nonce because every later abort test arms the seam
through it), then one phase per user story in priority order, then polish.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1–US5 from spec.md
- Every path is repository-relative

## Standing rules for every task that creates or edits a `.vb` file

- Header block first (`' File:`, `' Project:`, `' Description:`, `' Author: RCH Automation LLC`, `' Created: 2026-09-10`); XML docs on every public declaration; one type per file; SQL only in `src/CodeMem.Core/Repositories/*.vb`, `src/CodeMem.Core/Schema/*.vb` and `tests/CodeMem.Tests/Support/MapQueries.vb` (plus the SQL-scanning guards and `SchemaConstraintTests`, by the gate's own exclusion list).
- Red/Green/FIRE lines go in the test file header; the FIRE injection is reverted before the task closes.
- Stage A tests that this feature changes are edited in place (never copied); their headers gain a dated line saying what changed and why.

---

## Phase 1: Setup (Artefacts that must exist before the code moves)

**Purpose**: Capture the version-1 map with the Stage A extractor, and pin the two packages, before any
source file changes.

- [X] T001 With the working tree at Stage A (no source change yet), produce the version-1 map fixture: `dotnet run --project src/CodeMem.Extractor -- --solution tests/CodeMem.Tests/Fixtures/Sample/Sample.sln --db tests/CodeMem.Tests/Fixtures/Maps/sample-v1.sqlite` **twice** (so the registry has matched rows and two completed runs), confirm no `-journal` file remains, and add `<None Include="Fixtures/Maps/sample-v1.sqlite" />` with `CopyToOutputDirectory=PreserveNewest` to `tests/CodeMem.Tests/CodeMem.Tests.vbproj` so tests copy it from the output directory (research R30). Record `map_identity.schema_version = 1` and the row counts per table in a comment block in `tests/CodeMem.Tests/Fixtures/Maps/README.md`
- [X] T002 Add to `src/CodeMem.Extraction/CodeMem.Extraction.vbproj` the two pins `<PackageReference Include="Microsoft.Build.Tasks.Core" Version="17.8.43" />` and `<PackageReference Include="System.Formats.Asn1" Version="8.0.1" />` with a comment naming GHSA-h4j7-5rxr-p4wc, GHSA-w3q9-fxm7-j8fq and GHSA-447r-wph3-92pm (research R25); run `dotnet list CodeMem.sln package --vulnerable --include-transitive` and paste its output into `specs/002-stage-a-fixpack/quickstart.md` under "Dependency audit"; run the full suite → 38 tests still green (FR-118, FR-120)
- [X] T003 Confirm `dotnet build CodeMem.sln` is 0 errors / 0 warnings after T002 and that `tests/CodeMem.Tests/bin/Debug/net8.0/BuildHost-netcore/` still contains only `Microsoft.Build.Locator.dll` among MSBuild assemblies; record that observation as the FR-119 finding paragraph in `specs/002-stage-a-fixpack/quickstart.md` (already drafted there; replace "to be pasted" with the observed listing)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Non-behavioural support changes every story needs, plus the abort-seam nonce (behavioural,
Red-first) because the abort tests of US1, US2 and the updated I9 all arm the seam through it.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Support and record types (non-behavioural)

- [X] T004 [P] Create `src/CodeMem.Core/Schema/SchemaState.vb`: `Enum SchemaState` with `Fresh`, `Version1`, `Current`, `Foreign`, `Newer` (data-model.md "Map states at open"); XML doc on each member stating its detection rule
- [X] T005 [P] Create `src/CodeMem.Core/Repositories/NotAMapException.vb`: `Inherits Exception`, ctor `(path As String)`, message exactly `not a map: <path>` (contracts/cli.md)
- [X] T006 [P] Create `src/CodeMem.Extraction/Provenance/SdkResolutionException.vb`: `Inherits Exception`, ctor `(message As String)`; used when no host or no SDK resolves (research R21)
- [X] T007 [P] Extend `src/CodeMem.Extraction/Run/RunPhase.vb` with `DuringInitialize` (after the identity row, before the migration on a fresh map) and `DuringUpgrade` (after the migration statements, before `schema_version` is set), documented per data-model.md
- [X] T008 [P] Extend `src/CodeMem.Core/Records/RunStamp.vb` with `SdkVersion As String` (XML doc: "resolved .NET SDK version, never Nothing on a version-2 run")
- [X] T009 [P] Extend `tests/CodeMem.Tests/Support/RunRow.vb` with `SdkVersion As String` (nullable: Nothing when NULL) and `tests/CodeMem.Tests/Support/MapQueries.vb`: `ReadRuns` selects `sdk_version` (NULL → Nothing); add `ReadSchemaObjects(db) As List(Of String)` = `SELECT type, name, tbl_name, sql FROM sqlite_master ORDER BY type, name` joined as `type|name|tbl_name|sql` with NULL sql as `<null>` (research R22); add `CountUserTables(db)` = `SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'`; add `CreateForeignDatabase(db)` = `CREATE TABLE t (x)` on a fresh file (the only place a foreign table is made)
- [X] T010 [P] Extend `tests/CodeMem.Tests/Support/TempMap.vb`: optional constructor `New(fileNameSuffix As String)` so a test can mint `map-<guid>;semi.sqlite`; and `tests/CodeMem.Tests/Support/FixtureCopy.vb`: copy into `<temp>/codemem-tests/fixture-<guid>/Sample/`, expose `ParentDirectory` (`fixture-<guid>`), dispose deletes `fixture-<guid>` (research R28); existing users of `Directory`/`SolutionPath` unchanged
- [X] T011 [P] Create `tests/CodeMem.Tests/Support/V1MapFixture.vb`: `CopyToTemp() As String` copies `Fixtures/Maps/sample-v1.sqlite` (from `AppContext.BaseDirectory`) to a fresh `TempMap` path and returns it; no SQL
- [X] T012 Build `CodeMem.sln` → 0 errors; run the full suite → 38 green (nothing behavioural changed yet)

### Abort-seam nonce (F8, FR-116) — Red-first here because every later abort test depends on it

- [X] T013 Create `tests/CodeMem.Tests/Fixpack/F08_NonceTests.vb`: through `ExtractorProcess` against the committed fixture into a fresh `TempMap`: (a) `CODEMEM_TEST_ABORT_AT=DuringPublish:abc` with no nonce → exit 0 and one completed run; (b) `CODEMEM_TEST_NONCE=abc` alone → exit 0; (c) `CODEMEM_TEST_ABORT_AT=DuringPublish:abc` + `CODEMEM_TEST_NONCE=xyz` → exit 0; (d) both set with a fresh GUID nonce → non-zero exit, stderr contains `Process terminated. CODEMEM_TEST_ABORT_AT=DuringPublish`, no run row. Run → Red on (a) (today the bare variable aborts; the `:abc` suffix is unknown so instead assert (d) — today `DuringPublish:<guid>` is not a known phase and the run completes → (d) Red) → record which assertion carried the Red
- [X] T014 Implement the rule in `src/CodeMem.Extraction/Run/ExtractionRun.vb`: `ReadAbortPhase()` parses `CODEMEM_TEST_ABORT_AT` as `<phase>:<nonce>`, requires `CODEMEM_TEST_NONCE` non-empty and ordinal-equal to `<nonce>` and `<phase>` a known `RunPhase`; otherwise `RunPhase.None` (research R26); update `src/CodeMem.Extractor/CommandLine.vb` usage text: both variables under the test-only heading with the both-required rule (FR-117); update `tests/CodeMem.Tests/Invariants/I09_AtomicPublishTests.vb` to mint a GUID and pass both variables (header line explaining the change); run F08 + I09 → Green; FIRE: accept the phase when the nonce is absent → F08 (a) Red → revert → Green; record
- [X] T015 Update the Carve-Out Register in `specs/001-extractor-codemem-sqlite/plan.md` to point at the superseding table in `specs/002-stage-a-fixpack/plan.md` (one line; the 002 table is already written) and update `specs/001-extractor-codemem-sqlite/contracts/cli.md`'s environment-variable rows to reference `specs/002-stage-a-fixpack/contracts/cli.md` (FR-117)

**Checkpoint**: support types compile, the seam needs a nonce, 39 tests green.

---

## Phase 3: User Story 1 — Kind refresh on an upgraded map (Priority: P1) 🎯 MVP

**Goal**: An (A) match refreshes `kind`; a version-1 map is upgraded in place to version 2 inside the
write lock, with `sdk_version` stamped on every new run and guarded by a trigger identical on fresh and
upgraded maps.

**Independent Test**: `dotnet test --filter "F02|S02"` — kind change through three kinds keeps the id;
the committed v1 map upgrades, extracts, reads version 2, and its `sqlite_master` equals a fresh map's.

### Slice A — schema version 2, one shape, SDK stamp (the schema change the story needs)

- [X] T016 [US1] Create `tests/CodeMem.Tests/Fixpack/S02_SchemaUpgradeTests.vb` with the first facts: (1) `V1MapFixture.CopyToTemp()` → `Execute` → `ExitCode.Success`, `MapQueries.ReadSchemaVersion = 2`, every table's row count = before + that run's own rows (`extract_runs` +1, `code_symbols` unchanged since the fixture is unchanged, `solutions` unchanged), pre-existing run rows read `SdkVersion` Nothing, the new run row's `SdkVersion` non-empty, `map_guid` unchanged; (2) `ReadSchemaObjects` of the upgraded map equals `ReadSchemaObjects` of a fresh `TempMap` after one run; (3) a map with `SetSchemaVersion(3)` → `Failure`, no run row. Run → Red: (1) fails with `Failure` (Stage A refuses version 1 ≠ current 1? no — today Current = 1 so the v1 map opens and runs; the assertion `= 2` fails and `SdkVersion` is absent) → record the exact Red reason
- [X] T017 [US1] Create `tests/CodeMem.Tests/Fixpack/F04_SdkVersionTests.vb` (sdk half only; build files come in US3): after one run into a fresh map, `ReadRuns(0).SdkVersion` is non-empty and equals the trimmed output of `dotnet --version` run in the fixture directory (via `DotnetCli` — dev-time SDK use, Article XV); a run into the exit-4 seam path (`CorruptStagedCounts`) also writes a non-null `SdkVersion` on the failed row. Run → Red (column absent) → record
- [X] T018 [P] [US1] Set `Public Const Current As Integer = 2` in `src/CodeMem.Core/Schema/SchemaVersion.vb`
- [X] T019 [P] [US1] Rewrite `src/CodeMem.Core/Schema/SchemaRepository.vb` per `contracts/schema.sql`: `CreateVersion1(db As MapDatabase)` executes the VERSION 1 section (tables and indexes only — no PRAGMAs); `UpgradeToVersion2(db)` executes the MIGRATION section minus its final UPDATE: `ALTER TABLE extract_runs ADD COLUMN sdk_version TEXT NULL`, `CREATE TRIGGER tr_extract_runs_sdk_version_insert …`, `CREATE TRIGGER tr_extract_runs_sdk_version_update …` with the exact text of contracts/schema.sql (both paths must run identical statements — research R22); `SetSchemaVersion(db, version)` = `UPDATE map_identity SET schema_version = @version WHERE id = 1`; `ReadSchemaVersion(db)`; `CountUserTables(db)`; `HasMapIdentityRow(db)`
- [X] T020 [P] [US1] Create `src/CodeMem.Extraction/Provenance/SdkResolverNative.vb`: `Module` holding the three `UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet:=CharSet.Unicode)` delegates (`ResultFn(key As Integer, value As IntPtr)`, `ErrorWriterFn(message As IntPtr)`, `ResolveSdk2(exeDir, workingDir, flags, result) As Integer`, `SetErrorWriter(writer) As IntPtr`), `LocateHostfxr() As String` (the `hostfxr` module of the current process; else `DOTNET_ROOT`; else the `dotnet` on PATH → highest `host/fxr/*/hostfxr.*`), and `DotnetRootOf(hostfxrPath)` (three directories up) — research R21
- [X] T021 [US1] Create `src/CodeMem.Extraction/Provenance/SdkVersion.vb`: `Resolve(workingDirectory As String) As String` — load hostfxr via `NativeLibrary`, install an error writer capturing every line, call `hostfxr_resolve_sdk2(root, workingDirectory, 0, callback)`, take result key 0 (resolved SDK directory) → its leaf name; rc ≠ 0 or no key 0 → `SdkResolutionException` whose message is the captured host text folded to one line; never launches a process; XML doc cites R21
- [X] T022 [P] [US1] Extend `src/CodeMem.Core/Repositories/ExtractRunsRepository.vb`: bind `@sdk_version` (NULL when `stamp.SdkVersion Is Nothing`) in the one INSERT; the trigger, not code, refuses a v2 row without it (Article XII)
- [X] T023 [US1] Restructure `src/CodeMem.Core/Repositories/MapDatabase.vb`: `Open(path) As MapDatabase` builds the connection string with `SqliteConnectionStringBuilder With {.DataSource = path, .Pooling = False}` (F5, research R24), opens, runs `PRAGMA foreign_keys = ON` and `PRAGMA journal_mode = DELETE`, does **no** schema work; `InspectSchema(ByRef version As Integer) As SchemaState` per data-model.md (user tables = 0 → `Fresh`; no `map_identity` table or no row → `Foreign`; version 1 → `Version1`; 2 → `Current`; else `Newer`); keep `BeginImmediate`/`Commit`/`Rollback`/`CreateCommand`; remove `OpenOrCreate`; `src/CodeMem.Core/Repositories/MapIdentityRepository.vb`: `Insert(db As MapDatabase, guid, schemaVersion, createdUtc)`
- [X] T024 [US1] Rewrite step 1–4 of `src/CodeMem.Extraction/Run/ExtractionRun.vb` per plan.md "Run Order (revised)": everything from path normalisation onward inside one `Try`; `Open` → `BeginImmediate` → `InspectSchema` → `Fresh`: `CreateVersion1`, `MapIdentityRepository.Insert(db, guid, 1, now)`, `AbortIf(DuringInitialize)`, `UpgradeToVersion2`, `AbortIf(DuringUpgrade)`, `SetSchemaVersion(2)`; `Version1`: `UpgradeToVersion2`, `AbortIf(DuringUpgrade)`, `SetSchemaVersion(2)`; `Current`: nothing; `Foreign`: throw `NotAMapException`; `Newer`: throw `SchemaVersionMismatchException(version, Current)`; stamp `.SdkVersion = SdkVersion.Resolve(basePath)` after the compile gate and before the run row; exception mapping: `MapLockHeldException` → 3, everything else → 1 with **one** stderr line (newlines folded to ` | `), `SqliteException` 14 → 1 (not 3); exit 2 and 4 paths unchanged
- [X] T025 [US1] Update Stage A tests that the schema change touches, each with a dated header line: `tests/CodeMem.Tests/Invariants/US1_FirstRunTests.vb` asserts `schema_version = 2` on `map_identity` and the run; `tests/CodeMem.Tests/Guards/SchemaConstraintTests.vb` raw inserts carry `schema_version` 1 (still legal) and gain the trigger fire: a raw `INSERT INTO extract_runs (… schema_version = 2, sdk_version NULL …)` throws `SqliteException` whose message contains `sdk_version is required for schema_version >= 2`, on a fresh map **and** on an upgraded `V1MapFixture` copy (spec Q2 rider; SC-106); `tests/CodeMem.Tests/Invariants/I10_LockTests.vb` opens the holder with `MapDatabase.Open` + `BeginImmediate` (the file may be empty; the second process must still exit 3)
- [X] T026 [US1] Run `dotnet test tests/CodeMem.Tests/CodeMem.Tests.vbproj --filter "S02|F04_Sdk|SchemaConstraint|US1_FirstRun|I10"` → Green; then the full suite → Green; FIRE for S02 (2): drop the `sdk_version_update` trigger from `UpgradeToVersion2` only (fresh path keeps it) → `ReadSchemaObjects` differ → Red → revert → Green; FIRE for F04: return `RuntimeInformation.FrameworkDescription` from `SdkVersion.Resolve` → Red (`dotnet --version` mismatch) → revert → Green; record both
- [X] T027 [US1] Add to `tests/CodeMem.Tests/Fixpack/S02_SchemaUpgradeTests.vb` the abort fact: on a `V1MapFixture` copy, `ExtractorProcess` with `CODEMEM_TEST_ABORT_AT=DuringUpgrade:<guid>` + `CODEMEM_TEST_NONCE=<guid>` → non-zero exit; then `ReadSchemaVersion = 1`, `ReadSchemaObjects` equal to the pre-run snapshot, row counts unchanged; then a normal run → `Success` and version 2 (FR-113, SC-104). Run → expected Green from T024; FIRE: move `AbortIf(DuringUpgrade)` to after `SetSchemaVersion(2)` → the map reads 2 after the abort → Red → revert → Green; record

### Slice B — kind refresh (F2)

- [X] T028 [US1] Create `tests/CodeMem.Tests/Fixpack/F02_KindRefreshTests.vb`: on a `FixtureCopy`, run 1; record the row for `T:Sample.Fields` (`kind = class`, its `id`) and for `F:Sample.Fields.a`; `Replace("Sample.Lib/Fields.vb", "Public Class Fields", "Public Structure Fields")` and `Replace(…, "End Class", "End Structure")`; run 2: same `id`, `kind = structure`, `symbols_matched` = `symbols_observed`, `symbols_new = 0`, `symbols_retired = 0`, `rename_candidates = 0`; then `"Public Structure Fields"` → `"Public Module Fields"` and `"End Structure"` → `"End Module"`; run 3: same `id`, `kind = module`, same counts; `F:Sample.Fields.a` keeps its id through all three runs (research R27; SC-101). Run → Red (kind stays `class`) → record
- [X] T029 [US1] Extend `src/CodeMem.Core/Repositories/CodeSymbolsRepository.vb`: `RefreshMatched` and `Reactivate` gain a `kind As SymbolKind` parameter and `kind = @kind` in their UPDATE (FR-101, FR-103); pass `symbol.Kind` at both call sites in `src/CodeMem.Extraction/Run/ExtractionRun.vb`; run F02 → Green; FIRE: bind `@kind` to the row's old kind → Red → revert → Green; record
- [X] T030 [US1] Run the full suite `dotnet test tests/CodeMem.Tests/CodeMem.Tests.vbproj` → Green (`Reconciler` untouched; I5, I6, I7, I15 still hold)

**Checkpoint**: the MVP — a v1 map upgrades on its next run and a kind change keeps its id.

---

## Phase 4: User Story 2 — Safe start-up on any path (Priority: P2)

**Goal**: A `;` path, a 0-byte file, an interrupted first initialization and an invalid path each end in
exit 0 with a correct map at the named path or exit 1 with one stderr line; a foreign SQLite file is
refused untouched.

**Independent Test**: `dotnet test --filter "F05|F06|F09"`.

- [X] T031 [P] [US2] Create `tests/CodeMem.Tests/Fixpack/F05_ConnectionStringTests.vb`: `New TempMap(";semi")` → `Execute` → `Success`; the file exists at exactly `map.Path`; no file exists at the text before the `;`; `ReadRuns` has one completed row. Run → Red (Stage A concatenates the connection string; the `;` splits it) → record; expected Green after T023 — if already Green when first run, FIRE: replace the builder with concatenation → Red → revert → Green; record
- [X] T032 [P] [US2] Create `tests/CodeMem.Tests/Fixpack/F06_FreshMapTests.vb`: (a) `File.WriteAllBytes(map.Path, {})` → `Execute` → `Success`, `CountUserTables > 0`, version 2; (b) `MapQueries.CreateForeignDatabase(map.Path)` → `FileBytesHash` before → `ExtractorProcess` run → exit 1, stderr is exactly one line containing `not a map: ` and the path, stdout empty, hash unchanged (FR-105); (c) fresh path → `ExtractorProcess` with `CODEMEM_TEST_ABORT_AT=DuringInitialize:<guid>` + nonce → non-zero exit; then `CountUserTables(map.Path) = 0` or the file is absent/empty; then a normal run → `Success` (FR-106, SC-104). Run → Red on (a) (Stage A: file exists → not fresh → `no such table` → exit 1) → record
- [X] T033 [P] [US2] Create `tests/CodeMem.Tests/Fixpack/F09_PreflightTests.vb` (`ExtractorProcess`): (a) `--db " "` → exit 1, exactly one stderr line, empty stdout; (b) `--db "<temp>\ma<p>.sqlite"` → exit 1 (not 3), one stderr line; (c) `--db <an existing directory>` → exit 1, one line; (d) `--solution " "` → exit 1, one line; the line contains no `   at ` stack frame (FR-115, SC-103). Run → Red: (a) today is an unhandled `ArgumentException` (exit code is the CLR's, not 1); (b) today exits 3 → record
- [X] T034 [US2] Make F05, F06 and F09 Green through `src/CodeMem.Extraction/Run/ExtractionRun.vb` and `src/CodeMem.Core/Repositories/MapDatabase.vb` (T023/T024 carry the design; this task closes gaps the tests reveal: the directory-as-db case, the whitespace path, the one-line fold); FIRE for F09: move `Path.GetFullPath` back outside the `Try` → (a) Red → revert → Green; FIRE for F06 (b): treat `Foreign` as `Fresh` → the foreign file gains tables → Red (hash changed) → revert → Green; record all
- [X] T035 [US2] Update `tests/CodeMem.Tests/Guards/RefusalTests.vb` header: the schema-mismatch fact now uses version 3 via `SetSchemaVersion` (99 also works; 3 documents "greater than 2"); run US2 filter + full suite → Green

**Checkpoint**: every start-up case in SC-103 ends inside the exit-code contract.

---

## Phase 5: User Story 3 — Build files in the digest (Priority: P3)

**Goal**: The four well-known build files walked upward are compiled inputs; the digest claim is
reworded; the SDK stamp (delivered in US1) is documented as provenance.

**Independent Test**: `dotnet test --filter "F04"` — an empty `Directory.Build.props` above the copy
changes the digest and not the fact set.

- [X] T036 [US3] Create `tests/CodeMem.Tests/Fixpack/F04_BuildInputsTests.vb`: on a `FixtureCopy`, run 1 → record `SourceDigest` and `FactSet`; write `<Project />` to `Path.Combine(copy.ParentDirectory, "Directory.Build.props")`; run 2 → `SourceDigest` differs, `FactSet` equal except the counts line (all matched now) — compare the `S|`, `P|`, `E|` lines only; add `Directory.Build.targets`, `Directory.Packages.props` (`<Project />`) and `global.json` (`{}`) to `copy.ParentDirectory` and a second `Directory.Build.props` inside `copy.Directory`; run 3 → digest differs again, and `is_dirty` stays null (the copy is outside any repository) (FR-107, FR-110, SC-105). Run → Red (digest unchanged: the files are not inputs) → record
- [X] T037 [US3] Extend `src/CodeMem.Extraction/Workspace/CompiledInputs.vb`: `BuildFiles(basePath) As List(Of String)` walks `New DirectoryInfo(basePath)` through `.Parent` until Nothing, adding each existing `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`, `global.json`; `Enumerate` adds them (deduplicated, normalised, sorted like every other input); an unreadable build file throws (exit 1, spec edge case); XML doc cites R28 and states the selection rule (FR-108); run F04 → Green; FIRE: skip the walk's parent step (solution directory only) → run 2 Red → revert → Green; record
- [X] T038 [P] [US3] Reword Stage A's FR-005 in `specs/001-extractor-codemem-sqlite/spec.md` to "a digest over the selected compiled inputs" with a one-line pointer to FR-107/FR-108 of this feature, and the `source_digest` comment in `specs/001-extractor-codemem-sqlite/contracts/schema.sql` likewise (FR-108, US3 scenario 4)
- [X] T039 [US3] Run the full suite `dotnet test tests/CodeMem.Tests/CodeMem.Tests.vbproj` → Green (I2 determinism still holds: same files, same digest on two fresh maps)

---

## Phase 6: User Story 4 — The shipped executable and its dependencies (Priority: P4)

**Goal**: The nonce rule (delivered in Phase 2) is documented in `--help`; the pins (Phase 1) are audited
and recorded.

**Independent Test**: `dotnet test --filter "F08|Refusal"` plus the audit command.

- [X] T040 [US4] Add to `tests/CodeMem.Tests/Fixpack/F08_NonceTests.vb` the help fact: `ExtractorProcess.Run("--help")` → exit 0, stdout contains `CODEMEM_TEST_ABORT_AT`, `CODEMEM_TEST_NONCE` and the phrase `both` under a line containing `test-only` (FR-117). Run → expected Green from T014; FIRE: remove the nonce line from `CommandLine.Usage` → Red → revert → Green; record
- [X] T041 [US4] Re-run `dotnet list CodeMem.sln package --vulnerable --include-transitive` on the finished tree and confirm the quickstart's recorded output is still exact; confirm the FR-119 finding paragraph names the SDK's MSBuild the build host loads (`10.0.401` at planning time; record what is installed now)

---

## Phase 7: User Story 5 — Gates that cannot be slipped past by spelling (Priority: P5)

**Goal**: Tripwire and SQL-location gate match case-insensitively with whitespace normalised; both fire
demonstrations re-run and re-recorded.

**Independent Test**: `dotnet test --filter "Tripwire|SqlLocation"` plus the two FIREs.

- [X] T042 [P] [US5] Update `tests/CodeMem.Tests/Guards/TripwireTests.vb`: normalise every literal with `Regex.Replace(text, "\s+", " ")` and match every pattern (the positive `INSERT INTO` / `UPDATE ` count first, then each destructive form) with `RegexOptions.IgnoreCase` (FR-121; research R29); header gains the dated change line; run → Green
- [X] T043 [P] [US5] Update `tests/CodeMem.Tests/Guards/SqlLocationGateTests.vb` the same way (keyword regex `IgnoreCase` over whitespace-normalised literals; `New SqliteConnection` count unchanged); run → Green
- [X] T044 [US5] FIRE both (FR-122, SC-109): inject a method containing `"delete   from code_symbols where id = @id"` (lowercase, three spaces) into `src/CodeMem.Core/Repositories/CodeSymbolsRepository.vb` → Tripwire Red → revert → Green; inject `"select 1"` into `src/CodeMem.Extraction/Run/ExtractionRun.vb` → SqlLocation Red → revert → Green; re-record both `' FIRE:` lines with the lowercase injections

---

## Phase 8: Polish & Cross-Cutting

- [X] T045 [P] Update `specs/002-stage-a-fixpack/quickstart.md`: paste the full-suite totals (expected 38 + this feature's new facts), the audit output (T002/T041), and the by-hand results of every row in "Prove the corrections by hand"
- [X] T046 Validate the real map: `Copy-Item C:\_DB\codemem.sqlite $env:TEMP\codemem-v1-copy.sqlite`, extract the solution it was built from into the copy, and record in `specs/002-stage-a-fixpack/quickstart.md`: exit code, `schema_version` before/after, `sdk_version` of the new row, and that the two triggers exist — if the originating solution is not available on this machine, record that and validate against `Fixtures/Maps/sample-v1.sqlite` by hand instead; never run against the original first
- [X] T047 Execute every remaining command in `specs/002-stage-a-fixpack/quickstart.md` by hand and correct any line that does not match observed output (as Stage A's T115)
- [X] T048 Review pass against the v1.2.1 Review Gates for every file touched: Option settings (gate), SQL location (gate, now case-insensitive), FIRE line present in every new or changed guard (`grep -r "' FIRE:" tests/CodeMem.Tests/Fixpack tests/CodeMem.Tests/Guards tests/CodeMem.Tests/Invariants`), no rule at two doors (the trigger is the only owner of the `sdk_version` obligation; `ExtractRunsRepository` does not pre-check it), every Article V field including `sdk_version` written and tested, tripwire still counts positive writes first, no new abstraction; record the pass in `specs/002-stage-a-fixpack/plan.md` under a dated "Implementation record" heading; commit left to the Architect
- [X] T049 Append this feature's deviations (if any arose) and the observed environment (SDK version installed at implementation time) to the "Implementation record" in `specs/002-stage-a-fixpack/plan.md`; mark the four out-of-scope review items (F1, F7, F4 full, F11) as known limits in `specs/001-extractor-codemem-sqlite/Reports/report.md` with a one-line pointer to this feature

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** → **Foundational (Phase 2)** → **US1 (Phase 3)**: strictly sequential. T001 must run before any `.vb` file changes (it captures the Stage A extractor's output); T002 before any test run so the suite baseline includes the pins.
- **US2 (Phase 4)** depends on US1 Slice A (the restructured `MapDatabase` and the error boundary).
- **US3 (Phase 5)** depends on US1 Slice A (the `sdk_version` column exists) and on Phase 2 (`FixtureCopy.ParentDirectory`).
- **US4 (Phase 6)** depends on Phase 1 (pins) and Phase 2 (nonce); its tasks are documentation and a help-text fact.
- **US5 (Phase 7)** is independent of every story; it only needs Phase 2 complete.
- **Polish (Phase 8)** last.

### User Story Dependencies

- **US1**: after Phase 2. Slice A (T016–T027) before Slice B (T028–T030): the kind test runs on a version-2 map.
- **US2**: after US1 Slice A. T031–T033 are independent of each other; T034 closes them together.
- **US3**: after US1 Slice A. T036 → T037; T038 is documentation and can run any time after T016.
- **US4**: T040 after T014; T041 after T002.
- **US5**: T042 and T043 parallel; T044 after both.

### Within Each Story — the Article II loop

Every test task: write → run → Red for the stated reason → record → then the paired implementation task
→ Green → record. Every guard additionally carries its FIRE line. Where a fact is expected Green on first
run (it lands with an earlier slice), the FIRE is what makes the guard trusted (Article II).

### Parallel Opportunities

- Phase 2: T004–T011 all parallel; T013/T014 sequential after T012.
- US1 Slice A: T018–T020, T022 parallel once T016/T017 are Red; T021 after T020; T023 → T024 → T025 sequential.
- US2: T031, T032, T033 parallel; T034 after all three.
- US5: T042, T043 parallel.
- Phase 8: T045 parallel with T046/T047.

---

## Parallel Example: User Story 2

```text
# After Phase 3 Slice A is Green, write the three start-up tests together:
Task: "Create tests/CodeMem.Tests/Fixpack/F05_ConnectionStringTests.vb"   (T031)
Task: "Create tests/CodeMem.Tests/Fixpack/F06_FreshMapTests.vb"           (T032)
Task: "Create tests/CodeMem.Tests/Fixpack/F09_PreflightTests.vb"          (T033)
# Run all three -> record each Red -> T034 closes them in ExtractionRun.vb / MapDatabase.vb
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 (capture the v1 map with Stage A's extractor; pin the packages).
2. Phase 2 (support types; the nonce, Red-first).
3. Phase 3 Slice A (schema 2, one shape, SDK stamp, error boundary) then Slice B (kind refresh).
4. **STOP and VALIDATE**: `dotnet test --filter "F02|S02|F04_Sdk"`; upgrade a copy of a real v1 map by hand.

### Incremental Delivery

1. US1 → a v1 map upgrades on its next run; a kind change keeps its id.
2. US2 → the executable never leaves its exit-code contract at start-up.
3. US3 → the digest covers the build files the extractor chose.
4. US4 → help text and audit recorded.
5. US5 → the gates are spelling-proof.
6. Phase 8 → the quickstart is true; the review pass is recorded.

### Notes

- 49 tasks: Setup 3 · Foundational 12 · US1 15 · US2 5 · US3 4 · US4 2 · US5 3 · Polish 5.
- No new abstraction; no new project; no new CLI argument; no constitution amendment.
- `DELETE` still appears in exactly two production methods (parts and edges replacement); the migration adds only.
- Commit after each Green (one slice or one review item per commit); the commit message names the item (F2, F5, …) and whether a FIRE was recorded.

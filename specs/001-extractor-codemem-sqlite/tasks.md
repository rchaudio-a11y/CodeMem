---

description: "Task list for CodeMem Stage A — Extractor and codemem.sqlite"
---

# Tasks: CodeMem Stage A — Extractor and codemem.sqlite

**Input**: Design documents from `/specs/001-extractor-codemem-sqlite/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/schema.sql, contracts/cli.md,
quickstart.md — all present. Constitution v1.2.0 governs.

**Tests**: REQUIRED. Article II (Test-First) and FR-037 make every invariant I1–I15 a Red-first test, and
every guard must be shown to fire. The loop for each test task is: write → run → confirm Red *for the
stated reason* → **report Red to the Architect** → implement → confirm Green. A test that fails only
because a type does not compile is not Red; the story slices below create the public surface first so
Red means the assertion failed.

**Organization**: Phase 1 setup, Phase 2 foundational (non-behavioural only), then one phase per user
story in priority order, then guards and polish.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1–US5 from spec.md
- Every path is repository-relative

## Standing rules for every task that creates or edits a `.vb` file

- Header block first: filename, project, description, author `RCH Automation LLC`, created date.
- XML documentation on every public class, method, property, event and interface member.
- One class, module, interface or enum per file.
- SQL only inside `src/CodeMem.Core/Repositories/*.vb` (production) or `tests/CodeMem.Tests/Support/MapQueries.vb` (tests); parameters named and bound.
- Option Strict On / Option Explicit On / Option Infer Off — set in the project file, never per file.
- Guard tasks record their fire demonstration as a comment beside the test (`' FIRE: <date> injected <defect> → red; reverted → green`).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution and four projects that build empty, with the constitution's project-file settings
and the pinned packages from plan.md.

- [ ] T001 Create `CodeMem.sln`, directories `src/` and `tests/`, and append `bin/`, `obj/`, `*.user`, `TestResults/` to `.gitignore`
- [ ] T002 [P] Create `src/CodeMem.Core/CodeMem.Core.vbproj`: `net8.0` class library, `RootNamespace` `CodeMem.Core`, `OptionStrict` On, `OptionExplicit` On, `OptionInfer` Off, `GenerateDocumentationFile` true, `Version` 0.1.0, PackageReference `Microsoft.Data.Sqlite` 8.0.31
- [ ] T003 [P] Create `src/CodeMem.Extraction/CodeMem.Extraction.vbproj`: same settings, `Version` 0.1.0 (the stamped `extractor_version` is read from this assembly — research R12), ProjectReference Core, PackageReferences `Microsoft.CodeAnalysis.VisualBasic.Workspaces` 4.14.0, `Microsoft.CodeAnalysis.Workspaces.MSBuild` 4.14.0, `LibGit2Sharp` 0.32.0 (no `Microsoft.Build.Locator` — research R1)
- [ ] T004 [P] Create `src/CodeMem.Extractor/CodeMem.Extractor.vbproj`: `Exe`, `net8.0`, same settings, `Version` 0.1.0, ProjectReference Extraction
- [ ] T005 [P] Create `tests/CodeMem.Tests/CodeMem.Tests.vbproj`: same settings, ProjectReferences Core, Extraction, Extractor; PackageReferences `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.5, `Microsoft.NET.Test.Sdk` 17.14.1, `Xunit.SkippableFact` 1.5.85; `<Compile Remove="Fixtures/**" />` so fixture sources never compile into the test assembly
- [ ] T006 Add the four projects to `CodeMem.sln` (Core, Extraction, Extractor, Tests) and confirm `dotnet build CodeMem.sln` reports 0 errors with all projects empty
- [ ] T007 Create `tests/CodeMem.Tests/Guards/ProjectFileGateTests.vb`: test that every `.vbproj` under `src/` and `tests/` (excluding `Fixtures/`) contains `<OptionStrict>On`, `<OptionExplicit>On`, `<OptionInfer>Off`, `<GenerateDocumentationFile>true`; run → Green; FIRE: remove `OptionInfer` from one project file → Red → revert → Green; record beside the test

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Non-behavioural prerequisites every story needs — record types, enums, the committed fixture
solution, and the test harness. No repository, walker, rule or orchestration logic is written here;
those are driven Red-first inside the stories (Article II).

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

### Core record types (`src/CodeMem.Core/Records/`)

- [ ] T008 [P] Create `src/CodeMem.Core/Records/SourceLocation.vb`: `Structure` with `Path As String` (solution-relative, forward slashes), `StartOffset As Integer` (0-based), `Length As Integer`, `StartLine As Integer` (1-based), `StartColumn As Integer` (1-based)
- [ ] T009 [P] Create `src/CodeMem.Core/Records/SymbolKind.vb`: `Enum` with exactly the schema's kinds: `Namespace_`, `Class_`, `Module_`, `Structure_`, `Interface_`, `Enum_`, `EnumMember`, `Delegate_`, `Method`, `Constructor`, `Property_`, `Field`, `Event_`, `Project`; and a **separate file** `src/CodeMem.Core/Records/SymbolKindNames.vb` with `Module SymbolKindNames` mapping each to its schema text (`'enum_member'`, `'project'`, …) — one type per file (Article I)
- [ ] T010 [P] Create `src/CodeMem.Core/Records/EdgeVerb.vb`: `Enum` `PartOf`, `Calls`, `Uses`, `Implements_`, `Extends`, `Imports_`, `DependsOn`, `Handles_`; and a **separate file** `src/CodeMem.Core/Records/EdgeVerbNames.vb` with `Module EdgeVerbNames` mapping to `'part_of'` … `'handles'` (Article I)
- [ ] T011 [P] Create `src/CodeMem.Core/Records/ObservedPart.vb`: `Location As SourceLocation`, `PartHash As String`
- [ ] T012 [P] Create `src/CodeMem.Core/Records/ObservedSymbol.vb`: `DocCommentId`, `Kind`, `Name`, `ContainerDocCommentId` (nullable), `ProjectDocCommentId` (nullable — null for namespace and project), `Primary As SourceLocation`, `BodyHash`, `Parts As List(Of ObservedPart)`
- [ ] T013 [P] Create `src/CodeMem.Core/Records/ObservedEdge.vb`: `SourceDocCommentId`, `Verb As EdgeVerb`, `TargetDocCommentId`, `ViaDocCommentId` (nullable), `Location As SourceLocation`
- [ ] T014 [P] Create `src/CodeMem.Core/Records/RegistryRow.vb`: one field per `code_symbols` column in contracts/schema.sql, nullable `ContainerId`, `ProjectSymbolId`
- [ ] T015 [P] Create `src/CodeMem.Core/Records/RunStamp.vb`: `SolutionId`, `SourceDigest`, `CommitSha` (nullable), `IsDirty` (nullable Boolean), `BuildConfiguration`, `TargetFramework`, `ExtractorVersion`, `SchemaVersion`, `StartedUtc`, `FinishedUtc`
- [ ] T016 [P] Create `src/CodeMem.Core/Records/RunCounts.vb`: exactly the ten counts `SymbolsObserved`, `SymbolsMatched`, `SymbolsReactivated`, `SymbolsNew`, `SymbolsRetired`, `RegistryActiveBefore`, `NotesOrphaned`, `RenameCandidates`, `UnaccountedObserved`, `UnaccountedRegistry`
- [ ] T017 [P] Create `src/CodeMem.Core/Records/RenameCandidate.vb`: `RetiredSymbolId`, `NewDocCommentId`, `BodyHash`, `SamePath As Boolean`, `OffsetDistance` (nullable), `Rank`
- [ ] T018 [P] Create `src/CodeMem.Core/Records/SolutionRecord.vb`: `Id`, `Key`, `Name`, `RepoRoot` (nullable), `LastSeenPath`, `CreatedUtc`, `FirstRunId` (nullable)
- [ ] T019 [P] Create `src/CodeMem.Core/Reconciliation/ReconciliationResult.vb`: `Refreshes`, `Reactivations`, `Inserts`, `Retirements`, `Candidates`, `Counts As RunCounts`

### Extraction option and enum types (`src/CodeMem.Extraction/Run/`)

- [ ] T020 [P] Create `src/CodeMem.Extraction/Run/ExitCode.vb`: `Enum` `Success = 0`, `Failure = 1`, `BuildErrors = 2`, `LockHeld = 3`, `ResidualMismatch = 4`
- [ ] T021 [P] Create `src/CodeMem.Extraction/Run/RunPhase.vb`: `Enum` `None`, `AfterStaging`, `DuringPublish`
- [ ] T022 [P] Create `src/CodeMem.Extraction/Run/ExtractionOptions.vb`: `SolutionPath`, `DbPath`, `Configuration` (default `"Debug"`), `Framework` (nullable), `SolutionKey` (nullable → derived)
- [ ] T023 [P] Create `src/CodeMem.Extraction/Run/RunSeams.vb`: `CorruptStagedCounts As Action(Of RunCounts)` (nullable) and `MutateStaged As Action(Of List(Of ObservedSymbol))` (nullable); XML doc states both are test-only and that `Main` passes `Nothing`

### Fixture solution (`tests/CodeMem.Tests/Fixtures/Sample/`) — FR-035

- [ ] T024 Create `tests/CodeMem.Tests/Fixtures/Sample/Sample.sln` and `Sample.Lib/Sample.Lib.vbproj` (`net8.0` class library, `RootNamespace` `Sample`, Option Strict/Explicit On, Infer Off) with this exact file layout. **Inside a `Namespace Widgets` block** (doc ids `…Sample.Widgets.…`): `Widgets.vb` — one `Namespace Widgets` block containing `ISampleService` interface, its two implementers `AlphaService` and `BetaService`, and `BaseWidget` → `MidWidget` → `LeafWidget` (`Inherits` chain of two) where `BaseWidget` carries `Public Overridable Sub Describe()` with **no overrides and no callers anywhere in the fixture** (the string `Sub Describe(` occurs exactly once in the file; I6/I15 rename it and the fixture must still compile); `Twins.vb` — its own `Namespace Widgets` block containing class `Twins` with `Sub First()` and `Sub Second()` — identical signatures, identical bodies, names differ, no callers (I7 renames both). **At `RootNamespace` level, no `Namespace` block** (doc ids `…Sample.…`): `Fields.vb` — class `Fields` with `Private a, b As Integer` where `b` is referenced nowhere else (US3 sibling test renames it); `Overloads.vb` — class `Overloads` with `Sub Run(x As Integer)` and `Sub Run(x As String)` having identical bodies; `Consumer.vb` — carries the one `Imports System.Text` statement and a class `Consumer` whose method body contains `Dim w As LeafWidget = New LeafWidget()` (a local `As` clause for `uses` and an implicit-constructor `calls` target); no widget class declares a constructor
- [ ] T025 Create `tests/CodeMem.Tests/Fixtures/Sample/Sample.App/Sample.App.vbproj` (`net8.0-windows`, `UseWindowsForms` true, `RootNamespace` `Sample`, same options, ProjectReference `..\Sample.Lib\Sample.Lib.vbproj`) with `MainForm.vb` + `MainForm.Designer.vb` (partial; the designer declares `WithEvents Button1`, `Button2`, `Timer1` and sets `Button1.Size = New Size(75, 23)` — the string `New Size(75, 23)` occurs exactly once in the file, so `Button2.Size` uses a different value such as `New Size(90, 23)`; I4 edits that literal), a `Namespace Widgets` block in a second file `AppWidgets.vb` so `N:Sample.Widgets` is declared in **both** projects (the namespace merge case), three `Handles`-clause handlers of which one handles `Button1.Click, Button2.Click`, one `AddHandler Timer1.Tick, AddressOf OnTick` in `MainForm_Load`, and one `MessageBox.Show(...)` call (external `calls` target)
- [ ] T026 Verify the fixture: `dotnet build tests/CodeMem.Tests/Fixtures/Sample/Sample.sln` → 0 errors; count and record in a comment at the top of `MainForm.vb` the expected totals: `Handles` items = 4, `AddHandler` statements = 1, handler methods = 4

### Test harness (`tests/CodeMem.Tests/Support/`)

- [ ] T027 [P] Create `tests/CodeMem.Tests/Support/FixtureSolution.vb` implementing `IAsyncLifetime`: locate `Fixtures/Sample` by walking up from `AppContext.BaseDirectory` to the directory containing `CodeMem.Tests.vbproj`; run `dotnet restore Sample.sln` once; expose `SolutionPath`, `LibProjectPath`, `FixtureDirectory`
- [ ] T028 [P] Create `tests/CodeMem.Tests/Support/FixtureCollection.vb`: `<CollectionDefinition("Fixture")>` class implementing `ICollectionFixture(Of FixtureSolution)`
- [ ] T029 [P] Create `tests/CodeMem.Tests/Support/TempMap.vb`: `IDisposable` that mints a unique path under `Path.GetTempPath()` for a throwaway `codemem.sqlite` and deletes it on dispose
- [ ] T030 [P] Create `tests/CodeMem.Tests/Support/MapQueries.vb`: the **only** SQL in the test project — named methods `CountRows(db, table, solutionId)`, `ReadSymbols(db, solutionId)`, `ReadEdges(db, solutionId, verb)`, `ReadRuns(db)`, `ReadMapGuid(db)`, `ReadCandidates(db, runId)`, `SetSchemaVersion(db, n)`, `FactSet(db, solutionId)` (symbols keyed by doc id with kind/name/container doc id/project doc id/location/hash; parts; edges; ten counts — surrogate ids, run ids, timestamps and map guid excluded)
- [ ] T031 [P] Create `tests/CodeMem.Tests/Support/MapSnapshot.vb`: `FileBytesHash(path)` and `RowsForSolution(db, solutionId)` returning per-table ordered row dumps for byte-identical comparison
- [ ] T032 [P] Create `tests/CodeMem.Tests/Support/FixtureCopy.vb`: `IDisposable` copying `Fixtures/Sample` to a temp directory excluding `bin/` and `obj/`, running `dotnet restore`, with `Replace(relativeFile, find, replaceWith)` that asserts exactly one occurrence
- [ ] T033 [P] Create `tests/CodeMem.Tests/Support/ExtractorProcess.vb`: runs `dotnet <path to CodeMem.Extractor.dll> <args>` with optional environment variables, captures exit code, stdout, stderr, and elapsed time

**Checkpoint**: solution builds, fixture builds, harness compiles. No product behaviour exists yet.

---

## Phase 3: User Story 1 — First extraction of a compiled solution (Priority: P1) 🎯 MVP

**Goal**: One run against the fixture into a fresh map produces the stamp, every source-declared symbol
with parts and hashes, every edge of the eight verbs, ten counts with zero residuals, and exit 0.

**Independent Test**: `dotnet test --filter US1` — I2, I3, I8 (first half), I12 and scenario 1 pass on
a fresh temp map; then `quickstart.md` "Extract the fixture by hand".

### Slice A — stamp and identity (US1 scenario 1)

- [ ] T034 [US1] Create `src/CodeMem.Extraction/Run/ExtractionRun.vb` with the public surface only: `Public Shared Function Execute(options As ExtractionOptions, seams As RunSeams) As ExitCode` throwing `NotImplementedException`, so US1 tests compile and go Red for the stated reason
- [ ] T035 [US1] Create `tests/CodeMem.Tests/Invariants/US1_FirstRunTests.vb` (`<Collection("Fixture")>`): on a `TempMap`, `Execute` returns `ExitCode.Success`; `map_identity` has 1 row with a 36-char GUID and `schema_version = 1`; `solutions` has 1 row with `key = "Sample"`; `extract_runs` has 1 row with `outcome = 'completed'`, 64-hex `source_digest`, 40-hex `commit_sha` and non-null `is_dirty` (fixture lives inside this git repo), `build_configuration = "Debug"`, non-empty `target_framework`, `extractor_version = "0.1.0"`, `schema_version = 1`, all ten count columns present; elapsed < 60 s (SC-010). Run → Red (`NotImplementedException`) → report Red to the Architect
- [ ] T036 [P] [US1] Create `src/CodeMem.Core/Schema/SchemaVersion.vb`: `Module` with `Public Const Current As Integer = 1`
- [ ] T037 [P] [US1] Create `src/CodeMem.Core/Hashing/Sha256Hex.vb`: `Module` with `Compute(bytes As IEnumerable(Of Byte()))` → 64-char lowercase hex via `System.Security.Cryptography.SHA256`
- [ ] T038 [US1] Create `src/CodeMem.Core/Schema/SchemaRepository.vb`: `CreateSchema(connection)` executing the DDL of `contracts/schema.sql` verbatim (every table, index, CHECK, and `PRAGMA journal_mode = DELETE`), and `ReadSchemaVersion(connection)`; the DDL is the one place `CREATE TABLE` appears in Core
- [ ] T039 [US1] Create `src/CodeMem.Core/Repositories/MapDatabase.vb`: `OpenOrCreate(path)` (create file + schema + `map_identity` row when absent; open otherwise; `PRAGMA foreign_keys = ON`; `SqliteConnection.DefaultTimeout = 0`; refuse when `ReadSchemaVersion <> SchemaVersion.Current` by throwing `SchemaVersionMismatchException`), `BeginImmediate()` (executes `BEGIN IMMEDIATE`; a `SqliteException` with `SqliteErrorCode = 5` (BUSY) is rethrown as `MapLockHeldException`), `Commit()`, `Rollback()`, `IDisposable`; the two exception types live in their own files `src/CodeMem.Core/Repositories/SchemaVersionMismatchException.vb` and `src/CodeMem.Core/Repositories/MapLockHeldException.vb` (Article I)
- [ ] T040 [P] [US1] Create `src/CodeMem.Core/Repositories/MapIdentityRepository.vb`: `Insert(guid, schemaVersion, createdUtc)` (only ever called by `OpenOrCreate` on a fresh file), `ReadGuid()`
- [ ] T041 [P] [US1] Create `src/CodeMem.Core/Repositories/SolutionsRepository.vb`: `EnsureByKey(key, name, lastSeenPath, createdUtc)` returning `SolutionRecord` (insert if absent — `key` is `UNIQUE`), `RefreshLabels(solutionId, repoRoot, lastSeenPath)`, `SetFirstRunIfNull(solutionId, runId)`
- [ ] T042 [P] [US1] Create `src/CodeMem.Core/Repositories/ExtractRunsRepository.vb`: `InsertCompleted(stamp, counts)` and `InsertFailed(stamp, counts)` binding every `extract_runs` column including all ten counts; `outcome` is `'completed'` / `'failed'`
- [ ] T043 [P] [US1] Create `src/CodeMem.Extraction/Workspace/SolutionPaths.vb`: `BaseDirectory(solutionPath)` (directory of the `.sln` or `.vbproj`), `Relative(base, fullPath)` → forward-slash relative path with `..` segments allowed; ordinal comparison helper
- [ ] T044 [US1] Create `src/CodeMem.Extraction/Workspace/SolutionLoader.vb`: `Open(path, configuration, framework)` via `MSBuildWorkspace.Create(properties)` where `properties` carries `Configuration = configuration` and, when `framework` is given, `TargetFramework = framework` — so what is compiled is what is stamped (FR-001, Article V); for `.sln` or `.vbproj`, collecting `WorkspaceFailed` diagnostics; `CompileAll(solution)` returning one `Compilation` per project, each project's actual target framework, and the list of `DiagnosticSeverity.Error` diagnostics across all of them; workspace diagnostics of kind `Failure` surface as `WorkspaceLoadException` in its own file `src/CodeMem.Extraction/Workspace/WorkspaceLoadException.vb`
- [ ] T045 [US1] Create `src/CodeMem.Extraction/Workspace/CompiledInputs.vb`: `Enumerate(solution, basePath)` — the single enumeration (FR-005, Article XII): every `Document` whose path is **not** under `<project directory>/obj/` (research R3; a custom `IntermediateOutputPath` is unsupported), plus every project file, plus the solution file, **deduplicated by full path** (when `--solution` names a `.vbproj`, the solution file and the project file are one input); each as (`RelativePath`, `FullPath`, `Text` with CRLF→LF and leading BOM removed), sorted ordinal by `RelativePath`
- [ ] T046 [P] [US1] Create `src/CodeMem.Extraction/Workspace/SourceDigest.vb`: `Compute(inputs)` = SHA-256 over, per input in order, UTF-8(path), `&H00`, UTF-8(text), `&H00` (research R5)
- [ ] T047 [P] [US1] Create `src/CodeMem.Extraction/Provenance/GitProvenance.vb`: `Read(baseDirectory, inputs)` using `Repository.Discover`; returns (`CommitSha`, `IsDirty`, `RepoRoot`) all null when no repository; `IsDirty` = any input whose `RetrieveStatus(relativeToRepo)` is not `Unaltered`/`Ignored` (modified, staged, or untracked — spec Q3); never launches a process
- [ ] T048 [P] [US1] Create `src/CodeMem.Extractor/CommandLine.vb`: parse `--solution`, `--db`, `--configuration`, `--framework`, `--solution-key`, `--help` per contracts/cli.md into `ExtractionOptions`; unknown/missing → usage text on stderr and exit 1; `--help` lists `CODEMEM_TEST_ABORT_AT` under a "test-only" heading
- [ ] T049 [P] [US1] Create `src/CodeMem.Extractor/SummaryLine.vb`: format exactly `solution=<key> run_id=<n> observed=<n> matched=<n> reactivated=<n> new=<n> retired=<n> registry_before=<n> notes_orphaned=<n> candidates=<n> unaccounted_observed=<n> unaccounted_registry=<n> digest=<64 hex> sha=<40 hex|null>`
- [ ] T050 [US1] Create `src/CodeMem.Extractor/Program.vb`: `Main` → `CommandLine.Parse` → `ExtractionRun.Execute(options, Nothing)` → print summary line on success → return the `ExitCode` integer; nothing else lives here (Article I: executables only wire)
- [ ] T051 [US1] Implement `ExtractionRun.Execute` steps 1–6 and 11–12 of plan.md Run Order: derive key (`--solution-key` or file name without extension), `OpenOrCreate`, `BeginImmediate`, `EnsureByKey`, load with `SolutionLoader.Open(path, options.Configuration, options.Framework)` + compile (exit 2 path deferred to US2 — for now any Error diagnostic throws), enumerate inputs → digest + provenance, build `RunStamp` (`extractor_version` = `GetType(ExtractionRun).Assembly.GetName().Version` as `Major.Minor.Build` — the Extraction assembly, so in-process tests and the executable agree, research R12; `build_configuration` = the option; `target_framework` = `--framework` when given, else the loaded project's actual target framework), `InsertCompleted` with all-zero counts, `RefreshLabels`, `SetFirstRunIfNull`, `Commit`, return `Success`; `Rollback` on any exception
- [ ] T052 [US1] Run `tests/CodeMem.Tests/Invariants/US1_FirstRunTests.vb` → Green; record the Red→Green transition in that file's header comment

### Slice B — symbols, parts, hashes (I2 determinism + symbol facts)

- [ ] T053 [US1] Create `tests/CodeMem.Tests/Invariants/I02_DeterminismTests.vb`: extract the fixture into two fresh `TempMap`s; `MapQueries.FactSet` of each is equal (symbols keyed by doc id with kind/name/container/project/location/hash, parts, edges, ten counts). Run → Red (no symbols written) → report Red
- [ ] T054 [US1] Create `tests/CodeMem.Tests/Invariants/US1_SymbolTests.vb`: after one run — rows exist for `T:Sample.Widgets.ISampleService`, `T:Sample.Widgets.LeafWidget`, `M:Sample.Widgets.Twins.First`, `F:Sample.Fields.a`, `Project:Sample.App`, `Project:Sample.Lib`; `N:Sample.Widgets` is exactly **one** row whose `code_parts` include a path under `Sample.Lib/` and one under `Sample.App/`; no row has `doc_comment_id` ending in `#ctor` for a type with no explicit constructor; no row named `_Button1`; exactly one row for `Button1`; `project_symbol_id IS NULL` exactly for kinds `namespace` and `project`; every `body_hash` is 64 hex; each symbol's `path`/`start_offset` equals its first part in (path, offset) order (FR-010). Run → Red → report Red
- [ ] T055 [US1] Create `src/CodeMem.Extraction/Symbols/PartResolver.vb`: `PartNodeOf(reference)` — research R2 block promotion (`ClassStatement`→`ClassBlock`, `MethodStatement`→`MethodBlock`, property/event/enum/namespace likewise; field `ModifiedIdentifier` → its `VariableDeclaratorSyntax`); `IsNamespacePart(reference)` true only for `NamespaceBlockSyntax`; `LocationOf(node)` → `SourceLocation` via `GetLineSpan()` (+1 line, +1 column) — the one door for every location in the map
- [ ] T056 [US1] Create `src/CodeMem.Extraction/Symbols/TokenTextHasher.vb`: `HashInput(partNode, excludedTokens)` = each `DescendantTokens()` token's `Text` + LF, skipping tokens whose span is in `excludedTokens`; `ExcludedIdentifiers(symbol, partNode)` per research R4 — type/delegate/method/property/event/enum-member identifier, all tokens of a `NamespaceStatement.Name`, and for a field **every** `ModifiedIdentifier.Identifier` of the shared `VariableDeclaratorSyntax` (FR-013 sibling rule); `PartHash` = SHA-256 hex of the input; `BodyHash(parts)` = SHA-256 over the parts' inputs in (path, offset) order separated by `&H00`
- [ ] T057 [P] [US1] Create `src/CodeMem.Extraction/Symbols/ProjectSymbols.vb`: one `ObservedSymbol` per `Project` — kind `Project`, `DocCommentId` = `"Project:" & Path.GetFileNameWithoutExtension(project.FilePath)`, location = project file at offset 0 / length 0 / line 1 / column 1, no parts, `BodyHash` = SHA-256 of empty input, `ContainerDocCommentId` and `ProjectDocCommentId` null (research R14)
- [ ] T058 [US1] Create `src/CodeMem.Extraction/Symbols/SymbolWalker.vb`: `Walk(compilation, projectDocId, basePath)` recursing from `Assembly.GlobalNamespace`; keep iff `Not IsImplicitlyDeclared`, kind maps to `SymbolKind`; `Name` = `"New"` for constructors (the VB surface name, not Roslyn's `.ctor`); ≥ 1 declaring reference in a non-generated document (namespace: ≥ 1 `NamespaceBlockSyntax`); accessors/lambdas/locals/parameters/type parameters never rows; `ProjectDocCommentId` = `projectDocId` for every non-namespace row; `MergeNamespaces(observedFromAllCompilations)` merging namespace symbols by doc id — parts concatenated, primary = first in (path, offset), `BodyHash` recomputed; no other kind merged
- [ ] T059 [P] [US1] Create `src/CodeMem.Core/Repositories/CodeSymbolsRepository.vb` with `ReadActive(solutionId)` → `List(Of RegistryRow)` and `InsertNew(solutionId, symbol, containerId, projectSymbolId, runId)` → new id (`first_seen_run_id = last_seen_run_id = runId`, `is_active = 1`); no `DELETE`, `DROP`, or recreate anywhere in this file — ever
- [ ] T060 [P] [US1] Create `src/CodeMem.Core/Repositories/CodePartsRepository.vb`: `ReplaceForSolution(solutionId, parts)` = `DELETE FROM code_parts WHERE solution_id = @solution_id` then inserts
- [ ] T061 [US1] Create `src/CodeMem.Core/Reconciliation/Reconciler.vb` in its US1 form: `Reconcile(staged, snapshot, retired)` implementing step 1 (duplicate doc-id guard → `DuplicateDocCommentIdException`, in its own file `src/CodeMem.Core/Reconciliation/DuplicateDocCommentIdException.vb`, carrying both locations) and the all-new path (empty snapshot → every symbol is an insert; counts `SymbolsObserved = staged.Count`, `SymbolsNew = staged.Count`, others 0, `NotesOrphaned = 0`); (A)/(A′)/(B)/(C) are added in US3 under their own Red tests
- [ ] T062 [US1] Extend `ExtractionRun.Execute` in `src/CodeMem.Extraction/Run/ExtractionRun.vb` with steps 7–8 and the symbol part of step 11: walk every compilation, `MergeNamespaces`, add project symbols, invoke `seams?.MutateStaged(staged)`, reconcile, then in the transaction insert new rows in kind order (project, namespace, types, members) resolving `container_id` and `project_symbol_id` from a doc-id→id map, and `CodePartsRepository.ReplaceForSolution`; counts now come from `Reconciler`
- [ ] T063 [US1] Run `tests/CodeMem.Tests/Invariants/I02_DeterminismTests.vb` and `tests/CodeMem.Tests/Invariants/US1_SymbolTests.vb` → Green; record in each file's header comment

### Slice C — `handles` edges (I3)

- [ ] T064 [US1] Create `tests/CodeMem.Tests/Invariants/I03_HandlesTests.vb`: walk the fixture's syntax trees counting `HandlesClauseItemSyntax` (expect 4) and `AddHandlerStatementSyntax` (expect 1); assert `handles` edge count = 5; every handler method row has ≥ 1 incoming `handles` edge; edges for `Button1.Click`/`Button2.Click` items carry `via_symbol_id` = the `Button1`/`Button2` rows; the `AddHandler` edge has `via_symbol_id IS NULL`; every `target_doc_comment_id` starts with `E:`. Run → Red → report Red
- [ ] T065 [P] [US1] Create `src/CodeMem.Extraction/Edges/EdgeRule.vb`: `Interface` with `Sub Collect(context As EdgeContext, sink As List(Of ObservedEdge))` and `src/CodeMem.Extraction/Edges/EdgeContext.vb` (compilation, semantic model per tree, `PartResolver`, row-symbol lookup by doc id, project doc id)
- [ ] T066 [P] [US1] Create `src/CodeMem.Extraction/Edges/EnclosingSymbolResolver.vb`: `RowSymbolAt(model, position)` = `GetEnclosingSymbol` then walk `ContainingSymbol` until a symbol that is a row (accessor → `AssociatedSymbol` property; lambda → containing method; field initializer → field) — FR-016
- [ ] T067 [US1] Create `src/CodeMem.Extraction/Edges/HandlesRule.vb`: (a) per `HandlesClauseItemSyntax` on a method: target = bound event `.OriginalDefinition` doc id, `via` = bound `WithEvents` member's doc id when it is a row symbol (null for `MyBase.X` or external members), location = the event name token; (b) per `AddHandlerStatementSyntax` whose delegate operand is `AddressOf <method>`: source = that method, target = bound event of the event operand, `via` null, location = the event name token; unbound → not written
- [ ] T068 [US1] Create `src/CodeMem.Core/Repositories/CodeEdgesRepository.vb`: `ReplaceForSolution(solutionId, edges)` = `DELETE FROM code_edges WHERE solution_id = @solution_id` then inserts binding `source_symbol_id`, `verb`, `target_symbol_id` (nullable), `target_doc_comment_id`, `via_symbol_id` (nullable), five location columns
- [ ] T069 [US1] Extend `ExtractionRun` step 7 to run every registered `EdgeRule` and step 11 to resolve `source`/`target`/`via` ids from the doc-id map (target/via with no row → null id) and call `CodeEdgesRepository.ReplaceForSolution`; stage edges sorted by (path, offset, verb, target doc id) for stable insert order
- [ ] T070 [US1] Run `tests/CodeMem.Tests/Invariants/I03_HandlesTests.vb` → Green; record in its header comment

### Slice D — the other seven verbs and I12 evidence

- [ ] T071 [US1] Create `tests/CodeMem.Tests/Invariants/I12_EvidenceTests.vb`: for every `code_symbols` row **except kind `project`** (its span is the project file itself — exempt) and every `code_edges` row of the solution: `path` is non-null and solution-relative; the text at `[start_offset, start_offset + length)` of that file is non-empty and begins on line `start_line` at column `start_column`; and that span text equals the expected surface name — symbol rows: `name` (`New` for constructors); `part_of`: the **source** symbol's `name`; `calls`/`uses`/`implements`/`extends`/`imports`/`handles`: the target's surface name derived from `target_doc_comment_id` — a `SpecialType` target maps to its VB keyword (`T:System.Int32` → `Integer`, `T:System.String` → `String`, `T:System.Boolean` → `Boolean`, …), `#ctor` → the type name, generic arity `` `n `` and parameter lists stripped, last dotted segment taken; `depends_on`: the referenced project's file name. Run → Red (only `handles` and no other verbs exist) → report Red
- [ ] T072 [US1] Create `tests/CodeMem.Tests/Invariants/US1_EdgeTests.vb`: `part_of` from every non-project row; `T:Sample.Widgets.LeafWidget` `extends` `T:Sample.Widgets.MidWidget`; `AlphaService` and `BetaService` `implements` `ISampleService`; a `uses` edge for a parameter type and one for a local `As` clause; `imports` from a top-level type to `N:System.Text` with `target_symbol_id IS NULL`; `depends_on` `Project:Sample.App` → `Project:Sample.Lib` with `target_symbol_id` set; a `calls` edge to `M:System.Windows.Forms.MessageBox.Show(…)` with null target id; a `calls` edge for `New LeafWidget()` whose target is `T:Sample.Widgets.LeafWidget` (implicit ctor redirected); no `calls` edge whose source is an accessor or lambda doc id; no edge with a verb outside the eight. Run → Red → report Red
- [ ] T073 [P] [US1] Create `src/CodeMem.Extraction/Edges/PartOfRule.vb`: every row symbol except `project` → `ContainingSymbol.OriginalDefinition` doc id; no edge when the container is the global namespace; location = identifier of the primary declaration
- [ ] T074 [P] [US1] Create `src/CodeMem.Extraction/Edges/CallsRule.vb`: for `InvocationExpressionSyntax`, `ObjectCreationExpressionSyntax`, `MemberAccessExpressionSyntax` — source via `EnclosingSymbolResolver`; target `GetSymbolInfo(node).Symbol.OriginalDefinition` (`ReducedFrom` for extension methods); skip when `Symbol Is Nothing`, `CandidateSymbols.Length > 0`, target is local/parameter/type-parameter, or the node lies inside a `Handles`/`Implements` clause; implicit constructor → containing type doc id; location = the invoked name token (or the type name of `New`)
- [ ] T075 [P] [US1] Create `src/CodeMem.Extraction/Edges/UsesRule.vb`: for each named type in the `AsClause` of parameters, return types, fields, properties, events, and `LocalDeclarationStatement` declarators (one edge per named type including generic arguments; arrays → element type; `Integer?` → `Integer`), target `GetTypeInfo(typeSyntax).Type.OriginalDefinition`; skip error types and type parameters; source = declaring row symbol (locals: enclosing row symbol); location = the type syntax span
- [ ] T076 [P] [US1] Create `src/CodeMem.Extraction/Edges/ImplementsRule.vb`: type → each type in `ImplementsStatementSyntax`; member → each name in `ImplementsClauseSyntax`; bound targets only; location = the name span
- [ ] T077 [P] [US1] Create `src/CodeMem.Extraction/Edges/ExtendsRule.vb`: type with `InheritsStatementSyntax` → bound base type; location = the base type name span
- [ ] T078 [P] [US1] Create `src/CodeMem.Extraction/Edges/ImportsRule.vb`: for each file, each `SimpleImportsClauseSyntax` binding to a namespace (aliases and XML imports excluded) → one edge from every top-level type row declared in that file; location = the imported name span; project-level imports have no syntax and are never written
- [ ] T079 [P] [US1] Create `src/CodeMem.Extraction/Edges/DependsOnRule.vb`: per `ProjectReference` → project row → project row; location = first occurrence of the referenced project file name in the referencing `.vbproj` text (offset 0 / line 1 when absent — research R13)
- [ ] T080 [US1] Register the eight rules in `src/CodeMem.Extraction/Run/ExtractionRun.vb` (a `List(Of EdgeRule)` built in `Execute`; 8 call sites justify the interface) and run `I12_EvidenceTests`, `US1_EdgeTests`, `I02_DeterminismTests` → Green; record

### Slice E — counts and residuals (I8, first half)

- [ ] T081 [US1] Create `tests/CodeMem.Tests/Invariants/I08_ResidualTests.vb` with the first-half test: after one clean run `unaccounted_observed = 0`, `unaccounted_registry = 0`, `symbols_observed = symbols_new`, `symbols_matched = 0`, `symbols_reactivated = 0`, `symbols_retired = 0`, `registry_active_before = 0`, `notes_orphaned = 0`, `rename_candidates = 0`. Run → Red (residual columns not yet computed independently — assert on a deliberately wrong placeholder) → report Red
- [ ] T082 [US1] Create `tests/CodeMem.Tests/Guards/CountAuditorTests.vb` (unit, Article III-permitted): `Audit` of consistent counts yields 0/0; counts with `SymbolsMatched + 1` yield `UnaccountedObserved = -1` and `UnaccountedRegistry = -1`; counts with `SymbolsReactivated = 2` and matching observed total yield 0/0. Run → Red → report Red
- [ ] T083 [US1] Create `src/CodeMem.Core/Reconciliation/CountAuditor.vb`: `Audit(counts)` returning a copy with `UnaccountedObserved = SymbolsObserved − (SymbolsMatched + SymbolsReactivated + SymbolsNew)` and `UnaccountedRegistry = RegistryActiveBefore − (SymbolsMatched + SymbolsRetired)`; no reference to `Reconciler` (FR-025)
- [ ] T084 [US1] Wire `CountAuditor.Audit` into `src/CodeMem.Extraction/Run/ExtractionRun.vb` step 10 before publication (the exit-4 branch is US2); write the audited counts in `InsertCompleted`; run `I08_ResidualTests` (first half) and `CountAuditorTests` → Green; FIRE for `CountAuditorTests`: change `+ SymbolsNew` to `- SymbolsNew` → Red → revert → Green; record beside the test

**Checkpoint**: `quickstart.md` "Extract the fixture by hand" works; US1 is the MVP.

---

## Phase 4: User Story 2 — The extractor refuses rather than corrupts (Priority: P2)

**Goal**: Exit 2 on compile errors, exit 3 on a held lock, exit 4 on a residual mismatch, atomic
rollback on abort — each leaving the published map untouched, each proven through the real executable.

**Independent Test**: `dotnet test --filter US2` — I1, I8 (second half), I9, I10 plus the schema-mismatch
and usage-error refusals; every test compares the map before and after.

- [ ] T085 [US2] Create `tests/CodeMem.Tests/Invariants/I01_GreenGateTests.vb`: on a `FixtureCopy`, `Replace("Sample.Lib/Twins.vb", "Sub First()", "Sub First() As Integer")` — a unique string (Twins has two `End Sub`s, so that anchor would trip `Replace`'s exactly-once assertion) that yields a compile error; extract once beforehand so the map has content; `ExtractorProcess` run → exit 2, stderr contains `error BC` and a final `errors=<n>` line, stdout empty, `FileBytesHash` of the map identical before and after. Run → Red (US1 throws instead of exiting 2) → report Red
- [ ] T086 [US2] Implement the exit-2 path in `src/CodeMem.Extraction/Run/ExtractionRun.vb` step 5: on any `DiagnosticSeverity.Error`, write each diagnostic's `ToString()` then `errors=<n>` to stderr, `Rollback`, return `BuildErrors` — before any table is touched; run `I01_GreenGateTests` → Green; FIRE: skip the check → Red → revert → Green; record
- [ ] T087 [US2] Add to `tests/CodeMem.Tests/Invariants/I08_ResidualTests.vb` the second-half test: `Execute` with `RunSeams.CorruptStagedCounts = Sub(c) c.SymbolsMatched += 1` → `ExitCode.ResidualMismatch`; `extract_runs` gains one `outcome = 'failed'` row whose `unaccounted_observed = -1`; `MapSnapshot.RowsForSolution` for `code_symbols`, `code_parts`, `code_edges`, `rename_candidates` identical before and after. Run → Red → report Red
- [ ] T088 [US2] Implement steps 9–10 in `src/CodeMem.Extraction/Run/ExtractionRun.vb`: invoke `seams?.CorruptStagedCounts(counts)` then `Audit`; when either residual ≠ 0, `InsertFailed(stamp, counts)`, `Commit`, print the summary line to stderr with `outcome=failed` prepended, return `ResidualMismatch`; run → Green; FIRE: make the branch test `= 0` instead of `<> 0` → Red → revert → Green; record
- [ ] T089 [US2] Create `tests/CodeMem.Tests/Invariants/I10_LockTests.vb`: open the `TempMap` with `MapDatabase` in the test and hold `BeginImmediate()`; (a) in-process — a second `MapDatabase` on the same path: `BeginImmediate()` throws `MapLockHeldException` in under 1 s (this carries the SC-008 "refused in under 1 second" claim); (b) `ExtractorProcess` run against the same path → exit 3 within 2 s including runtime start-up, stdout empty, no new `extract_runs` row after release. Run → Red → report Red
- [ ] T090 [US2] Implement the exit-3 path in `src/CodeMem.Extraction/Run/ExtractionRun.vb` and `src/CodeMem.Core/Repositories/MapDatabase.vb`: `MapLockHeldException` from `BeginImmediate` (and a failure to open the file for writing) → stderr `lock held` line, return `LockHeld`; confirm `DefaultTimeout = 0` yields immediate `SQLITE_BUSY` (research R6 said this must be tested, not assumed); run → Green; FIRE: set `DefaultTimeout = 5` → test exceeds 1 s → Red → revert → Green; record
- [ ] T091 [US2] Create `tests/CodeMem.Tests/Invariants/I09_AtomicPublishTests.vb`: extract once (previous run); mutate a `FixtureCopy` so the next run has changes; `ExtractorProcess` with `CODEMEM_TEST_ABORT_AT=AfterStaging` → non-zero exit; then `RowsForSolution` for all published tables identical to the previous run — this read is what opens the database and performs SQLite's hot-journal rollback, so it MUST come first; **only after that read** assert no `<db>-journal` file remains; repeat with `CODEMEM_TEST_ABORT_AT=DuringPublish` (fires between parts and edges) in the same order. A journal check before the read is a false Red: after a crash the hot journal legitimately sits on disk until the next connection opens. Run → Red → report Red
- [ ] T092 [US2] Implement the abort seam in `src/CodeMem.Extraction/Run/ExtractionRun.vb`: read `CODEMEM_TEST_ABORT_AT` once; at `AfterStaging` (after step 9) and `DuringPublish` (between `ReplaceForSolution` of parts and of edges) call `Environment.FailFast("CODEMEM_TEST_ABORT_AT")`; run → Green; FIRE: move the `DuringPublish` point to after `Commit` → Red → revert → Green; record
- [ ] T093 [P] [US2] Create `tests/CodeMem.Tests/Guards/RefusalTests.vb`: (a) `MapQueries.SetSchemaVersion(db, 99)` then run → exit 1, no new run row; (b) `ExtractorProcess` with no arguments → exit 1 and usage on stderr; (c) `--db` pointing into a non-existent directory → exit 1 (FR-034: a missing directory is a usage error, not an unobtainable lock); (d) `ExtractorProcess` against the fixture into a fresh `TempMap` → exit 0 and stdout is exactly one line matching `^solution=\S+ run_id=\d+ observed=\d+ matched=\d+ reactivated=\d+ new=\d+ retired=\d+ registry_before=\d+ notes_orphaned=\d+ candidates=\d+ unaccounted_observed=-?\d+ unaccounted_registry=-?\d+ digest=[0-9a-f]{64} sha=([0-9a-f]{40}|null)$` (FR-033, contracts/cli.md). Run → Red where not yet handled → implement the `Failure` mappings in `ExtractionRun`/`Program` (database errors → exit 1 with the SQLite message, including any constraint name, on stderr) → Green; record
- [ ] T094 [P] [US2] Create `tests/CodeMem.Tests/Guards/SchemaConstraintTests.vb` (real SQLite, Article XII — the schema's rules fire, code does not restate them): each of these inserts throws `SqliteException` naming the constraint — second `map_identity` row; duplicate `solutions.key`; `commit_sha` set with `is_dirty` null; second active `code_symbols` row for one identity; `class` row with null `project_symbol_id`; `project` row with non-null `project_symbol_id`; `via_symbol_id` on a `calls` edge; verb `'produces'`; `first_seen_run_id` pointing at a missing run — and a retired duplicate identity **is** allowed. Then, through the production route (Article XIII): `Execute` with `RunSeams.MutateStaged = Sub(s) <set one class symbol's ProjectDocCommentId to Nothing>` → `ExitCode.Failure`, no run row written, and stderr contains the CHECK's text `kind IN ('namespace', 'project')` — proving a constraint failure propagates as exit 1 with the constraint's name (Article XII: code lets the constraint fire and surfaces it). These tests are their own fire demonstration; record

**Checkpoint**: every refusal in contracts/cli.md is proven through the executable; the map is never half-written.

---

## Phase 5: User Story 3 — Identity survives re-extraction (Priority: P3)

**Goal**: (A) matching, (A′) reactivation, (B) rename candidates with (C) rank, retirement, designer-part
hashing and map identity — each on its original id.

**Independent Test**: `dotnet test --filter US3` — I4, I5, I6, I7, I14, I15 and the sibling-identifier test.

- [ ] T095 [US3] Create `tests/CodeMem.Tests/Invariants/I05_IdentityMatchTests.vb`: run twice on one map unchanged; every symbol keeps its `id`, `first_seen_run_id` unchanged, `last_seen_run_id` = run 2; run-2 counts: `symbols_matched = symbols_observed`, `symbols_new = 0`, `symbols_retired = 0`, `symbols_reactivated = 0`, `registry_active_before = symbols_observed`, residuals 0. Run → Red (US1 reconciler inserts everything again — the partial unique index makes it throw) → report Red
- [ ] T096 [US3] Add to `CodeSymbolsRepository`: `RefreshMatched(id, name, containerId, projectSymbolId, location, bodyHash, runId)` (`UPDATE … SET … last_seen_run_id = @run_id`), `Retire(id)` (`UPDATE code_symbols SET is_active = 0 WHERE id = @id`); implement `Reconciler` step 2 (A) against `snapshot` by doc id → refresh, step 4 retire unobserved, counts `SymbolsMatched`, `SymbolsRetired`, `RegistryActiveBefore = snapshot.Count`; publication order per data-model.md steps 2 and 5; run → Green; record
- [ ] T097 [P] [US3] Create `tests/CodeMem.Tests/Invariants/I14_MapIdentityTests.vb`: `ReadMapGuid` equal after runs 1, 2 and 3 on one map. Run → expected Green already; FIRE: make `OpenOrCreate` call `MapIdentityRepository.Insert` on an existing file → Red → revert → Green; record (Article II: a guard is trusted only after it has fired)
- [ ] T098 [P] [US3] Create `tests/CodeMem.Tests/Invariants/I04_DesignerChangeTests.vb`: on a `FixtureCopy`, run; record `body_hash` of `T:Sample.MainForm` and the `part_hash` of its `MainForm.vb` part; `Replace("Sample.App/MainForm.Designer.vb", "New Size(75, 23)", "New Size(80, 23)")` (a token change); run again; `body_hash` changed, `MainForm.vb` `part_hash` unchanged, `id` unchanged. Run → expected Green; FIRE: make `PartResolver.PartNodeOf` return the compilation unit → Red → revert → Green; record
- [ ] T099 [US3] Create `tests/CodeMem.Tests/Invariants/I06_RenameCandidateTests.vb`: on a `FixtureCopy`, run; `Replace("Sample.Lib/Widgets.vb", "Sub Describe(", "Sub Explain(")`; run 2: old `M:…Describe` row `is_active = 0` with its id, new `M:…Explain` row minted, exactly one `rename_candidates` row (`retired_symbol_id`, `new_symbol_id`, `rank = 1`, `same_path = 1`, `offset_distance = 0`), no other row changed `id` or `is_active`, `rename_candidates` count = 1; run 3 unchanged: no new candidate. Run → Red → report Red
- [ ] T100 [US3] Create `src/CodeMem.Core/Reconciliation/ProximityRanker.vb`: `Rank(newSymbol, retiredRows)` ordering by (`same_path` desc, `offset_distance` asc nulls last, retired `id` asc) and assigning `rank` 1..n; `same_path` = paths equal; `offset_distance` = `|retired.start_offset − new.StartOffset|` when same path else null — evidence only, no threshold (Article VI (C))
- [ ] T101 [US3] Create `src/CodeMem.Core/Repositories/RenameCandidatesRepository.vb`: `Insert(solutionId, runId, retiredSymbolId, newSymbolId, bodyHash, samePath, offsetDistance, rank)`; append-only — no `UPDATE`/`DELETE`
- [ ] T102 [US3] Implement step 5 (B) in `src/CodeMem.Core/Reconciliation/Reconciler.vb`: for each **new** symbol, `R` = rows retired this run with equal `kind`, equal resolved container id, equal `body_hash`; step 6 (C) via `ProximityRanker`; `counts.RenameCandidates = Σ|R|`; publication step 8 inserts candidates after new ids exist; run `I06_RenameCandidateTests` → Green; FIRE: compare `body_hash` with `<>` → Red → revert → Green; record
- [ ] T103 [US3] Create `tests/CodeMem.Tests/Invariants/I07_AmbiguousRenameTests.vb`: on a `FixtureCopy`, run; rename both twins in one edit (`First`→`Uno`, `Second`→`Dos` in `Sample.Lib/Twins.vb`); run 2: both old rows retired with ids intact, two new rows, each new row has exactly two candidates, ranks 1 and 2, the rank-1 candidate of `Uno` is the retired row with the smaller `offset_distance`, `rename_candidates` count = 4, registry otherwise unchanged. Run → Red or Green as (B)/(C) dictate — if Green, FIRE: reverse the rank order → Red → revert; record
- [ ] T104 [US3] Create `tests/CodeMem.Tests/Invariants/I15_ReactivationTests.vb`: on a `FixtureCopy`, run 1; rename `Describe`→`Explain`, run 2; rename `Explain`→`Describe`, run 3: `M:…Describe` row is the run-1 row (same `id`), `is_active = 1`, `first_seen_run_id` = run 1, `last_seen_run_id` = run 3; `Explain` row retired; run-3 counts `symbols_reactivated = 1`, `symbols_new = 0`, `rename_candidates = 0`, both residuals 0; total `code_symbols` rows unchanged from run 2. Run → Red → report Red
- [ ] T105 [US3] Add to `src/CodeMem.Core/Repositories/CodeSymbolsRepository.vb`: `ReadRetiredByDocIds(solutionId, docIds)` (ordered `last_seen_run_id DESC, id DESC`) and `Reactivate(id, name, containerId, projectSymbolId, location, bodyHash, runId)` (`UPDATE … SET is_active = 1, … last_seen_run_id = @run_id` — `first_seen_run_id` untouched); implement `Reconciler` step 3 (A′): for each (A) miss, the most recently retired row with that doc id → reactivation, else new; `SymbolsReactivated` counted; publication step 3 before inserts; the doc-id→id map includes reactivated rows; run `I15_ReactivationTests`, then the whole US3 filter → Green; FIRE: skip (A′) → I15 Red → revert → Green; record
- [ ] T106 [P] [US3] Create `tests/CodeMem.Tests/Invariants/US3_SiblingIdentifierTests.vb`: on a `FixtureCopy`, run; record `body_hash` of `F:Sample.Fields.a`; `Replace("Sample.Lib/Fields.vb", "a, b As Integer", "a, c As Integer")`; run 2: `a`'s `body_hash` unchanged and `id` kept; `b` retired; `c` new. Run → expected Green from T056; FIRE: exclude only the symbol's own identifier → Red → revert → Green; record (FR-013)

**Checkpoint**: ids are durable across rename, revert, designer edits and re-runs; candidates are proposals only.

---

## Phase 6: User Story 4 — One file, many solutions (Priority: P4)

**Goal**: A second solution in the same map leaves the first byte-identical; write scope is exactly `WHERE solution_id = @solution_id`; `--solution-key` separates same-named solutions.

**Independent Test**: `dotnet test --filter US4` — I11 and the key-override test.

- [ ] T107 [US4] Create `tests/CodeMem.Tests/Invariants/I11_MultiSolutionTests.vb`: extract `Sample.sln` (key `Sample`) into a `TempMap`; `RowsForSolution` snapshot A; extract `Sample.Lib/Sample.Lib.vbproj` (key `Sample.Lib`) into the same map; A's rows identical to the snapshot; `solutions` has 2 rows; no `id` in `code_symbols`, `code_parts`, `code_edges`, `extract_runs` appears under both `solution_id`s; B's `N:Sample.Widgets` row is distinct from A's. Run → Red or Green — if Green, FIRE: drop the `WHERE solution_id` from `CodePartsRepository.ReplaceForSolution` → Red → revert → Green; record
- [ ] T108 [US4] Create `tests/CodeMem.Tests/Invariants/US4_SolutionKeyTests.vb`: extract `Sample.sln` twice, the second time with `--solution-key Other` via `ExtractorProcess`; `solutions` has rows `Sample` and `Other`; the `Other` run's counts show all symbols new; the `Sample` rows are untouched; `last_seen_path` and `repo_root` on `Sample` are refreshed after a run from a `FixtureCopy` at a different path while `key` and `id` are unchanged (FR-032 labels-not-key). Run → Red if `--solution-key` is unparsed → implement in `CommandLine`/`ExtractionRun` → Green; record

---

## Phase 7: User Story 5 — Operator acceptance against a real solution (Priority: P5)

**Goal**: A Skip-armed runner reports counts, `handles` written vs. counted, partial types, residuals and elapsed time for any solution the Operator names.

**Independent Test**: unset `CODEMEM_ACCEPT_SOLUTION` → runner reported Skipped; set it → the report prints.

- [ ] T109 [US5] Create `tests/CodeMem.Tests/Acceptance/AcceptanceRunner.vb`: `<SkippableFact>` with `Skip.If(String.IsNullOrEmpty(Environment.GetEnvironmentVariable("CODEMEM_ACCEPT_SOLUTION")))`; extract into a `TempMap` via `ExtractorProcess` timing the run; independently walk the solution's syntax trees counting `HandlesClauseItemSyntax` + `AddHandlerStatementSyntax` and partial types (types with > 1 declaring reference); print `summary line`, `handles_written=<n> handles_in_source=<n>`, `partial_types=<n>`, both residuals, `elapsed_ms=<n>`; assert only that the process exited 0 — everything else is reported, never gated (FR-039)
- [ ] T110 [US5] Confirm in `dotnet test` output that `AcceptanceRunner` is listed as **Skipped** with the variable unset, and that with it set to `tests/CodeMem.Tests/Fixtures/Sample/Sample.sln` the report prints `handles_written=5 handles_in_source=5 partial_types=1`; record both outputs in the Carve-Out Register row of plan.md

---

## Phase 8: Guards & Cross-Cutting

**Purpose**: The constitution's review gates made mechanical, the I13 tripwire, and quickstart validation.

- [ ] T111 [P] Create `tests/CodeMem.Tests/Guards/TripwireTests.vb` (I13): read every `.vb` under `src/CodeMem.Core/Repositories/` and `src/CodeMem.Core/Schema/`; assert the count of `INSERT INTO` + `UPDATE ` occurrences in string literals is > 0 **first**; then assert zero occurrences of `DELETE FROM code_symbols`, `DROP TABLE code_symbols`, `DROP TABLE IF EXISTS code_symbols`, a `CREATE TABLE code_symbols` preceded anywhere by a `DROP`, and `ON DELETE CASCADE` anywhere in the DDL (each destructive form asserted separately). FIRE: add a method containing `"DELETE FROM code_symbols WHERE id = @id"` → Red → revert → Green; record
- [ ] T112 [P] Create `tests/CodeMem.Tests/Guards/SqlLocationGateTests.vb`: every `.vb` under `src/` **outside** `CodeMem.Core/Repositories/` and `CodeMem.Core/Schema/` contains no string literal matching `\b(SELECT|INSERT|UPDATE|DELETE|CREATE TABLE|PRAGMA)\b`; every `.vb` under `tests/` outside `Support/MapQueries.vb` and `Guards/SchemaConstraintTests.vb` likewise; and `New SqliteConnection` appears in exactly one production file, `src/CodeMem.Core/Repositories/MapDatabase.vb` (FR-003: the extractor opens no database other than its map). FIRE: add a `SELECT` literal to `ExtractionRun.vb` → Red → revert → Green; record
- [ ] T113 [P] Create `tests/CodeMem.Tests/Guards/FileHeaderGateTests.vb`: every `.vb` under `src/` and `tests/` (excluding `Fixtures/`, `bin/`, `obj/`) begins with a comment block containing its own file name, its project name, `Description:`, `Author: RCH Automation LLC`, and `Created:` with an ISO date; and every `Public` `Class`/`Module`/`Interface`/`Enum`/`Sub`/`Function`/`Property`/`Event` declaration is immediately preceded by a `''' <summary>` block. FIRE: delete one header → Red → revert → Green; record
- [ ] T114 Run `dotnet test tests/CodeMem.Tests` in full: I1–I15, all guards Green, `AcceptanceRunner` Skipped, whole collection within SC-010 plus restore time; paste the totals into `specs/001-extractor-codemem-sqlite/quickstart.md` under "Run the test suite"
- [ ] T115 Execute every command in `quickstart.md` by hand — build, hand extraction twice (second run `matched=<n> reactivated=0 new=0 retired=0`, same digest), the four refusal rows, the acceptance run — and correct any quickstart line that does not match observed output
- [ ] T116 Review pass against the Review Gates in constitution v1.2.0 for every file touched in this feature: Option settings (T007 gate), no SQL outside repositories (T112), fire demonstration + production-route test for every guard (grep for `' FIRE:` in every `Guards/` and `Invariants/` file), no rule validated at two doors, every `extract_runs` field of Article V and every count of Article VIII written and tested (T035, T081, T095), tripwire (T111), header/XML docs (T113), new abstractions justified (`EdgeRule` 8 sites, `RunSeams` justified in plan.md); record the pass in the commit message

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)** → **Foundational (Phase 2)** → **US1 (Phase 3)** — strictly sequential; nothing runs before US1's Slice A because `ExtractionRun.Execute` is the production route every later test enters through (Article XIII).
- **US2 (Phase 4)** depends on US1 Slices A and E (the run exists; counts exist).
- **US3 (Phase 5)** depends on US1 (symbols, parts, hashes, the US1-form `Reconciler`).
- **US4 (Phase 6)** depends on US1; independent of US2/US3 (but I11's fire demonstration touches `CodePartsRepository`, so run it after US3 is Green to avoid masking).
- **US5 (Phase 7)** depends on US1 and the `ExtractorProcess` harness.
- **Guards (Phase 8)**: T111–T113 can be written any time after Phase 2 but only go Green once their subject files exist; T114–T116 last.

### User Story Dependencies

- **US1**: after Phase 2. Slices A → B → C → D → E in order (each slice's implementation extends `ExtractionRun`).
- **US2**: after US1 Slice E. Tests within US2 are independent of each other.
- **US3**: after US1. T095/T096 (A) before T099–T102 (B)/(C) before T104/T105 (A′); T097, T098, T106 independent.
- **US4**: after US1.
- **US5**: after US1.

### Within Each Story — the Article II loop

Every test task: write → run → Red for the stated reason → **report Red to the Architect** → then and only then the paired implementation task → Green → record. Every guard additionally carries its FIRE demonstration beside the test.

### Parallel Opportunities

- Phase 1: T002–T005 in parallel after T001.
- Phase 2: T008–T023 all parallel; T027–T033 all parallel; T024→T025→T026 sequential.
- US1 Slice A: T036, T037, T040–T043, T046–T049 parallel once T035 is Red; T038→T039→T051 sequential.
- US1 Slice B: T057, T059, T060 parallel with T055→T056→T058.
- US1 Slice D: T073–T079 all parallel (seven rule files) once T071/T072 are Red.
- US2: T093, T094 parallel with the I1/I8/I9/I10 pairs.
- US3: T097, T098, T106 parallel with the T095–T105 chain.
- Phase 8: T111–T113 parallel.

---

## Parallel Example: User Story 1, Slice D

```text
# After T071 and T072 are Red and reported, launch the seven rule files together:
Task: "Create src/CodeMem.Extraction/Edges/PartOfRule.vb"      (T073)
Task: "Create src/CodeMem.Extraction/Edges/CallsRule.vb"       (T074)
Task: "Create src/CodeMem.Extraction/Edges/UsesRule.vb"        (T075)
Task: "Create src/CodeMem.Extraction/Edges/ImplementsRule.vb"  (T076)
Task: "Create src/CodeMem.Extraction/Edges/ExtendsRule.vb"     (T077)
Task: "Create src/CodeMem.Extraction/Edges/ImportsRule.vb"     (T078)
Task: "Create src/CodeMem.Extraction/Edges/DependsOnRule.vb"   (T079)
# Then T080 registers them and runs I12 + US1_EdgeTests + I02 to Green.
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 → Phase 2 (project skeletons, records, fixture, harness).
2. Phase 3 Slices A–E, each Red → report → Green.
3. **STOP and VALIDATE**: `quickstart.md` hand extraction; two fresh maps identical (I2); `handles` count matches source (I3); every row traces to a line (I12); residuals 0.
4. A map file with correct facts exists — the feature's stated deliverable.

### Incremental Delivery

1. US1 → the map is correct on a first run.
2. US2 → the map can never be made incorrect by a failed, locked, mismatched or aborted run.
3. US3 → ids survive change; candidates are proposed, reverts reactivate.
4. US4 → many solutions in one file.
5. US5 → an Operator can point it at anything and read the numbers.
6. Phase 8 → the gates are mechanical and the quickstart is true.

### Notes

- 116 tasks: Setup 7 · Foundational 26 · US1 51 · US2 10 · US3 12 · US4 2 · US5 2 · Guards 6.
- No task introduces an abstraction beyond `EdgeRule` (8 call sites) and `RunSeams` (justified in plan.md Complexity Tracking).
- `DELETE` appears in exactly two production methods — `CodePartsRepository.ReplaceForSolution` and `CodeEdgesRepository.ReplaceForSolution` — both scoped `WHERE solution_id = @solution_id`, neither touching `code_symbols`.
- Commit after each Green (one slice or one invariant per commit); the commit message names the invariant and whether a FIRE demonstration was recorded.

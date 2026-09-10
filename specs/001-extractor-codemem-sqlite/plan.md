# Implementation Plan: CodeMem Stage A — Extractor and codemem.sqlite

**Branch**: `001-extractor-codemem-sqlite` | **Date**: 2026-09-09 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/001-extractor-codemem-sqlite/spec.md`

**Governing document**: `.specify/memory/constitution.md` v1.2.0. Article IV requires every relationship
verb's extraction rule to be written in the plan; they are, under *Extraction Rules* below.

## Summary

A VB.NET console extractor loads a VB.NET solution through Roslyn's `MSBuildWorkspace`, refuses unless
every project compiles with zero errors, and writes source-declared symbols, their declaring parts, and
eight typed relationship verbs into one SQLite file under a run stamp. Identity is reconciled against a
never-truncated registry (Article VI (A)/(A′)/(B)/(C)), ten counts with two independent residuals gate
publication (Article VIII), and publication is one `BEGIN IMMEDIATE` transaction that doubles as the
inter-process lock (Article V). Staging is in memory. Roslyn is pinned to **4.14.0** because the 5.x
line does not ship `MSBuildWorkspace` for .NET 8 (verified, [research.md](research.md) R1).

## Technical Context

**Language/Version**: VB.NET on .NET 8 (`net8.0`; fixture WinForms project `net8.0-windows`). Option
Strict On, Option Explicit On, Option Infer Off in every project file. `GenerateDocumentationFile` on
for XML output (enforcement is a Review Gate — the VB compiler emits no diagnostic, verified).

**Primary Dependencies** (all verified against NuGet on 2026-09-09):

| Package | Version | Project | Why |
|---------|---------|---------|-----|
| Microsoft.CodeAnalysis.VisualBasic.Workspaces | 4.14.0 | Extraction | VB semantic model |
| Microsoft.CodeAnalysis.Workspaces.MSBuild | 4.14.0 | Extraction | `MSBuildWorkspace`; out-of-process build host finds the SDK, no Locator needed |
| Microsoft.Data.Sqlite | 8.0.31 | Core | Direct SQLite access, no ORM (Article XI) |
| LibGit2Sharp | 0.32.0 | Extraction | In-process commit sha + dirty flag (FR-006) |
| xunit / xunit.runner.visualstudio / Microsoft.NET.Test.Sdk | 2.9.3 / 3.1.5 / 17.14.1 | Tests | Collection fixtures = loaded once per collection |
| Xunit.SkippableFact | 1.5.85 | Tests | Runtime Skip for the acceptance runner |

**Storage**: one SQLite file named by `--db`; schema in [contracts/schema.sql](contracts/schema.sql);
`journal_mode = DELETE`, `foreign_keys = ON`.

**Testing**: xUnit; real SQLite, real compiled fixture; no mocks (Article III). Each invariant I1–I14 is
one test, Red first (Article II). Fixture restored once per collection with `dotnet restore`.

**Target Platform**: Windows 11 developer workstation (WinForms fixture needs
`Microsoft.WindowsDesktop.App`); the extractor itself is cross-platform .NET 8.

**Project Type**: CLI executable over class libraries.

**Performance Goals**: SC-010 — fixture extraction into a fresh map in under 60 s (test-loop budget,
not a scale claim). No other target this feature; the acceptance runner reports elapsed time.

**Constraints**: Article XV — no runtime dependency outside .NET; PowerShell/SQL as dev-time artifacts
only. Article IX — opens no database other than the map. Article IV — no heuristics, no network.

**Scale/Scope**: unbounded this feature (spec Q5); in-memory staging accepted.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Article / Gate | Status | How this plan satisfies it |
|----------------|--------|----------------------------|
| I. Library-First | **PASS with justification** | Four projects: Core, **Extraction (new)**, Extractor, Tests. The Extractor executable only wires. Justification in Complexity Tracking. One type per `.vb` file (file list below). |
| II. Test-First | PASS | Every invariant is a Red-first test; each guard's fire demonstration is a task; vacuous-Red rule applied to I13 (count positive writes before asserting absence). |
| III. Integration-First | PASS | Real `MSBuildWorkspace`, real SQLite file, committed fixture solution. Unit tests only for `CountAuditor` and `TokenTextHasher`, which never substitute for fixture tests. |
| IV. Compiler Fact Only | PASS | Eight verbs, each with a deterministic rule below. Determinism defined over the logical fact set (I2 on two fresh maps). Compiled-inputs enumeration is a path rule, not a heuristic. |
| V. Green Only, Stamped | PASS | Exit 2 writes nothing on any Error diagnostic. `extract_runs` inserted first inside the publication transaction. Digest always present; sha/flag nullable. `BEGIN IMMEDIATE` = lock; second extractor exits 3 immediately. |
| VI. Reconcile, Never Truncate | PASS | (A) on `solution_id` + `doc_comment_id` against active rows; (A′) reactivation of the most recently retired row carrying an identity no active row holds — id kept, `first_seen_run` unchanged, counted as `symbols_reactivated` (I15); (B) kind + container + `body_hash` against rows retired this run, new symbols only; (C) offset proximity + rank as evidence only, never a threshold. Partial unique index enforces one active row per identity. No DELETE/DROP/recreate/cascade reaches `code_symbols`; I13 tripwire asserts it. |
| VII. Evidence on Every Row | PASS | Every symbol, part, and edge row has path + offset/length + line/column (NOT NULL). Project rows locate at the project file. |
| VIII. Counts That Reconcile | PASS | Ten counts including `symbols_reactivated`; `CountAuditor` computes both residuals from the counts alone in a separate class (observed side includes reactivated; registry side does not); non-zero → exit 4, failed row committed, published tables untouched. |
| IX. One File, Many Solutions, One Writer | PASS | Every fact row carries `solution_id`; all writes are `WHERE solution_id = @solution_id`; `map_identity` written once. Tests use temp map files (role, not path). |
| X. Absence Must Be Representable | PASS | `commit_sha`/`is_dirty`/`target_symbol_id`/`via_symbol_id`/`offset_distance`/`repo_root`/`first_run_id` nullable; no JSON/blob columns; `CHECK` ties `is_dirty` null to `commit_sha` null. |
| XI. Anti-Abstraction | **PASS with justification** | `Microsoft.Data.Sqlite` direct; all SQL in named repository methods in Core. One interface is introduced ahead of three call sites (`RunSeams`) — justified in Complexity Tracking. |
| XII. One Door for Every Rule | PASS | Schema owns structural invariants (see schema.sql); code lets constraints fire and surfaces `SqliteException` with the constraint text. Compiled-inputs enumeration is one function feeding digest and dirty flag. |
| XIII. Production-Route Reachability | PASS | Tests invoke `ExtractionRun.Execute` — the method `Main` calls — and spawn the real executable for I9/I10. |
| XIV. Archive, Never Delete | PASS | No retired code yet; `_Archive/` convention recorded in quickstart. |
| XV. Compiled .NET, No Foreign Runtime | PASS | All dependencies are NuGet .NET packages; `dotnet restore` in tests is the platform SDK, dev-time. |
| Gate: Option settings in every touched project file | PASS | Each `.vbproj` carries `OptionStrict/OptionExplicit/OptionInfer` explicitly (no `Directory.Build.props`, so the gate is checkable per file). |
| Gate: no SQL outside a named repository method | PASS | Repositories in `CodeMem.Core/Repositories/`; tripwire test scans for SQL keywords outside that folder. |
| Gate: header block + XML docs on every file | PASS | Task per file; reviewer-enforced (compiler cannot). |
| Gate: any new abstraction has three call sites | **Justified** | See Complexity Tracking. |

**Post-design re-check**: unchanged. Two justified items, no violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-extractor-codemem-sqlite/
├── plan.md              # This file
├── research.md          # Phase 0: verified facts and decisions R1–R14
├── data-model.md        # Phase 1: entities, reconciliation algorithm, counts, staging records
├── quickstart.md        # Phase 1: build, test, run, expected outcomes
├── contracts/
│   ├── schema.sql       # DDL, schema version 1
│   └── cli.md           # Command line, exit codes, summary line, environment variables
└── tasks.md             # Phase 2 (/speckit-tasks — not created here)
```

### Source Code (repository root)

```text
CodeMem.sln
src/
├── CodeMem.Core/                        # schema, records, repositories, reconciliation — no Roslyn
│   ├── CodeMem.Core.vbproj
│   ├── Schema/
│   │   ├── SchemaVersion.vb             # Module: Current = 1
│   │   └── SchemaRepository.vb          # CreateSchema(), ReadSchemaVersion()  (DDL lives here)
│   ├── Records/
│   │   ├── SourceLocation.vb            # Structure: Path, StartOffset, Length, StartLine, StartColumn
│   │   ├── SymbolKind.vb                # Enum
│   │   ├── EdgeVerb.vb                  # Enum
│   │   ├── ObservedSymbol.vb            # staged symbol (doc id, kind, name, container doc id, location, hash)
│   │   ├── ObservedPart.vb
│   │   ├── ObservedEdge.vb
│   │   ├── RegistryRow.vb               # a code_symbols row as read
│   │   ├── RunStamp.vb                  # provenance fields of extract_runs
│   │   ├── RunCounts.vb                 # the ten counts
│   │   ├── RenameCandidate.vb
│   │   └── SolutionRecord.vb
│   ├── Repositories/
│   │   ├── MapDatabase.vb               # Open/Create, BeginImmediate (the lock), Commit/Rollback
│   │   ├── MapIdentityRepository.vb
│   │   ├── SolutionsRepository.vb       # EnsureByKey, RefreshLabels, SetFirstRun
│   │   ├── ExtractRunsRepository.vb     # InsertCompleted, InsertFailed
│   │   ├── CodeSymbolsRepository.vb     # ReadActive, ReadRetiredByDocIds, RefreshMatched, Reactivate, InsertNew, Retire — no DELETE
│   │   ├── CodePartsRepository.vb       # ReplaceForSolution
│   │   ├── CodeEdgesRepository.vb       # ReplaceForSolution
│   │   └── RenameCandidatesRepository.vb
│   ├── Reconciliation/
│   │   ├── Reconciler.vb                # (A)/(B)/(C) over staged symbols + registry snapshot → ReconciliationResult
│   │   ├── ReconciliationResult.vb
│   │   ├── ProximityRanker.vb           # (C) evidence and rank
│   │   └── CountAuditor.vb              # residuals from counts alone (FR-025); its own file, own logic
│   └── Hashing/
│       └── Sha256Hex.vb                 # Module: hex SHA-256 over byte sequences
├── CodeMem.Extraction/                  # Roslyn-facing library (NEW — see Complexity Tracking)
│   ├── CodeMem.Extraction.vbproj
│   ├── Workspace/
│   │   ├── SolutionLoader.vb            # MSBuildWorkspace open, compilations, Error diagnostics
│   │   ├── CompiledInputs.vb            # THE enumeration: documents (minus obj/), project files, solution file
│   │   ├── SourceDigest.vb              # R5
│   │   └── SolutionPaths.vb             # solution-relative, forward-slash paths
│   ├── Provenance/
│   │   └── GitProvenance.vb             # LibGit2Sharp: sha, dirty over CompiledInputs, repo root
│   ├── Symbols/
│   │   ├── SymbolWalker.vb              # visits declared symbols, applies kind + implicit filters
│   │   ├── PartResolver.vb              # R2 block promotion, namespace CompilationUnit exclusion
│   │   ├── TokenTextHasher.vb           # R4 token serialization, own-identifier exclusion
│   │   └── ProjectSymbols.vb            # Project:<name> rows
│   ├── Edges/
│   │   ├── EdgeRule.vb                  # Interface implemented by the eight rules (8 call sites)
│   │   ├── PartOfRule.vb
│   │   ├── CallsRule.vb
│   │   ├── UsesRule.vb
│   │   ├── ImplementsRule.vb
│   │   ├── ExtendsRule.vb
│   │   ├── ImportsRule.vb
│   │   ├── DependsOnRule.vb
│   │   ├── HandlesRule.vb
│   │   └── EnclosingSymbolResolver.vb   # FR-016 attribution walk
│   └── Run/
│       ├── ExtractionOptions.vb
│       ├── RunSeams.vb                  # test seams (Complexity Tracking)
│       ├── RunPhase.vb                  # Enum for the abort seam
│       ├── ExtractionRun.vb             # orchestrator: steps 1–8 of the run order
│       └── ExitCode.vb                  # Enum: Success=0, Failure=1, BuildErrors=2, LockHeld=3, ResidualMismatch=4
├── CodeMem.Extractor/                   # console entry point — wiring only
│   ├── CodeMem.Extractor.vbproj
│   ├── Program.vb                       # parse → ExtractionRun.Execute(options, Nothing) → exit code
│   ├── CommandLine.vb                   # --solution --db --configuration --framework --solution-key
│   └── SummaryLine.vb                   # FR-033 one-line formatter
tests/
└── CodeMem.Tests/
    ├── CodeMem.Tests.vbproj
    ├── Fixtures/
    │   └── Sample/                      # committed fixture solution (FR-035)
    │       ├── Sample.sln
    │       ├── Sample.App/              # WinForms net8.0-windows: MainForm.vb + MainForm.Designer.vb, handlers, AddHandler
    │       └── Sample.Lib/              # classlib: interface + 2 implementers, Inherits chain, overloads, twins
    ├── Support/
    │   ├── FixtureCollection.vb         # [CollectionDefinition] + ICollectionFixture(Of FixtureSolution)
    │   ├── FixtureSolution.vb           # restore once, load once, expose paths
    │   ├── FixtureCopy.vb               # temp copy for mutation tests
    │   ├── TempMap.vb                   # throwaway codemem.sqlite at a temp path
    │   ├── MapSnapshot.vb               # byte/row snapshots for I1, I9, I11
    │   └── ExtractorProcess.vb          # spawns the real executable (I9, I10)
    ├── Invariants/
    │   ├── I01_GreenGateTests.vb … I14_MapIdentityTests.vb   (one file per invariant)
    ├── Guards/
    │   ├── TripwireTests.vb             # I13 + SQL-outside-repositories gate
    │   └── CountAuditorTests.vb         # unit; fire demonstration recorded beside it
    └── Acceptance/
        └── AcceptanceRunner.vb          # [SkippableFact] on CODEMEM_ACCEPT_SOLUTION
```

**Structure Decision**: `src/` + `tests/` split, four projects. `CodeMem.Core` stays Roslyn-free so a
later read-only MCP server can reference it without carrying the compiler. All Roslyn-facing logic is
in `CodeMem.Extraction`; the executable parses arguments and calls one method.

## Run Order (FR-004 … FR-034, Article V)

Executed by `ExtractionRun.Execute(options, seams)`:

1. Resolve options; derive solution key; compute solution-relative path base.
2. `MapDatabase.OpenOrCreate(dbPath)`: create schema + `map_identity` if absent; read
   `schema_version`, refuse on mismatch → exit 1.
3. `BeginImmediate()` with `DefaultTimeout = 0`. `SQLITE_BUSY` → exit 3, nothing written.
4. `SolutionsRepository.EnsureByKey(key, name, path)` — identity setup, not a run fact.
5. `SolutionLoader.Open(solutionPath)`; for every project `GetCompilationAsync`; collect diagnostics
   with `Severity = Error`. Any → print them to stderr, `Rollback`, exit 2. Workspace failures of kind
   `Failure` → exit 1.
6. `CompiledInputs.Enumerate(solution)` once → `SourceDigest.Compute(inputs)` and
   `GitProvenance.Read(solutionDir, inputs)` (sha, dirty, repo root).
7. Extract into staging, one `SymbolWalker` pass per compilation: `ObservedSymbol`/`ObservedPart`
   lists; the eight `EdgeRule`s → `ObservedEdge` list. Namespaces observed by more than one
   compilation are merged by doc-comment id into one `ObservedSymbol` whose parts accumulate (spec
   FR-007); no other kind is merged. After the merge, refuse on any duplicate doc-comment id within
   the solution → exit 1 (spec edge case).
8. `CodeSymbolsRepository.ReadActive(solutionId)` → registry snapshot, plus
   `ReadRetiredByDocIds(solutionId, unmatched doc ids)` for (A′); `Reconciler.Reconcile(staged,
   snapshot, retired)` → `ReconciliationResult` (matched updates, reactivations, new inserts,
   retirements, candidates, counts).
9. Seam: `seams?.CorruptStagedCounts(counts)`. Seam: abort if `CODEMEM_TEST_ABORT_AT = AfterStaging`.
10. `CountAuditor.Audit(counts)` → residuals. Non-zero → `ExtractRunsRepository.InsertFailed(stamp,
    counts)`, `Commit`, exit 4.
11. Publish, in order, inside the open transaction: `InsertCompleted` (run id) → `RefreshMatched` →
    `Reactivate` → `InsertNew` (assigning ids; resolve container, project and edge target ids from
    doc ids) → `Retire` →
    `CodePartsRepository.ReplaceForSolution` → `CodeEdgesRepository.ReplaceForSolution` →
    `RenameCandidatesRepository.Insert` → `SolutionsRepository.RefreshLabels` / `SetFirstRun` if null.
    Seam: abort if `CODEMEM_TEST_ABORT_AT = DuringPublish` (between parts and edges).
12. `Commit`. Print the summary line (contracts/cli.md). Exit 0.

Any exception on steps 5–11 → `Rollback` (or the process dies and SQLite rolls back the journal) →
published tables unchanged (FR-030).

## Extraction Rules (Article IV — the deterministic rule per verb)

**Symbol selection**: every `ISymbol` reached by walking `Compilation.Assembly.GlobalNamespace`
recursively, per compilation, kept iff (a) `Not IsImplicitlyDeclared`, (b) kind maps to `SymbolKind`
(namespace only when it has ≥ 1 `NamespaceBlockSyntax` part — R2), (c)
`DeclaringSyntaxReferences.Length > 0` in a non-generated document. Plus one `project` symbol per
`Project` (R14). Accessors, lambdas, locals, parameters, type parameters are never rows. Every
non-namespace, non-project row records the compilation that declared it as `project_symbol_id` — a
compiler fact carried as a column, not a verb. **Namespaces are keyed by doc-comment id across
compilations**: `Sample.App` and `Sample.Lib` both yield `N:Sample`; they become one observed
namespace whose parts are every `Namespace` block from every project, primary = first in (path,
span) order. No other kind is merged — a type cannot be declared in two assemblies under one
doc-comment id, and `My.*` types have no source declaring reference and are already filtered.

**Parts**: one per declaring reference after R2 promotion; located by the part node's `Span` and
`GetLineSpan()` (line + 1, character + 1). `part_hash` per R4. Primary declaration = first part in
(path ordinal, start_offset) order (FR-010). `body_hash` per R4.

**Occurrence location** for edges: the span of the identifier that names the target (so I12 holds),
converted with the same `SourceLocation` builder used for symbols (one door).

| Verb | Source | Target (`OriginalDefinition`) | Occurrence span | Not written when |
|------|--------|-------------------------------|-----------------|-----------------|
| `part_of` | every row except `project` | `ContainingSymbol` | identifier of the primary declaration | container is the global namespace |
| `calls` | enclosing row symbol (FR-016) of an `InvocationExpressionSyntax`, `ObjectCreationExpressionSyntax`, or `MemberAccessExpressionSyntax` | `GetSymbolInfo(node).Symbol` (method, constructor, property, event, field); implicit ctor → containing type | the name token of the invoked member (`MemberAccess.Name`, `IdentifierName`, or the type name of `New`) | `Symbol Is Nothing` or `CandidateSymbols.Length > 0`; target is a local/parameter; occurrence inside a `Handles`/`Implements` clause (those are other verbs) |
| `uses` | the declaring row symbol; for a local `As` clause, the enclosing row symbol | `GetTypeInfo(typeSyntax).Type` for each named type in the `AsClause` of parameters, return, fields, properties, events, and `LocalDeclarationStatement` declarators; one edge per named type incl. generic arguments | the named type syntax span | type is an error type, type parameter, or `Object` inferred (no `As`) |
| `implements` | type → each `TypeSyntax` in `ImplementsStatementSyntax`; member → each name in `ImplementsClauseSyntax` | bound interface / interface member | the name span | unbound |
| `extends` | type with an `InheritsStatementSyntax` | bound base type | the base type name span | unbound |
| `imports` | each top-level type row declared in a file | each namespace bound by a `SimpleImportsClauseSyntax` (alias and XML imports excluded) in that file | the imported name span | clause binds to a type, not a namespace; project-level imports (no syntax) |
| `depends_on` | project row | project row of each `ProjectReference` | first occurrence of the referenced project file name in the `.vbproj` text (R13); offset 0/line 1 if absent | never (all project refs are written) |
| `handles` | (a) handler method row, per `HandlesClauseItemSyntax`; (b) the method bound by the `AddressOf` operand of an `AddHandlerStatementSyntax` | the bound event; `via_symbol_id` = bound `WithEvents` member row for (a) when source-declared, else null | (a) the event name token of the item; (b) the event name token of the event operand | event or (for b) method does not bind, or the delegate operand is not `AddressOf` |

**Determinism guards**: all ordering is ordinal; staging lists are sorted by (path, start_offset,
verb, target doc id) before publication so insert order — and therefore autoincrement ids within one
run — is stable (ids are excluded from I2 anyway, but stable order keeps diffs readable).

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| 4th project `CodeMem.Extraction` (Article I "adding a project requires written justification") | Article I also says executables only wire. Roslyn walking is ~15 classes of real logic that cannot live in the executable. | Putting it in `CodeMem.Core` makes Core depend on Roslyn (~50 MB of assemblies) and drags the compiler into every future consumer, including the read-only MCP server the constitution names. Putting it in `CodeMem.Extractor` puts logic in the executable, violating Article I directly. |
| `RunSeams` interface/class with one production call site (Rule of Three) | I8 and I9 require a test seam reachable through the production entry point (Article XIII). | A `#If DEBUG` block is a second door and unreachable from a Release-built production route. An environment variable alone works for the abort seam (and is used), but count corruption needs an in-process hook to mutate a staged object. |
| `EdgeRule` interface | Eight implementations — three-call-site rule satisfied (8 > 3). Listed only because it is an interface. | n/a |

## Carry-Forwards (decisions the spec hands to this plan, now fixed)

- Fixture loaded once per collection (FR-036, SC-010): `ICollectionFixture(Of FixtureSolution)`;
  mutation tests use `FixtureCopy`.
- Test framework, hash algorithm, lock mechanism, digest concatenation, extractor-version source: R9,
  R4, R6, R5, R12.
- DDL: [contracts/schema.sql](contracts/schema.sql) (spec Key Entities said "propose the DDL in the plan").
- In-memory staging (spec Q5).
- `Project:<name>` synthetic id and its empty-input hash (R14).

## Carve-Out Register

| Item | Kind | Armed by | Behaviour when unarmed |
|------|------|----------|------------------------|
| `AcceptanceRunner` | Skip-armed runner (FR-039) | `CODEMEM_ACCEPT_SOLUTION=<path>` | Reported as **Skipped** via `SkippableFact`; never passes or fails silently |
| `CODEMEM_TEST_ABORT_AT` | Test-only process-abort seam (I9) | environment variable `AfterStaging` / `DuringPublish` | Inert; labelled test-only in `--help` |
| `RunSeams.CorruptStagedCounts` | Test-only in-process seam (I8) | passing a `RunSeams` to `Execute` | `Main` passes `Nothing`; no effect |

## Open Items for the Architect (flagged, not decided here)

Plan review of 2026-09-10 resolved three of the four original items (symbol → project relation, now
the `project_symbol_id` column; Article VI (C) wording, removed in v1.2.0; sibling declarator
identifiers, now excluded — R4). One stands:

1. **`depends_on` span fallback** (R13) is the one place an edge can carry offset 0 / line 1 when the
   reference is injected by a props file. Outside the fixture; I12 cannot see it. Stands as recorded.

## Phase 0 / Phase 1 outputs

- [research.md](research.md) — R1–R14, all unknowns resolved; no NEEDS CLARIFICATION remains.
- [data-model.md](data-model.md) — entities, reconciliation algorithm, counts, staging records.
- [contracts/schema.sql](contracts/schema.sql), [contracts/cli.md](contracts/cli.md).
- [quickstart.md](quickstart.md).

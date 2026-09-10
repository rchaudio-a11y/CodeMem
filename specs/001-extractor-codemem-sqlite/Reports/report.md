# CodeMem Codebase Analysis

**Analysis date:** 2026-09-09  
**Scope:** Production projects, schema, extraction rules, reconciliation, CLI, tests, specifications, and current constitution  
**Method:** Static inspection plus local Release build, full test execution, and NuGet vulnerability audit

## Executive Summary

CodeMem is a compact, carefully structured VB.NET extractor with unusually strong tests around its core promises. The dependency direction is clean, the console application only wires components, SQLite publication is atomic for an existing map, and the tests exercise real Roslyn compilations and real SQLite files rather than mocks.

The implemented Stage A feature is healthy on its committed fixture: the Release build succeeds and all armed tests pass. However, the code should not yet be described as production-ready for arbitrary solutions. Four issues affect the trust model directly:

1. Roslyn documentation-comment IDs are scoped to a compilation, but CodeMem treats them as solution-wide identities. Valid multi-project solutions can therefore be rejected when two projects declare the same fully qualified symbol.
2. A type can change kind while retaining its documentation-comment ID, but reconciliation does not update the stored kind. That can silently leave an incorrect compiler fact in the map.
3. The source digest omits effective build inputs such as imported MSBuild properties, SDK identity, analyzer/source-generator inputs, and resolved references. Equal digests therefore do not always imply equal compilations or facts.
4. The current dependency graph contains two transitive packages reported by NuGet with High-severity advisories.

**Overall assessment:** strong engineering foundation and strong fixture-level correctness, with schema-level identity and provenance work required before broad production use.

## Validation Snapshot

Commands were run from the repository root on 2026-09-09.

| Check | Result |
|---|---|
| `dotnet test .\CodeMem.sln --configuration Release --nologo` | Build succeeded; 38 tests discovered; 37 passed; 0 failed; 1 skipped |
| Test duration | 99.3 seconds; 104.1 seconds including build |
| Skipped test | `AcceptanceRunner.ReportOnTheNamedSolution`; `CODEMEM_ACCEPT_SOLUTION` was not set |
| `dotnet list .\CodeMem.sln package --vulnerable --include-transitive` | Two High-severity transitive vulnerabilities reported |
| Worktree before this report | Clean |

The green suite is meaningful, but it proves the committed fixture and named invariants. It does not cover all valid Roslyn solution shapes or all effective build inputs.

## Architecture

The solution contains four projects with a sound one-way dependency chain:

```text
CodeMem.Extractor  ->  CodeMem.Extraction  ->  CodeMem.Core
        ^                    ^                     ^
        +--------------------+---------------------+
                         CodeMem.Tests
```

| Project | Responsibility | Assessment |
|---|---|---|
| `CodeMem.Core` | Schema, records, repositories, reconciliation, hashing | Cohesive and Roslyn-free; suitable for reuse by a future reader/MCP layer |
| `CodeMem.Extraction` | MSBuildWorkspace loading, symbol/edge extraction, provenance, run orchestration | Clear ownership; most product complexity correctly lives here |
| `CodeMem.Extractor` | CLI parsing and process entry point | Thin wiring layer as intended |
| `CodeMem.Tests` | Fixture integration tests, invariants, refusal tests, review gates | Broad and behavior-focused; no Roslyn or SQLite mocks |

The primary run path is [ExtractionRun.vb](src/CodeMem.Extraction/Run/ExtractionRun.vb#L20-L217):

1. Resolve paths and solution key.
2. Open or create the map.
3. Acquire a `BEGIN IMMEDIATE` writer transaction.
4. Ensure the solution identity row.
5. Load and compile every project through Roslyn.
6. Compute source and Git provenance.
7. Stage symbols, parts, and eight edge verbs in memory.
8. Reconcile against the active symbol registry.
9. Audit independent residual counts.
10. Publish the run, registry changes, parts, edges, and rename candidates in one transaction.
11. Commit and print one summary line.

## What Is Done Well

### Transactional publication

[MapDatabase.vb](src/CodeMem.Core/Repositories/MapDatabase.vb#L60-L101) uses `BEGIN IMMEDIATE` with a native zero busy timeout. Publication for an existing map remains inside that transaction, and the abort tests exercise the real executable. This provides a credible old-snapshot-or-new-snapshot guarantee for the tested publication path.

### Reconciliation accounting

[Reconciler.vb](src/CodeMem.Core/Reconciliation/Reconciler.vb#L18-L108) separates matching, reactivation, insertion, retirement, and candidate generation. `CountAuditor` independently derives both residuals. This is a good defense against silent partial reconciliation.

### Evidence and deterministic ordering

Paths and spans are represented explicitly, part hashes omit trivia, multi-part symbols are ordered, and staged edges are deduplicated and sorted before insertion. [I02_DeterminismTests.vb](tests/CodeMem.Tests/Invariants/I02_DeterminismTests.vb#L14-L42) correctly compares canonical logical facts rather than timestamps and surrogate IDs.

### Integration-oriented tests

The suite covers compilation refusal, stable identity on unchanged runs, rename candidates, reactivation, designer partial changes, evidence spans, atomic aborts, lock refusal, multi-solution isolation, map identity, schema constraints, and source-policy gates. The tests use a compiled two-project fixture and real SQLite files.

### Focused domain model

The database distinguishes durable symbol registry rows from replaceable observation rows. External edge targets retain a documentation-comment ID even when no internal symbol row exists. The eight edge rules are explicit and compiler-bound rather than inferred heuristically.

## Findings

### F1 - High: Compilation-scoped symbol IDs are treated as solution-wide IDs

**Status:** Confirmed by implementation inspection  
**Affected area:** Symbol identity, edge resolution, schema  
**Known limit (2026-09-10):** out of scope for fixpack 002 (`specs/002-stage-a-fixpack/spec.md`, Out of Scope); stays open.

`ISymbol.GetDocumentationCommentId()` identifies a symbol within a compilation, not uniquely across every project in a solution. CodeMem stages every project's IDs into one solution-wide set in [ExtractionRun.vb](src/CodeMem.Extraction/Run/ExtractionRun.vb#L122-L152). Namespace duplicates are deliberately merged first; the uniform guard in [Reconciler.vb](src/CodeMem.Core/Reconciliation/Reconciler.vb#L19-L30) rejects any duplicates that remain. The schema then enforces one active `(solution_id, doc_comment_id)` row in [SchemaRepository.vb](src/CodeMem.Core/Schema/SchemaRepository.vb#L67-L94).

A valid solution can contain `ProjectA.Acme.Widget` and `ProjectB.Acme.Widget` as separate types with the same `T:Acme.Widget` documentation ID. CodeMem will refuse that solution. Synthetic project IDs are also based only on the project file's base name in [ProjectSymbols.vb](src/CodeMem.Extraction/Symbols/ProjectSymbols.vb#L14-L21), so two projects in different directories that share a filename have the same collision class.

This cannot be fixed by merely removing the duplicate guard: the `doc id -> row id` dictionaries used for containers and edges would then bind facts to an arbitrary project row.

**Recommendation:** Introduce a schema-v2 identity key that includes compilation/project identity for project-owned symbols, while retaining an explicit canonicalization rule for namespaces. Edge staging must carry target compilation or assembly identity in addition to the documentation ID. Add a fixture with two projects declaring the same fully qualified type and two same-named project files.

### F2 - High: Type-kind changes can leave a stale fact in `code_symbols`

**Status:** Confirmed by implementation inspection  
**Affected area:** Reconciliation correctness

Reconciliation matches active rows only by documentation-comment ID in [Reconciler.vb](src/CodeMem.Core/Reconciliation/Reconciler.vb#L49-L64). A Roslyn type documentation ID starts with `T:` and does not encode whether the declaration is a class, structure, interface, module, enum, or delegate. Changing a declaration kind can therefore retain the same ID.

The matched-row update in [CodeSymbolsRepository.vb](src/CodeMem.Core/Repositories/CodeSymbolsRepository.vb#L86-L102) refreshes name, ownership, location, hash, and last-seen run, but never updates `kind`. A successful class-to-structure change can consequently leave `kind = 'class'` even though the current compiler fact is `structure`.

**Recommendation:** Decide whether a kind change preserves logical identity. Either include kind in the match key and retire/mint on change, or update kind as part of a match. Add an integration test that changes a fixture type through at least two `T:` kinds and verifies the resulting registry row.

### F3 - High: Two vulnerable transitive packages are present

**Status:** Verified by `dotnet list package --vulnerable --include-transitive`  
**Affected area:** Dependency security

NuGet reported:

| Package | Resolved version | Severity | Advisory |
|---|---:|---|---|
| `Microsoft.Build.Tasks.Core` | 17.7.2 | High | GHSA-h4j7-5rxr-p4wc |
| `System.Formats.Asn1` | 7.0.0 | High | GHSA-447r-wph3-92pm |

Both arrive through `Microsoft.CodeAnalysis.Workspaces.MSBuild` 4.14.0, declared in [CodeMem.Extraction.vbproj](src/CodeMem.Extraction/CodeMem.Extraction.vbproj#L16-L20). The second package is reached through the MSBuild cryptography dependency chain. This analysis did not establish whether CodeMem exercises each vulnerable operation, so exploitability remains unassessed; the vulnerable components are nevertheless shipped in the runtime dependency graph.

**Recommendation:** Test the newest compatible Roslyn/MSBuild workspace package first. If the current Roslyn pin must remain, evaluate explicit safe-version overrides for the affected transitive packages and rerun the full fixture and acceptance suites. Add the vulnerability command as a CI gate.

### F4 - High: The run digest does not identify the effective compilation

**Status:** Confirmed design limitation  
**Affected area:** Provenance and determinism  
**Known limit (2026-09-10):** fixpack 002 delivered the cheap half only (the four well-known build files walked upward, FR-107, and the `sdk_version` stamp, FR-109) and reworded FR-005 (FR-108); the full evaluated manifest (imported props/targets, analyzers, reference identities) is out of scope there and stays open.

[CompiledInputs.vb](src/CodeMem.Extraction/Workspace/CompiledInputs.vb#L19-L60) hashes Roslyn source documents, project files, and the opened solution file. [SourceDigest.vb](src/CodeMem.Extraction/Workspace/SourceDigest.vb#L15-L31) faithfully hashes that list, but the list omits inputs that can change compiler facts:

- `Directory.Build.props` and `Directory.Build.targets`
- imported `.props` and `.targets`
- `Directory.Packages.props`, resolved package assets, and metadata-reference identities
- SDK and reference-pack versions; the repository has no `global.json`
- analyzer configs, source-generator assemblies, and generator options
- environment/global properties other than the explicitly supplied configuration/framework

For example, changing an imported `RootNamespace` or conditional compilation constant can alter documentation IDs and edges without changing the recorded digest. Files can also change while a run is in progress; the database lock does not provide a consistent source snapshot.

This means the digest is deterministic over its selected files, but it is not a complete fingerprint of the compilation that produced the map.

**Recommendation:** Either narrow the documented claim to a "selected source-input digest" or capture an evaluated build manifest. At minimum stamp the SDK/MSBuild version, all evaluated project properties that affect compilation, normalized metadata-reference identities, analyzer/generator identities, and imported build files. Detect source changes between load and publication or run against an immutable checkout.

### F5 - Medium: The SQLite connection string is assembled from an unescaped path

**Status:** Confirmed by implementation inspection  
**Affected area:** Database path correctness

[MapDatabase.vb](src/CodeMem.Core/Repositories/MapDatabase.vb#L23-L28) builds the connection string with:

```vb
"Data Source=" & path & ";Pooling=False"
```

A legal path containing a semicolon is interpreted as connection-string syntax rather than solely as a filename. This can open a different path or apply unintended connection options, contrary to the rule that the requested map is the only database opened.

**Recommendation:** Construct the string with `SqliteConnectionStringBuilder`, assigning `DataSource` and `Pooling` as typed properties. Add a test using a temporary directory or filename containing `;`.

### F6 - Medium: Fresh-map initialization is outside the protected transaction

**Status:** Confirmed by implementation inspection  
**Affected area:** First-run crash recovery and concurrency

When a file does not exist, [MapDatabase.OpenOrCreate](src/CodeMem.Core/Repositories/MapDatabase.vb#L23-L47) creates the schema and identity row before `ExtractionRun` calls `BeginImmediate`. The atomic-abort test starts from an already initialized map, so it does not exercise this path.

A process interruption during schema creation can leave a partial file that is no longer considered fresh and cannot self-recover. Two processes racing to create the same new path can also pass through file existence/open/schema setup before the extractor lock has established single-writer ownership.

**Recommendation:** Build a fresh map in a sibling temporary file, validate it, and atomically rename it into place, or make initialization a recoverable transaction with an explicit incomplete marker and creation lock. Add first-run abort and concurrent-first-open tests.

### F7 - Medium: A solution-level target-framework stamp can be wrong or incomplete

**Status:** Confirmed by implementation inspection  
**Affected area:** Run provenance  
**Known limit (2026-09-10):** out of scope for fixpack 002 (`specs/002-stage-a-fixpack/spec.md`, Out of Scope); stays open.

[SolutionLoader.vb](src/CodeMem.Extraction/Workspace/SolutionLoader.vb#L91-L111) reads raw project XML and chooses the first literal `TargetFramework` or first `TargetFrameworks` value. It does not evaluate conditions, property expansion, or imported values. [ExtractionRun.vb](src/CodeMem.Extraction/Run/ExtractionRun.vb#L109-L123) then stamps only the first compiled project's framework when `--framework` is absent.

A mixed-framework solution cannot be represented accurately by that single value, and an evaluated framework can differ from the raw XML text.

**Recommendation:** Read the evaluated target framework from MSBuild project state and record it per project, or stamp a deterministic ordered list for the run. Add conditional, imported, and mixed-target fixture cases.

### F8 - Medium: A test-only environment variable can terminate the production executable

**Status:** Confirmed by implementation inspection  
**Affected area:** Operational reliability

[ExtractionRun.vb](src/CodeMem.Extraction/Run/ExtractionRun.vb#L256-L270) always reads `CODEMEM_TEST_ABORT_AT`; recognized values call `Environment.FailFast` on the normal production route. The behavior is labeled test-only, but it is active in the Release executable and exposed in CLI help.

An inherited or injected environment variable can therefore force a hard process termination. SQLite protects an existing published map, but availability and diagnostics are affected.

**Recommendation:** Put destructive process-abort testing behind a separate test host or require an explicit, non-default test mode with a per-run nonce. The normal shipped executable should not honor a crash instruction from ambient environment alone.

### F9 - Medium: Some preflight failures escape the exit-code contract

**Status:** Confirmed by implementation inspection  
**Affected area:** CLI reliability

Path normalization and initial database opening occur before the broad exception handler in [ExtractionRun.vb](src/CodeMem.Extraction/Run/ExtractionRun.vb#L20-L58). Exceptions such as invalid path syntax, `Path.GetFullPath` failures, or non-SQLite I/O/access failures during open can escape `Execute` instead of returning exit code 1 with a controlled message.

**Recommendation:** Put option/path normalization and map opening inside the top-level error boundary. Preserve the specific mappings for build errors, lock contention, schema mismatch, and residual mismatch, then map remaining expected input/I/O failures to exit 1.

### F10 - Low: Source-policy guards are useful but lexically bypassable

**Status:** Confirmed test limitation  
**Affected area:** Preventive test strength

[TripwireTests.vb](tests/CodeMem.Tests/Guards/TripwireTests.vb#L19-L55) and [SqlLocationGateTests.vb](tests/CodeMem.Tests/Guards/SqlLocationGateTests.vb#L17-L73) scan string literals with case-sensitive exact patterns. Lowercase SQL, split/concatenated strings, alternate whitespace, helper APIs, or a differently spelled connection constructor can bypass these gates.

The tests remain valuable tripwires and their positive controls avoid vacuous success, but they are not proofs that destructive SQL or extra connections are impossible.

**Recommendation:** Prefer tests against a centralized command factory or typed repository surface. As a minimum, normalize SQL and use case-insensitive token patterns. Keep runtime schema/FK tests as the stronger behavioral layer.

### F11 - Low: Real-solution and scale evidence is currently absent

**Status:** Observed in this analysis run  
**Affected area:** Operational confidence  
**Known limit (2026-09-10):** out of scope for fixpack 002 (`specs/002-stage-a-fixpack/spec.md`, Out of Scope); stays open. (The 002 quickstart records one real-solution upgrade of a GameRoom map copy: 2172 symbols, 6 s, exit 0; that is a validation of the upgrade, not the profiling this item asks for.)

The only acceptance runner was skipped because `CODEMEM_ACCEPT_SOLUTION` was unset. The implementation stages all symbols and edges in memory, traverses syntax trees separately for multiple edge rules, loads retired rows before filtering them in memory, and performs row-at-a-time inserts. These choices are reasonable for Stage A but have no current evidence on a large or heterogeneous solution.

**Recommendation:** Run the acceptance path against at least one representative production solution and record elapsed time, peak memory, project count, symbol/edge counts, unresolved-binding counts, and database size. Profile before adding abstractions or batching.

## Test Coverage Assessment

### Strongly covered

- Real Roslyn and SQLite integration
- Existing-map publication atomicity at two injected abort points
- Immediate writer-lock refusal
- Compile-error refusal without changing an existing map
- Stable IDs on unchanged extraction
- Rename candidate generation and ambiguity ranking
- Reactivation after a rename revert
- Multi-solution write scoping for distinct fixture identities
- Source evidence for the committed fixture
- Independent residual accounting
- Core schema constraints and code-policy tripwires

### Important missing cases

- Duplicate documentation IDs in different projects/assemblies
- Two projects with the same project filename
- Type-kind changes that retain a `T:` documentation ID
- Abort or concurrent access during first-time map initialization
- Imported/conditional MSBuild properties and central package configuration
- Source generators, analyzer configs, and changed reference packs
- Mixed and multi-target solutions
- Database paths containing connection-string delimiters
- Invalid/unreadable paths before database acquisition
- A current run against a real non-fixture solution

## Constitution and Specification Alignment

| Area | Assessment |
|---|---|
| Library-first structure | Strong alignment |
| Integration-first testing | Strong alignment |
| Compiler-bound facts | Strong for the eight specified verbs; generated/effective build inputs need a clearer boundary |
| Green-only publication | Strong for existing maps and compiler diagnostics |
| Durable registry | Strong no-delete behavior; constitution, spec, and code agree on documentation-ID matching, but its cross-project scope remains a model risk |
| Evidence on rows | Strong on fixture syntax; `depends_on` can intentionally fall back to a zero-length project-file location |
| Reconciled counts | Strong alignment |
| One file, multiple solutions | Demonstrated for distinct fixture identities; project-scoped symbol collisions remain unresolved |
| Determinism | Demonstrated for canonical facts over two fresh fixture maps; not guaranteed across unrecorded build environments |
| Governance version | Current constitution is v1.2.1 and records the amendments referenced by the feature artifacts |

## Recommended Order of Work

1. **Resolve identity semantics and migrate the schema.** Address project/assembly scope and type-kind transitions together because both affect durable IDs and edge resolution.
2. **Repair the provenance contract.** Decide whether the product promises source-file reproducibility or effective-compilation reproducibility, then stamp enough inputs to make that promise true.
3. **Remove the vulnerable dependency graph.** Upgrade or safely override the two reported packages and make vulnerability scanning repeatable in CI.
4. **Harden database startup.** Use a connection-string builder and make fresh-map initialization atomic and recoverable.
5. **Correct target-framework stamping and top-level error handling.** Add focused integration tests for each boundary.
6. **Remove or isolate the production crash seam.** Preserve process-abort coverage through a dedicated test route.
7. **Amend the identity contract with the schema change.** The constitution, specification, and code currently agree, so any project-scoped identity redesign must update all three together.
8. **Run representative acceptance and profiling.** Use evidence from a real solution before optimizing traversal or persistence.

## Final Assessment

CodeMem's strongest quality is that its safety properties are implemented, not merely described: the existing-map transaction boundary, residual accounting, source evidence, and fixture integration tests are all credible. The main weaknesses sit one level below those tests, in assumptions about identity scope and what constitutes the input to a compilation.

The project is in good shape for continued development and controlled fixture use. Before broad production adoption, the identity model, provenance stamp, vulnerable transitive dependencies, and fresh-map startup path should be treated as release-blocking work.
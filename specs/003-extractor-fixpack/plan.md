# Implementation Plan: CodeMem Fixpack 003 — Extractor Rules: Out-of-Repo Declarations and Bare-Name References

**Branch**: `003-extractor-fixpack` | **Date**: 2026-09-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `/specs/003-extractor-fixpack/spec.md`

**Governing document**: `.specify/memory/constitution.md` v1.2.1, unamended. Parent plans:
[../001-extractor-codemem-sqlite/plan.md](../001-extractor-codemem-sqlite/plan.md) and
[../002-stage-a-fixpack/plan.md](../002-stage-a-fixpack/plan.md); this plan changes the extractor in place
and lists only what changes.

**Ceremony**: STANDARD. No spike. **One stop, after this plan, before tasks** (§STOP 1): the Architect sees
rule 1's file-scoping predicate before anything runs against MemOS.

## Summary

Two extraction rules, no schema change. **Rule 1**: a run resolves one *scope root* — the repository working
directory it already writes to `solutions.repo_root`, or the solution directory when there is none — and a
declaring file outside it (a compiled document, or a project file) declares nothing: no row, no part, no edge
source. References to what it declared take the external-target shape the map already uses for framework
symbols. The duplicate-id refusal is untouched and gains its first production-route test. **Rule 2**: the
`calls` rule gains a fourth occurrence shape — a simple name the compiler binds to a field, property, event
or method that is a row of this run — located at the identifier, one per identifier, verb `calls`. Handles
wiring is untouched. The extractor version becomes 0.2.0. Acceptance is the live map: GameRoom and CodeMem
re-extracted (the 059 orphan baseline must move, by kind), then MemOS with `--solution-key MemOS` —
completing, or refusing on an in-repo pair, either recorded.

## Technical Context

**Language/Version**: VB.NET on .NET 8 (`net8.0`), unchanged. Option Strict/Explicit On, Infer Off.

**Primary Dependencies**: unchanged. Roslyn 4.14.0 (`SimpleNameSyntax`, `GetSymbolInfo`, `GetEnclosingSymbol`),
LibGit2Sharp 0.32.0 (already read once per run; the tests also use `Repository.Init`, `Commands.Stage`,
`Commit` — research R39), Microsoft.Data.Sqlite 8.0.31, xUnit 2.9.3. **No new package.** One version
number changes: `CodeMem.Extraction.vbproj` `<Version>` 0.1.0 → 0.2.0 (R38).

**Storage**: one SQLite file; schema version **2, unchanged**. No DDL, no migration.

**Testing**: xUnit, real SQLite, real compiled fixture, no mocks (Article III). New tests under
`tests/CodeMem.Tests/Fixpack003/`, one file per rule plus one for the refusal; each Red-first with the reason
recorded in the header (FR-219); every guard with a `' FIRE:` line. The committed fixture source is not
edited; every mutation is on a `FixtureCopy`.

**Target Platform**: Windows 11 developer workstation; the scope-root prefix test follows the platform's
case rule via `StringComparison.OrdinalIgnoreCase`, as the existing `obj/` rule does.

**Project Type**: CLI over class libraries, unchanged. No new project.

**Performance Goals**: SC-010 (fixture extraction under 60 s; ~2 s observed) still gates. Rule 2 adds one
`GetSymbolInfo` per simple name per in-scope tree (R40); the acceptance runner's elapsed time on GameRoom is
recorded before and after.

**Constraints**: no schema bump, no CLI argument, no constitution amendment (description); Article IX (the
extractor opens nothing but the map; `memos.sqlite` and `code_map_solutions` are never touched); Article
XII (one door for the scope decision and one rule per verb); Article XIV (retired rows stay).

**Scale/Scope**: three real solutions (GameRoom 2 172 active symbols, CodeMem 694, MemOS ten projects, not
yet mapped).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Article / Gate | Status | How this plan satisfies it |
|----------------|--------|----------------------------|
| I. Library-First | PASS | No new project. One new type, `SolutionScope` (Extraction/Workspace), one per file. `Program.vb` unchanged. |
| II. Test-First | PASS | Rule 1's tests go Red on the existing duplicate refusal (the fixture copy collides today); rule 2's go Red on missing edges. The in-repo refusal test cannot go Red first and is proven by its fire (§Test design). Every guard carries a FIRE line. |
| III. Integration-First | PASS | Real SQLite, real compiled fixture copies, one real repository created by a test; the binding facts come from a Roslyn probe, not a mock (R34). |
| IV. Compiler Fact Only | PASS | Scope is a path fact of the compiled document plus the repository the run already reads; every bare-name edge is `GetSymbolInfo`'s binding with no candidates. Both rules are written here (contracts/extraction-rules.md). The extractor version changes because the fact set for one digest changes (R38). |
| V. Green Only, Stamped | PASS | Unchanged order: an out-of-scope project's errors still refuse (FR-203); the stamp is written first; publication is one transaction. |
| VI. Reconcile, Never Truncate | PASS | Identity unchanged; the duplicate guard unchanged and now tested through the executable (FR-205); out-of-scope rows retire by step 4 and stay (FR-207); no candidate arises; no DELETE reaches `code_symbols` (I13 unchanged). |
| VII. Evidence on Every Row | PASS | Bare-name edges carry the identifier's path and span (FR-214); I12 covers them unchanged. |
| VIII. Counts That Reconcile | PASS | No count changes meaning; residuals asserted 0 · 0 in every new test and on the live runs (SC-206, SC-207). |
| IX. One File, Many Solutions, One Writer | PASS | Nothing but the map is opened; the MemOS registry binding is explicitly not done here (FR-218). |
| X. Absence Must Be Representable | PASS | No sentinel: an external or out-of-scope target is NULL with its doc id present; the scope root is not stored because both of its sources already are (data-model). |
| XI. Anti-Abstraction | PASS | `SolutionScope` has two call sites in hand (documents, project files) and is the one door the constitution's Article XII demands, not a Rule-of-Three abstraction; no new SQL. |
| XII. One Door for Every Rule | PASS | Scope: `SolutionScope.Contains` alone; consulted by `CompiledInputs.SourceTrees` and `ExtractionRun`. Verb `calls`: `CallsRule` alone gains the shape (no second rule class for one verb). Duplicate ids: `Reconciler` step 1 alone. |
| XIII. Production-Route Reachability | PASS | Every new fact enters through `ExtractionRun.Execute`; the refusal through the executable (`ExtractorProcess`). |
| XIV. Archive, Never Delete | PASS | No file retired; rows retire by flag. |
| XV. Compiled .NET, No Foreign Runtime | PASS | The orphan-baseline reproduction script is a dev-time scratch artefact outside the repository; the product gains no runtime. |
| Gate: Option settings in every touched project file | PASS | `CodeMem.Extraction.vbproj` changes one `<Version>` line; `ProjectFileGateTests` still scans it. |
| Gate: no SQL outside a named repository method | PASS | No new SQL anywhere; the new tests read through `MapQueries`. |
| Gate: header block + XML docs | PASS | Existing gate covers `SolutionScope.vb` and the three test files. |
| Gate: tripwire (I13) | PASS | No repository file changes. |
| Gate: residuals shown to block publication | PASS | Unchanged (I8); re-asserted on the new runs. |
| Gate: candidates never applied | PASS | Unchanged; SC-206 asserts none written. |
| Gate: three call sites for a new abstraction | PASS | None introduced beyond the Article XII door above. |

**Post-design re-check**: unchanged; no violations; no Complexity Tracking entries.

## Project Structure

### Documentation (this feature)

```text
specs/003-extractor-fixpack/
├── plan.md                    # This file
├── research.md                # R31–R40, verified on this machine (probe, live map, MemOS run)
├── data-model.md              # no schema change; scope root, edge shape, reconciliation effects
├── quickstart.md              # fixture validation, the live-map operator steps and their record tables
├── contracts/
│   ├── extraction-rules.md    # scope rule; the amended calls rule
│   └── cli.md                 # unchanged CLI; the refusal line; the three operator commands
├── checklists/requirements.md
└── tasks.md                   # /speckit-tasks output — after STOP 1
```

### Source Code (repository root) — files that change or appear

```text
src/
├── CodeMem.Extraction/
│   ├── CodeMem.Extraction.vbproj         # <Version>0.2.0</Version> (R38)
│   ├── Workspace/
│   │   ├── SolutionScope.vb              # NEW: Resolve(repoRoot, basePath); Root; IsRepository; Contains(fullPath) — the one door
│   │   └── CompiledInputs.vb             # SourceTrees(project, compilation, scope): + "and in scope"; Enumerate unchanged
│   ├── Edges/
│   │   └── CallsRule.vb                  # fourth shape: SimpleNameSyntax → CollectBareName
│   └── Run/
│       └── ExtractionRun.vb              # step 7: scope = SolutionScope.Resolve(git.RepoRoot, basePath); step 8: skip out-of-scope projects, scoped trees
tests/CodeMem.Tests/
├── Acceptance/AcceptanceRunner.vb        # SourceTrees call gains the scope (counts handles in scope)
└── Fixpack003/                           # NEW folder
    ├── R01_ScopeRootTests.vb             # (a) injected file, no repo; (b) repository above the solution; (c) project file out of scope; (d) re-extraction retires
    ├── R01_DuplicateRefusalTests.vb      # in-repo pair through the executable: exit 1, one line, both locations, no user table
    └── R02_BareNameTests.vb              # committed fixture occurrences; vbCrLf negative; two runs: no candidate, residuals 0
```

**Structure Decision**: unchanged four projects. Rule 1 lands where the tree-selection rule already lives
(`CompiledInputs`) with its predicate in one new type beside it; rule 2 lands in the class that owns the
verb.

## Run Order (revised; changes in bold)

Executed by `ExtractionRun.Execute(options, seams)`; steps 1–6 and 9–13 unchanged from 002.

7. Enumerate compiled inputs → digest; `GitProvenance.Read` → git facts; `SdkVersion.Resolve` → stamp
   (unchanged). **Then `scope = SolutionScope.Resolve(git.RepoRoot, basePath)`.**
8. **For each compiled project whose project file `scope.Contains`**: trees =
   `CompiledInputs.SourceTrees(project, compilation, scope)`; walk symbols; stage. Merge namespaces; **add a
   project row for each in-scope project only**; run the seven tree rules over the scoped trees (**`CallsRule`
   now emits the bare-name shape**); `PartOfRule` over the merged symbols; canonicalise (folds coincident
   occurrences). Out-of-scope projects: **no walk, no project row, no rules** — their diagnostics were already
   counted in step 6.

The order matters in one place: the scope needs `git.RepoRoot`, which step 7 already produces before staging.

## Rule 1 — design

**`SolutionScope`** (`CodeMem.Extraction.Workspace`, `Public Class`, one per file):

| Member | Contract |
|--------|----------|
| `Shared Function Resolve(repoRoot As String, basePath As String) As SolutionScope` | `repoRoot` non-empty → root = repoRoot, `IsRepository = True`; else root = basePath, `IsRepository = False`. Root normalised by `Path.GetFullPath` and given one trailing `Path.DirectorySeparatorChar`. |
| `ReadOnly Property Root As String` | the normalised root with trailing separator |
| `ReadOnly Property IsRepository As Boolean` | which source the root came from (for the summary of a test, not stored) |
| `Function Contains(fullPath As String) As Boolean` | `Path.GetFullPath(fullPath).StartsWith(Root, StringComparison.OrdinalIgnoreCase)` |

**`CompiledInputs.SourceTrees(project, compilation, scope)`**: the existing method gains the parameter; the
document filter becomes `IsSourceDocument(project, document) AndAlso scope.Contains(document.FilePath)`. The
2-argument overload is removed (one door, two callers: `ExtractionRun` and `AcceptanceRunner`).
`Enumerate` and `IsSourceDocument` are untouched (FR-206).

**`ExtractionRun`**: after step 7, `Dim scope As SolutionScope = SolutionScope.Resolve(git.RepoRoot, basePath)`;
in step 8 the two `For Each project In compiled` loops that stage symbols and add project rows skip a project
when `Not scope.Contains(project.Project.FilePath)`, and the rules loop runs over the same in-scope set
(`treesOf` and `perProject` only hold in-scope projects). Everything downstream (reconcile, audit, publish)
is unchanged.

**What this does to the live rows**: GameRoom's and CodeMem's two injected-program rows are not observed on
the next run → retired, counted, kept (FR-207). Their `part_of` edges and any other edge sourced from them
vanish with the wholesale replacement of `code_edges` for the solution (Article VI: observation tables are
replaced per run).

**Refusal unchanged**: `Reconciler` step 1 still throws `DuplicateDocCommentIdException` naming both primary
locations; `ExtractionRun.OneLine` still folds it to one stderr line; exit 1; the fresh-map creation rolls
back (002 FR-106). The new test drives it through the executable (FR-205, Article XIII).

## Rule 2 — design

`CallsRule.Collect` gains, before the three existing shapes, a branch for `SimpleNameSyntax`:

```text
CollectBareName(context, model, simple, sink):
  parent = simple.Parent
  if parent is MemberAccessExpressionSyntax and parent.Name is simple  → return   (member-access shape owns it)
  if parent is QualifiedNameSyntax                                     → return   (type position)
  if parent is AttributeSyntax                                         → return   (binds to a constructor)
  if InsideOtherVerbClause(simple)                                     → return   (Handles/Implements/Inherits/Imports)
  info = model.GetSymbolInfo(simple); if info.Symbol Is Nothing or info.CandidateSymbols.Length > 0 → return
  target = info.Symbol
  admit: IFieldSymbol; IPropertySymbol; IEventSymbol; IMethodSymbol with MethodKind Ordinary, DeclareMethod or ReducedExtension
  else → return                                                                  (locals, parameters, range variables, types, namespaces, type parameters, constructors)
  targetDocId = context.DocIdOf(target); if Not context.IsRow(targetDocId) → return   (rows only, FR-209)
  source = EnclosingSymbolResolver.RowSymbolAt(context, model, simple); if Nothing → return
  sink.Add(calls, source → targetDocId, via Nothing, location = context.LocationOf(simple.Identifier))
```

Facts the design rests on (R34): a parenthesis-less call is already an `InvocationExpressionSyntax`; the
identifier under an invocation binds to the resolved overload and folds with the invocation's own edge at the
same token (R35, FR-213); accessor bodies attribute to the property through `RowSymbolOf`'s
`AssociatedSymbol` walk (FR-016, unchanged); field initializers and parameter defaults attribute to the
field and the method respectively.

`InsideOtherVerbClause` is reused as is. No other rule changes. `HandlesRule` is untouched (FR-211).

**Verb**: `calls`, because the map already records `obj.Field`, `obj.Property`, `obj.Event` and
`AddressOf obj.Method` as `calls` (Stage A FR-017); one question, one verb.

## Test design

| File | Facts | Red (before code) | FIRE (after Green) |
|------|-------|-------------------|--------------------|
| `R01_ScopeRootTests` | (a) no repository: injected file under `<parent>/.nuget/packages/…` in both projects + a call to its function → exit 0, no row/part with that path, one `calls` edge with NULL target, residuals 0 · 0. (b) repository at `<parent>` with a commit: `../Shared/Extra.vb` type is a row, injected file (outside the repository, under the system temp) is not, `repo_root` = `<parent>`. (c) `Sample.Lib` moved to `<parent>/Outside/`, no repository: no `Project:Sample.Lib` row, `depends_on` and `extends` to it carry NULL targets, `N:Sample.Widgets` has the App part only. (d) run on a copy, then re-run after the injected file becomes out of scope → `symbols_retired = 2`, rows kept inactive, `rename_candidates = 0`. | (a) exit 1 duplicate id (today's behaviour); (b) injected module is a row / collision; (c) `Project:Sample.Lib` row present; (d) retirements 0 | root = `basePath` even when `repoRoot` is present → (b) red (SharedExtra missing); revert → green |
| `R01_DuplicateRefusalTests` | `Dup.vb` (`Public Class Dup`) in both projects; executable; fresh `--db`: exit 1, one stderr line containing `T:Sample.Dup`, `Sample.App/Dup.vb:1,14`, `Sample.Lib/Dup.vb:1,14`; stdout empty; `CountUserTables = 0` | **not Red** (the guard exists; recorded as the production-route proof, like `RefusalTests`) | `Reconciler` step 1 skips the throw → exit 0 → red; revert → green |
| `R02_BareNameTests` | committed fixture: the six expected occurrences (quickstart §4) at computed line/column, non-NULL targets, span text = name; `handles` count still 5; no edge sourced from an accessor; `vbCrLf` copy: 0 edges to `F:Microsoft.VisualBasic.Constants.vbCrLf`; two runs: run 2 `rename_candidates = 0`, `symbols_retired = 0`, residuals 0 · 0, candidate row count unchanged | zero of the six edges exist | admit fields only (drop properties) → `Timer1` facts red; revert → green |

**One existing test is expected to go Red, named here before any battery runs** (added after STOP 1, before
T001): `Fixpack/S02_SchemaUpgradeTests.VersionOneMapUpgradesInPlace` asserts `before("code_edges") =
after("code_edges")` over the Stage A version-1 fixture map (137 edges written by the Stage A extractor).
Rule 2 writes the fixture's bare-name occurrences too, so the re-extraction after the upgrade writes more
edges than the Stage A run did. `code_edges` is an observation table replaced wholesale per run; equality
across extractor versions was never the fact the test meant ("every table keeps its rows plus this run's own"
is a fact about identity and run tables). **Amendment, in place, after the Red is recorded**: the `code_edges`
assertion compares the upgraded map's edge count with the count a fresh map of the same fixture gets from the
same executable; `code_symbols`, `code_parts` and `rename_candidates` equality stand (the fixture has no
out-of-scope declaration). The header gains the dated line. Expected Red carrier: `Assert.Equal` on
`code_edges`, expected 137.

Every other Stage A and 002 test is expected to stay green without edits (no other test pins an edge count;
I12 reads all edges and validates the new spans; I3's `handles` count is unchanged). Any other Red is
unexpected and stops the work (§STOP 1, ruling 10).

A second existing test goes Red at the version bump, named here before it runs:
`Invariants/US1_FirstRunTests` asserts `run.ExtractorVersion = "0.1.0"`; FR-217 makes it `0.2.0`. Amended in
place to the new value with a dated header line (the fact "the run stamps the assembly version" is unchanged).
Expected Red carrier: `Assert.Equal("0.1.0", …)`.

**Expected Reds, named before they happen**: the three rows above, S02 (1) at rule 2, and US1_FirstRunTests at
the version bump. Any other failure is unexpected.

## Operator steps (after implementation; the PM acceptance)

Exactly as [quickstart.md](quickstart.md) "Live map": copy the map; record the baseline; GameRoom, CodeMem,
MemOS in that order with explicit keys; re-run the orphan statement; read `_balance`'s references; fill the
record tables. The MemOS outcome is recorded verbatim, whichever it is; on (b) the work stops and the
Article VI (A) trigger is reported, not opened.

## Known limits (recorded for the extractor; carried forward)

- **Bare names bound to external members are not recorded** (FR-209; STOP 1 ruling: a stated limit, not a
  property). `vbCrLf`, `vbTab`, `Math.PI` through an `Imports`, or any member of a referenced assembly named
  bare leaves no edge, whereas the same member through member access leaves an external-target edge. A
  consumer reading a symbol's outbound `calls` therefore sees external uses made through member access only.
  **Carried to MemOS 060**: the `codemem_references` description update must state this hole by its
  mechanism, beside the `AddHandler … AddressOf` one (058 FR-017a's standard).
- Escaped identifiers (`[Stop]`) carry the brackets in the occurrence span (Stage A behaviour, unchanged).
- The scope root is not a literal `.nuget/packages/` rule: a repository-local package cache is mapped and, with
  two test projects, refused by name (spec Edge Cases).

## STOP 1 — RULED 2026-09-13 (at plan, before tasks)

**Rulings** (recorded in the spec, Clarifications, *Session 2026-09-13 — STOP 1 rulings*): the predicate is
accepted as stated with the addition that the prefix comparison is on resolved full paths, case-insensitive
and separator-normalised, and that the SDK-file Red exercises the relative form the 2026-09-12 refusal
printed; Q2 and Q3 as answered; row 5 (rows only) is accepted **as a stated limit, not a property** and is
recorded above and carried to MemOS 060's description update; the rest stand. Proceed: tasks, Red-first, the
suite, then the quickstart **against a copy of the map** — GameRoom, CodeMem, then MemOS with its key; a
refusal on an in-repo pair stops the work.

The decisions as they were put (each was one line to reverse):

| # | Decision | Where | Alternative |
|---|----------|-------|-------------|
| 1 | **Rule 1 predicate: path prefix on the declaring file's full path against the scope root** (not "not a compile item": the injected file *is* one, R32) | spec Q1, FR-201–FR-203 | none workable |
| 2 | **Scope root = `solutions.repo_root`'s value when a repository with a commit exists; otherwise the solution directory** | spec Q1, FR-201, R33 | no scoping without a repository (leaves non-repository solutions unmappable and the rule untestable on the fixture) |
| 3 | **Project files are declaring files too**: a project outside the scope root contributes nothing; references to it are external | FR-203 | keep project rows for every project in the solution file (then a linked in-scope document of an out-of-scope project would need a project row it cannot have under the CHECK) |
| 4 | **Digest unchanged** — provenance covers out-of-scope inputs; identity does not | spec Q6, FR-206 | scope the digest too (changes every solution's digest; hides a compiled input) |
| 5 | **Rule 2 targets rows only** — no edge for `vbCrLf` and friends | spec Q4, FR-209 | record externals with NULL target, as member access does (one edge per `vbCrLf`; the description's invariant would then need "when the target is a row") |
| 6 | **Bare events included** (`RaiseEvent X`, `AddHandler X`) | spec Q5, FR-208 | strike: fields, properties, methods only |
| 7 | **One occurrence per identifier, verb `calls`, no read/write or accessor split** | spec Q2, FR-210 | none without a schema change |
| 8 | **WithEvents declarations write nothing; bare use of the member does** | spec Q3, FR-212 | none |
| 9 | **Extractor version 0.2.0** | spec Q7, FR-217, R38 | leave 0.1.0 (the same digest and version would then yield two fact sets) |
| 10 | **No second stop**: each Red is recorded in the test header before its implementation; the work stops only for a Red not named in §Test design | this plan | a Red report before each implementation, as 001 did |

Everything else is Stage A and 002 unchanged.

## Complexity Tracking

No constitution violations; nothing to justify.

## Implementation record (2026-09-13)

**Environment**: Windows 11; `dotnet --version` 10.0.401; runtime 8.0.31 runs the net8.0 projects. Starting point
commit `4efb7f9` (002 committed), 59 passed / 1 skipped / 1 m 54 s, 0 warnings.

**Order followed**: tasks.md T001–T019 in order, with T013 folded into T019 (see deviations). The three test files
were written and run on the untouched tree first (T005); every Red was the one §Test design names, and the
relative-form refusal line the STOP 1 ruling asked for was observed verbatim (`../.nuget/packages/fake.test.sdk/1.0.0/build/net8.0/Fake.Test.Sdk.Program.vb:4,8 and …:4,8`).
Rule 1 landed (T006–T009), its battery went green on first run (5 of 5), both fires recorded; the duplicate
refusal's fire (T014) went red on the schema's UNIQUE index instead of the named line — the two doors are
distinct, as Article XII wants. Rule 2 landed as one method in `CallsRule` (T015); its battery went green on
first run, I3 and I12 unchanged, S02 (1) red at 137 vs 144 exactly as named and amended; both fires recorded
(T017). Version 0.2.0 (T018) turned `US1_FirstRunTests` red as named; amended. Full suite (T019): 69 total,
68 passed, 1 skipped, 0 failed, 2.17 min.

**Review pass (T025), v1.2.1 Review Gates, every touched file**:

- Option settings: `CodeMem.Extraction.vbproj` changed one `<Version>` line and gained a comment;
  `ProjectFileGateTests` green.
- SQL location: no new SQL; `SqlLocationGateTests` green (the new tests read through `MapQueries`).
- Header block and XML docs: `FileHeaderGateTests` green over `SolutionScope.vb`, the three new test files and
  every edited file.
- Tripwire (I13): green; no repository file changed.
- FIRE lines: `R01_ScopeRootTests` ×2 (T011, T012), `R01_DuplicateRefusalTests` ×1 (T014),
  `R02_BareNameTests` ×2 (T017 i, ii); the two amended prior tests carry their dated lines.
- One door: scope is decided in `SolutionScope.Contains` alone, consulted by `CompiledInputs.SourceTrees` and
  `ExtractionRun` (project files); `calls` has one rule class; the duplicate guard is `Reconciler` step 1 alone.
- Counts: both residuals asserted 0 · 0 in every new fact and read on every Phase 5 run.
- Candidates: `rename_candidates` asserted 0 and unchanged (R01 (d), R02 (3)).
- No new abstraction beyond the Article XII door; no new project; no CLI argument; no schema change; no
  constitution amendment.

**Deviations from the design documents, with reasons**:

1. **The Red battery's filter.** T005 named `FullyQualifiedName~Fixpack003`; the folder is not part of the test
   namespace (`CodeMem.Tests`), so the first run matched only S02. Re-run with `~R01_|~R02_`; tasks.md corrected.
2. **A fifth R01 fact, on the door itself.** The T012 fire (ordinal, unnormalised comparison) did not turn the four
   fixture facts red: the paths they build are already case-consistent, and `Path.GetFullPath` normalises
   separators on Windows. The ruling's addition needed a guard shown to fire, so
   `PrefixComparisonIsResolvedCaseInsensitiveAndSeparatorNormalised` was added (mixed case, forward slashes,
   `..` segments, a same-prefix sibling, the no-repository fallback); the same injection turns it red. It is not
   Red-first (written against the finished rule) and its header says so.
3. **T013 folded into T019.** After rule 1 alone, the R01 battery, S02 and the refusal fact were run rather than the
   whole suite; the whole suite ran once on the finished tree.
4. **S02 (1) amended** as named: `code_edges` compared with a fresh map from the same executable; the other three
   table equalities stand.
5. **`US1_FirstRunTests` amended** as named: the version literal.
6. **`AcceptanceRunner` resolves the scope** the way the run does (git facts over the enumerated inputs) so
   `handles_in_source` counts the same trees; no task named the git read, the plan did.
7. **The T014 fire's carrier** was the message assertion, not the exit code: with the guard skipped the run reached
   publication and `ix_code_symbols_solution_id_doc_comment_id_active` refused with SQLite error 19 → still exit 1,
   but "database error: UNIQUE …" instead of the named pair. Recorded as such; the guard is what names the pair.

**Known limit, carried** (STOP 1): bare names bound to external members write nothing; recorded above under
"Known limits" and owed to MemOS 060's `codemem_references` description.

**Phase 5 (the copy of the map, 2026-09-13; T020–T024)**: all three runs exit 0 with residuals 0 · 0 and 0
candidates. GameRoom retired exactly the two injected rows (digest identical to runs 1–2, version 0.2.0); CodeMem
retired those two plus the old two-argument `SourceTrees` and minted this feature's 34 symbols; **MemOS completed
— outcome (a)** — and joined the copy as solution 3 with 14 713 symbols across its 10 projects, no `../` row or
edge anywhere. Orphans moved 306 → 279 (GameRoom) and 125 → 136 (CodeMem; the rise is this feature's own test
classes and facts, +12, against −1 module and −1 `Main`), each delta accounted for by kind in the quickstart.
`_balance` went from 0 to 22 occurrences; `StepCornered` from 0 to 1. The live files' hashes are unchanged.

**Live map (2026-09-13, after commit `f648846`, at the Architect's request)**: backup
`C:\_DB\codemem.pre-003.2026-09-13.sqlite` taken, extractor rebuilt from the commit, the three runs made against
`C:\_DB\codemem.sqlite` in the same order with the same counts as the copy (quickstart "Live map record"); MemOS
is solution 3, run 6. `memos.sqlite` untouched (hash equal before and after).

**Not done here**: the MemOS-side registry binding of solution 3 (`code_map_solutions` row 1 →
`codemem_solution_id = 3`), which is MemOS's write, not the extractor's.

## Phase 0 / Phase 1 outputs

- [research.md](research.md) — R31–R40: the collision reproduced, the binding probe, the orphan baseline
  reproduced to the row, the decisions with alternatives.
- [data-model.md](data-model.md) — no DDL change; scope root, edge shape, reconciliation effects.
- [contracts/extraction-rules.md](contracts/extraction-rules.md), [contracts/cli.md](contracts/cli.md).
- [quickstart.md](quickstart.md) — fixture validation and the live-map record tables.

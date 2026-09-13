---

description: "Task list for CodeMem Fixpack 003 — extractor rules: out-of-repo declarations and bare-name references"
---

# Tasks: CodeMem Fixpack 003 — Extractor Rules

**Input**: Design documents from `/specs/003-extractor-fixpack/`

**Prerequisites**: plan.md (STOP 1 ruled 2026-09-13), spec.md (with the STOP 1 rulings), research.md
(R31–R40), data-model.md, contracts/extraction-rules.md, contracts/cli.md, quickstart.md — all present.
Constitution v1.2.1 governs, unamended. 002's code and its 60 tests are the starting point (commit `4efb7f9`;
59 passed, 1 skipped, 1 m 54 s measured 2026-09-13).

**Tests**: REQUIRED. FR-219 and Article II make every behaviour change a Red-first test against real SQLite
and the real compiled fixture: write → run → confirm Red *for the stated reason* → record it in the test file
header → implement → confirm Green → record. Every guard carries a `' FIRE:` line. Tests for this feature live
in `tests/CodeMem.Tests/Fixpack003/`, `<Collection("Fixture")>`. **No second stop** (ruling 10): each Red is
recorded before its implementation; the work stops only for a Red not named in plan §Test design.

**Organization**: Phase 1 setup (nothing to build; baselines are already measured), Phase 2 the Red batteries
for both rules (written and run before any source change, in one battery so the S02 Red is observed on the
untouched tree too), Phase 3 rule 1 (US1), Phase 4 rule 2 (US2), Phase 5 the live-map acceptance on a copy
(US3), Phase 6 polish.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1–US3 from spec.md
- Every path is repository-relative

## Standing rules for every task that creates or edits a `.vb` file

- Header block first (`' File:`, `' Project:`, `' Description:`, `' Author: RCH Automation LLC`, `' Created: 2026-09-13`); XML docs on every public declaration; one type per file; no SQL outside `src/CodeMem.Core/Repositories/*.vb`, `src/CodeMem.Core/Schema/*.vb` and `tests/CodeMem.Tests/Support/MapQueries.vb`.
- Red/Green/FIRE lines go in the test file header; the FIRE injection is reverted before the task closes.
- Stage A and 002 tests that this feature changes are edited in place (never copied); their headers gain a dated line saying what changed and why.
- The committed fixture source under `tests/CodeMem.Tests/Fixtures/Sample/` is never edited; every mutation runs on a `FixtureCopy`.

---

## Phase 1: Setup

**Purpose**: Confirm the starting point the Reds are measured against.

- [X] T001 Confirm the working tree is at `4efb7f9` with only `specs/003-extractor-fixpack/` and `.agents/` untracked, `dotnet build CodeMem.sln` is 0 errors / 0 warnings, and the baseline of 59 passed / 1 skipped stands (already measured 2026-09-13; re-run only if the tree moved). Record the confirmation in `specs/003-extractor-fixpack/quickstart.md` under "Build and test"

---

## Phase 2: Foundational — the Red batteries (before any source change)

**Purpose**: Every new fact fails on the untouched tree for the reason the plan names; the one expected Red in
an existing test (S02) is observed on the same tree.

**⚠️ CRITICAL**: No source change until T005 has recorded every Red.

- [X] T002 [P] Create `tests/CodeMem.Tests/Fixpack003/R01_ScopeRootTests.vb` with four facts on `FixtureCopy` (plan §Test design): (a) `InjectedFileOutsideTheSolutionIsNotASymbol` — write `<parent>/.nuget/packages/fake.test.sdk/1.0.0/build/net8.0/Fake.Test.Sdk.Program.vb` (a `Namespace Global` module `__FakeTestSdkProgram` with `Sub Main(args As String())` and `Public Function Marker() As Integer`), add `<Compile Include="..\..\.nuget\packages\fake.test.sdk\1.0.0\build\net8.0\Fake.Test.Sdk.Program.vb" />` (the relative form, per the STOP 1 ruling) to both project files via `FixtureCopy.Replace` on `</Project>`, make `Consumer.Build` call `__FakeTestSdkProgram.Marker()`, run through `ExtractorProcess` with a `TempMap`; assert exit 0 (message = stderr, so the Red shows the refusal line), no symbol or part row whose path contains `.nuget/packages/`, one `calls` edge from `M:Sample.Consumer.Build` to `M:__FakeTestSdkProgram.Marker` with `TargetSymbolId` null, residuals 0 · 0; (b) `RepositoryAboveTheSolutionIsTheScopeRoot` — `.gitignore` (`bin/`, `obj/`) and `Shared/Extra.vb` (`Public Class SharedExtra`) in `copy.ParentDirectory`, linked into `Sample.Lib` as `..\..\Shared\Extra.vb`; the injected file under `<temp>/codemem-tests/nuget-<guid>/.nuget/packages/…` linked into both projects by its relative path from each project directory; `Repository.Init(copy.ParentDirectory)`, stage `*`, commit with a `Signature`; run via `ExtractionRun.Execute`; assert `T:Sample.SharedExtra` is a row at path `../Shared/Extra.vb`, no `T:__FakeTestSdkProgram` row, `ReadSolutions(...)(0).RepoRoot` trimmed equals `copy.ParentDirectory`, `CommitSha` non-null; delete the outside directory in `Finally`; (c) `ProjectFileOutsideTheScopeRootContributesNothing` — `Directory.Move` `Sample.Lib` to `<parent>/Outside/Sample.Lib`, fix the path in `Sample.sln` and the `ProjectReference` in `Sample.App.vbproj`, `DotnetCli.Run("restore …")`, run; assert no `Project:Sample.Lib` and no `T:Sample.Widgets.LeafWidget` row, `Project:Sample.App` present, the `depends_on` edge from `Project:Sample.App` to `Project:Sample.Lib` has a null target, the `extends` edge `T:Sample.Widgets.AppWidget` → `T:Sample.Widgets.LeafWidget` has a null target, every part of `N:Sample.Widgets` is under `Sample.App/`, residuals 0 · 0; (d) `DeclarationsThatLeaveTheScopeAreRetired` — run 1 with `Sample.Lib/Generated/Fake.Test.Sdk.Program.vb` (Main only, in scope by the default glob), then move it to `<parent>/.nuget/packages/fake.test.sdk/1.0.0/build/net8.0/` and add the relative `<Compile Include>`; run 2: `SymbolsRetired = 2`, both rows present with `IsActive = False`, `RenameCandidates = 0`, `SymbolsMatched = run1.SymbolsObserved - 2`, residuals 0 · 0. Header: RED lines to be filled by T005
- [X] T003 [P] Create `tests/CodeMem.Tests/Fixpack003/R01_DuplicateRefusalTests.vb` with one fact `TwoInScopeDeclarationsOfOneIdAreRefusedByName`: `Dup.vb` (`Public Class Dup` + `End Class`) written into both `Sample.Lib/` and `Sample.App/` of a `FixtureCopy`; `ExtractorProcess.Run` with a `TempMap`; assert exit 1, empty stdout, exactly one stderr line containing `duplicate doc-comment id T:Sample.Dup`, `Sample.App/Dup.vb:1,14` and `Sample.Lib/Dup.vb:1,14`, no `   at ` frame, and `MapQueries.CountUserTables(map.Path) = 0`. Header: "RED: not red — the guard exists (Stage A FR-019 step 1); recorded as its production-route proof (Article XIII); trusted through its FIRE (T014)"
- [X] T004 [P] Create `tests/CodeMem.Tests/Fixpack003/R02_BareNameTests.vb` with three facts: (1) `BareNamesOnTheCommittedFixtureAreOccurrences` — extract `_fixture.SolutionPath` into a `TempMap`; a private `Locate(relativeFile, lineAnchor, token)` reads the fixture file, finds the line containing the anchor and the whole-word column of the token, and returns (line, column); assert exactly one `calls` edge for each of the seven expected occurrences (quickstart §4: `M:Sample.Fields.Total`→`F:Sample.Fields.a` at `Return a`; `MainForm_Load`→`P:Sample.MainForm.Timer1` and →`M:Sample.MainForm.OnTick(System.Object,System.EventArgs)` on the `AddHandler` line; `MainForm_Shown`→`Timer1` at `Timer1.Start()`; `OnTick`→`Timer1` at `Timer1.Stop()`; `Dispose(System.Boolean)`→`F:Sample.MainForm.components` at `AndAlso components IsNot Nothing` and at `components.Dispose()`), each with `TargetSymbolId.HasValue`, the located line and column, `Length = token.Length`, and the file substring at `StartOffset` equal to the token; assert `ReadEdges(…, "handles").Count = 5` and no edge whose target doc id contains `.get_` or `.set_`; (2) `BareExternalMembersWriteNothing` — `FixtureCopy`, `Replace("Sample.Lib/Consumer.vb", "sb.Append(w.ToString())", "sb.Append(w.ToString())" & vbCrLf & "        sb.Append(vbCrLf)")`, run; assert 0 edges with target `F:Microsoft.VisualBasic.Constants.vbCrLf` and at least one `calls` edge from `M:Sample.Consumer.Build` to a target starting `M:System.Text.StringBuilder.Append(` with a null target (the member-access contrast — the stated limit); (3) `TwoRunsWriteNoCandidateAndBalance` — extract the committed fixture twice into one `TempMap`; run 2: `RenameCandidates = 0`, `SymbolsRetired = 0`, `SymbolsNew = 0`, residuals 0 · 0, `CountRows("rename_candidates")` equal before and after. Header: RED lines to be filled by T005
- [X] T005 Run `dotnet test CodeMem.sln --filter "FullyQualifiedName~R01_|FullyQualifiedName~R02_|FullyQualifiedName~S02_SchemaUpgradeTests" (the folder is not part of the test namespace)` on the untouched source tree; confirm each Red is the named one: R01 (a) exit 1 with the refusal line in the relative form (`../.nuget/packages/fake.test.sdk/1.0.0/build/net8.0/Fake.Test.Sdk.Program.vb:4,8 and …:4,8`), (b) `T:__FakeTestSdkProgram` present or a collision, (c) `Project:Sample.Lib` present, (d) `SymbolsRetired` 0; R01_Duplicate green (not Red, as declared); R02 (1) zero of the seven edges, (2) green (no bare edges exist at all today: **not Red**, recorded as such — its Red-worthy half is proven by (1) and its guard by the T017 FIRE), (3) green (not Red: candidates were already 0; trusted through I6/I7 and T017); S02 (1) still green on the untouched tree (its Red comes with rule 2, T016). Record every line verbatim in the three headers and the S02 observation in `specs/003-extractor-fixpack/plan.md` "Implementation record". Any Red not in plan §Test design stops the work

**Checkpoint**: every named Red observed and recorded; no source file changed.

---

## Phase 3: User Story 1 — Out-of-repo declarations are not solution symbols (Priority: P1)

**Goal**: One scope root per run; declaring documents and project files outside it contribute nothing;
references to them are external targets; the in-scope duplicate refusal is unchanged and proven through the
executable.

**Independent Test**: `R01_ScopeRootTests` (a)–(d) and `R01_DuplicateRefusalTests` green; every Stage A and
002 test green.

- [X] T006 [US1] Create `src/CodeMem.Extraction/Workspace/SolutionScope.vb`: `Public Class SolutionScope` with `Shared Function Resolve(repoRoot As String, basePath As String) As SolutionScope` (non-empty `repoRoot` → root = repoRoot and `IsRepository = True`, else root = basePath), `ReadOnly Property Root As String` (through `Path.GetFullPath`, `AltDirectorySeparatorChar` replaced by `DirectorySeparatorChar`, one trailing separator), `ReadOnly Property IsRepository As Boolean`, `Function Contains(fullPath As String) As Boolean` (`Path.GetFullPath(fullPath)` with separators normalised, `StartsWith(Root, StringComparison.OrdinalIgnoreCase)`). XML docs name FR-201 and the STOP 1 addition (resolved full paths, case-insensitive, separator-normalised)
- [X] T007 [US1] In `src/CodeMem.Extraction/Workspace/CompiledInputs.vb`, change `SourceTrees(project, compilation)` to `SourceTrees(project, compilation, scope As SolutionScope)`: the document filter becomes `IsSourceDocument(project, document) AndAlso scope.Contains(document.FilePath)`; remove the two-argument form (one door, two callers); update the file header with a dated 003 line; `Enumerate` and `IsSourceDocument` untouched (FR-206)
- [X] T008 [US1] In `src/CodeMem.Extraction/Run/ExtractionRun.vb`: after `GitProvenance.Read` in step 7, `Dim scope As SolutionScope = SolutionScope.Resolve(git.RepoRoot, basePath)`; in step 8 build `inScope` = the compiled projects whose `Project.FilePath` `scope.Contains`, and run the symbol walk (with `SourceTrees(…, scope)`), the project rows and the seven tree rules over `inScope` only; step 6 (the green gate over every compiled project) unchanged; header gains the dated 003 line and the run-order comment names the scope
- [X] T009 [US1] In `tests/CodeMem.Tests/Acceptance/AcceptanceRunner.vb`, resolve the scope the way the run does (`CompiledInputs.Enumerate` → `GitProvenance.Read(basePath, inputs).RepoRoot` → `SolutionScope.Resolve`) and pass it to `SourceTrees`, so the handles-in-source count covers the same trees the run maps; header gains the dated line
- [X] T010 [US1] `dotnet build CodeMem.sln` → 0 errors / 0 warnings; run `--filter FullyQualifiedName~R01_` → all five facts green; record GREEN lines with the date in both R01 headers
- [X] T011 [US1] FIRE for `R01_ScopeRootTests` (b): in `SolutionScope.Resolve` use `basePath` as the root even when `repoRoot` is present → (b) red (`T:Sample.SharedExtra` missing) and (a), (c), (d) still green; revert → green; record the FIRE line
- [X] T012 [US1] FIRE for the separator/case addition: in `SolutionScope.Contains` compare with `StringComparison.Ordinal` and skip the separator normalisation → run the R01 battery and record whether any fact went red (on this file system the copy's paths are lower/upper consistent, so a green result is recorded as "not fired on this machine; the rule is exercised by the paths the tests build"), revert → green; record the observation honestly
- [X] T013 [US1] (folded into T019: the whole suite ran once on the finished tree; after rule 1 alone the R01 battery, S02 and the refusal fact were run) Run the whole suite → every Stage A and 002 test green except the named S02 (1), which is **still green here** (rule 2 is not in yet); record

**Checkpoint**: rule 1 complete; the fixture maps no injected file; MemOS not yet run (Phase 5).

---

## Phase 4: User Story 2 — Bare-name references are recorded (Priority: P2)

**Goal**: The `calls` rule's fourth shape; one occurrence per identifier; rows only (the stated limit);
handles wiring unchanged; no candidate written; counts balance.

**Independent Test**: `R02_BareNameTests` (1)–(3) green; I3's five `handles` edges unchanged; I12 green over
every edge; S02 (1) amended and green.

- [X] T014 [US2] FIRE for `R01_DuplicateRefusalTests` (deferred from Phase 3 so it fires against the finished rule 1): in `src/CodeMem.Core/Reconciliation/Reconciler.vb` step 1, replace the `Throw` with `seen(symbol.DocCommentId) = symbol` (skip) → the fact red (exit 0 instead of 1); revert → green; record the FIRE line
- [X] T015 [US2] In `src/CodeMem.Extraction/Edges/CallsRule.vb`, add the fourth shape before the existing three: `Dim simple As SimpleNameSyntax = TryCast(node, SimpleNameSyntax)`; when non-null call `CollectBareName(context, model, simple, sink)` and `Continue For`. `CollectBareName`: return when the parent is a `MemberAccessExpressionSyntax` whose `Name Is simple`, a `QualifiedNameSyntax`, or an `AttributeSyntax`, or when `InsideOtherVerbClause(simple)`; `GetSymbolInfo(simple)`: return when `Symbol Is Nothing` or `CandidateSymbols.Length > 0`; admit `IFieldSymbol`, `IPropertySymbol`, `IEventSymbol`, and `IMethodSymbol` whose `MethodKind` is `Ordinary`, `DeclareMethod` or `ReducedExtension`; `targetDocId = context.DocIdOf(target)`; return unless `context.IsRow(targetDocId)` (FR-209, the stated limit — say so in the XML doc); `source = EnclosingSymbolResolver.RowSymbolAt(context, model, simple)`; return when Nothing; add the `calls` edge located at `simple.Identifier`. Update the file's description line and the class summary (four shapes; rows only for the fourth)
- [X] T016 [US2] `dotnet build` → 0/0; run `--filter "FullyQualifiedName~R02_|FullyQualifiedName~I03_|FullyQualifiedName~I12_|FullyQualifiedName~S02_"` → R02 (1)–(3) green, I3 and I12 green, **S02 (1) red on `code_edges` (expected 137)** — record the Red line verbatim in `S02_SchemaUpgradeTests.vb`'s header with the reason (rule 2 writes the fixture's bare-name occurrences; an observation table replaced per run is not "kept" across extractor versions), then amend the assertion in place: read `freshEdges` = `CountRows` of `code_edges` on a fresh `TempMap` extracted by the same executable, assert `after("code_edges") = freshEdges` (and keep `code_symbols`, `code_parts`, `rename_candidates` equality); re-run → green; record GREEN lines in R02 and S02
- [X] T017 [US2] FIRE for `R02_BareNameTests`: in `CollectBareName` admit fields only (drop `IPropertySymbol`, `IEventSymbol`, `IMethodSymbol`) → (1) red on the `Timer1` and `OnTick` facts; revert → green. Second injection: drop the `IsRow` check → (2) red (an edge to `F:Microsoft.VisualBasic.Constants.vbCrLf` appears); revert → green; record both FIRE lines
- [X] T018 [US2] In `src/CodeMem.Extraction/CodeMem.Extraction.vbproj`, `<Version>0.1.0</Version>` → `<Version>0.2.0</Version>` (R38, FR-217); rebuild; `dotnet test --filter FullyQualifiedName~ProjectFileGateTests` green; a fixture run's `ExtractorVersion` reads `0.2.0` (assert once in `R02_BareNameTests` (3) on run 2's `RunRow.ExtractorVersion`; record). **Named Red**: `Invariants/US1_FirstRunTests` asserts `"0.1.0"` → red on the bump; record the Red line in its header, amend the literal to `"0.2.0"` in place with the dated line, re-run → green
- [X] T019 [US2] Run the whole suite → all green (59 + 8 new, 1 skipped); record the run line in `quickstart.md` "Build and test" and the elapsed time of the fixture extraction (SC-010)

**Checkpoint**: both rules complete on the fixture; the suite is the evidence.

---

## Phase 5: User Story 3 — The live map, on a copy (Priority: P3)

**Goal**: The PM acceptance of tasks 142376 and 142358 on a copy of `C:\_DB\codemem.sqlite` (STOP 1: against a
copy, never the live file). GameRoom, CodeMem, then MemOS with its key. A refusal on an in-repo pair stops
the work.

**Independent Test**: the record tables in `quickstart.md` filled with measured values.

- [X] T020 [US3] `Copy-Item C:\_DB\codemem.sqlite <scratch>\codemem.003-copy.sqlite`; confirm the copy's `map_identity`, solutions (1 GameRoom, 2 CodeMem) and three runs match the live map; record the orphan baseline on the copy with `specs/003-extractor-fixpack/orphans-baseline.sql` through a scratch read-only runner (306 = 97 + 209; 125 = 3 + 1 + 2 + 119 expected, R37)
- [X] T021 [US3] Run the built executable: `--solution C:\Users\rchau\source\repos\GameRoom\GameRoom.sln --db <copy> --solution-key GameRoom`; expect exit 0, `symbols_retired = 2`, `rename_candidates = 0`, residuals 0 · 0, `extractor_version = 0.2.0`; record the summary line verbatim and the elapsed time in `quickstart.md`
- [X] T022 [US3] Run `--solution C:\Users\rchau\source\repos\CodeMem\CodeMem.sln --db <copy> --solution-key CodeMem`; expect exit 0, the same two retirements, new symbols for this feature's own types and tests, residuals 0 · 0; record verbatim
- [X] T023 [US3] Run `--solution C:\Users\rchau\source\repos\rchaudio-a11y\MemOS\MemOS.sln --db <copy> --solution-key MemOS`; record **whichever** outcome verbatim: (a) exit 0 and the summary line — MemOS joins the copy as solution 3, its id noted for the MemOS-side registry binding (not done here); (b) exit 1 and the refusal line naming an in-repo pair — record, **stop the work**, and report the Article VI (A) trigger; nothing is amended
- [X] T024 [US3] After the runs, on the copy: re-run `orphans-baseline.sql` per solution; `codemem_references`' statement for `_balance` (the `code_edges` rows whose `target_symbol_id` is the row `F:GameRoom.Games.Blackjack.BlackjackControl._balance`, or its exact doc id as found, with path, line, column, verb, source); fill every cell of the quickstart record tables (ten counts, orphans total and per project / per kind deltas, the explanation by kind: rule 1 module + `Main`; rule 2 bare `AddressOf` methods; containers through 059 FR-013); confirm `rename_candidates` count on the copy is unchanged (0) and `C:\_DB\codemem.sqlite` and `C:\_DB\memos.sqlite` are byte-identical to before (hash before and after)

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T025 [P] Review pass against the v1.2.1 Review Gates over every touched file: header + XML docs (`FileHeaderGateTests`), Option settings (`ProjectFileGateTests`), SQL location (`SqlLocationGateTests`), tripwire (`TripwireTests`), every FIRE line present (R01 ×2, R01_Duplicate ×1, R02 ×2, S02 amendment), no new abstraction; record in `plan.md` "Implementation record"
- [X] T026 [P] Write `plan.md` "Implementation record (2026-09-13)": environment, order followed, every deviation from the design documents with its reason, the S02 amendment, the known limits carried to MemOS 060, what was not done (commits are the Architect's; the MemOS registry binding; the live file untouched)
- [X] T027 Final full suite run on the finished tree; paste the result into `quickstart.md`; confirm `git status` shows only the intended files changed and the copy of the map lives outside the repository

---

## Dependencies & Execution Order

- Phase 2 (T002–T005) before any source change; T002–T004 are parallel (three new files).
- Phase 3 (T006 → T007 → T008 → T009 → T010 → T011 → T012 → T013) sequential (shared files).
- Phase 4: T014 first (a FIRE against finished rule 1), then T015 → T016 → T017 → T018 → T019.
- Phase 5 strictly in order T020 → T021 → T022 → T023 → T024; T023 (b) ends the work at T024's record.
- Phase 6 after Phase 5 (or after Phase 4 if T023 stopped the work).

## Implementation strategy

Rule 1 first (US1): it is the smaller change and the one MemOS waits on; its Reds are the collision the live
run already showed. Rule 2 second (US2): one method in one rule class. The live-map acceptance (US3) runs on a
copy, never the live file, and its MemOS step is allowed to end the work.

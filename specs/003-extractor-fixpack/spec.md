# Feature Specification: CodeMem Fixpack 003 — Extractor Rules: Out-of-Repo Declarations and Bare-Name References

**Feature Branch**: `003-extractor-fixpack`

**Created**: 2026-09-13

**Status**: Implemented 2026-09-13 (STOP 1 ruled; suite 68 passed / 1 skipped; Phase 5 on a copy of the map:
MemOS completed, outcome (a)); uncommitted, commits are the Architect's

**Input**: User description: "CodeMem 003: extractor fixpack. PM: tasks 142376 and 142358. Parent: 002,
accepted. Build two extractor rules. (1) Out-of-repo declarations are not solution symbols. (2) Bare-name
references are recorded. Invariants as tests. Do not amend Article VI (A) or bump the schema; do not touch
code_map_solutions or anything in memos.sqlite; do not fix MemOS's test projects. Ceremony: STANDARD, no
spike, one stop after plan. Clarify before planning: the rule 1 predicate; one occurrence or two for a
property access and which verb; WithEvents declarations." (Prompt document 142377; umbrella task 142385.)

**Governing document**: `.specify/memory/constitution.md` v1.2.1, unamended. Where this specification and
the constitution appear to differ, the constitution wins and the difference is a defect in this document.

**Parent features**: `specs/001-extractor-codemem-sqlite/` (Stage A) and `specs/002-stage-a-fixpack/`
(accepted). Requirements here are numbered FR-201 onward so they never collide with FR-001–FR-039 or
FR-101–FR-123. Where a Stage A or 002 requirement changes, the change is stated here and the number is named.

**PM**: task 142376 (out-of-repo files; parent 131389 Fixpacks) and task 142358 (bare-name references; parent
132087 Keys registry + read surface), both on the CodeMem project 131373. Their details were read from the
PM store on 2026-09-13 and are quoted where they bind.

## Clarifications

### Session 2026-09-13 (before planning)

The three questions the description asked to be settled before planning, answered here with the reasoning;
each answer is a proposal until the Architect rules at the plan stop. Four further decisions the description
did not make are recorded after them, in the same form, so nothing is decided silently.

- Q1: Rule 1 predicate — repo-root prefix on the declaring file's path, or "file not in any project's compile
  items"? → A: **Repo-root prefix on the declaring file's full path.** The colliding file is a compile item:
  the test SDK's targets inject `Microsoft.NET.Test.Sdk.Program.vb` into every test project's evaluated
  compile list, and the workspace reports it as one of the project's documents. A compile-item test would
  therefore exclude nothing; the only fact that distinguishes the file from MemOS's own code is where it
  lives. The predicate is: *the declaring file's full path lies under the scope root*, where the scope root
  is the repository working directory when the solution lies in one — the value the run already writes to
  `solutions.repo_root` — and otherwise the solution's base directory (the directory of the `.sln` or
  `.vbproj` given). Comparison is a path-prefix test on normalised full paths, following the platform's case
  rule, the same rule the existing `obj/` exclusion uses. "Declaring file" means every declaring document
  (a syntax tree) **and** the project file of a project row: a project whose file lies outside the scope
  root contributes no project row and no symbols; references to any of them from inside the scope root
  become external targets by doc-comment id. Verified 2026-09-13: the current extractor refuses MemOS with
  exit 1 naming `../../../../.nuget/packages/microsoft.net.test.sdk/17.8.0/build/netcoreapp3.1/Microsoft.NET.Test.Sdk.Program.vb:4,8`
  twice (once per test project); the NuGet cache lives under the user profile, outside every repository.
- Q2: Does a property access record one occurrence (the property) or two (get and set accessors), and which
  verb? → A: **One occurrence, targeting the property, verb `calls`.** Accessors are never rows (Stage A
  Clarifications: compiler-generated and accessor symbols are skipped; the map has no accessor identity to
  point at), the compiler binds the bare identifier to the property symbol and not to an accessor
  (verified 2026-09-13 with a binding probe), and `calls` is the verb the map already writes for
  `obj.Property` and `obj.Field` member access (Stage A FR-017). A read and a write are not distinguished:
  the map has no read/write column and Article X forbids inventing one behind a verb; a compound assignment
  is one identifier and one occurrence. The same holds for a field: one occurrence per identifier, verb
  `calls`.
- Q3: WithEvents field declarations — occurrence, or already covered by `handles`? → A: **Neither: a
  declaration is not a reference, so no occurrence is written for the declaration itself.** The `Handles
  Button1.Click` clause's container is already recorded as the `handles` edge's `via_symbol_id` and stays
  so. A WithEvents member named bare in a body (`Timer1.Start()` names `Timer1`) is a bare property get and
  records one `calls` occurrence targeting the WithEvents member's row, like any other property.
- Q4 (not asked; decided): Which bound targets does rule 2 record? → A: **Only targets that are rows of this
  run** (declared in the solution and inside the scope root). A bare name bound to an external member —
  `vbCrLf` binds to a field of `Microsoft.VisualBasic.Constants` — writes nothing. Reason: the description's
  invariant "every recorded bare-name occurrence has a non-NULL `target_symbol_id`" is then true by
  construction, and the consumers this feature serves (`codemem_references` takes a mapped symbol id only;
  `codemem_orphans` counts inbound edges on rows) never ask about an external target. The alternative —
  record externals with a NULL target as member access does — would add one edge per `vbCrLf` and satisfy
  no consumer; it is the one place rule 2 is narrower than member access, and it is stated here.
- Q5 (not asked; decided): Are bare event names covered? → A: **Yes, the same way.** `RaiseEvent X`,
  `AddHandler X, …` and `RemoveHandler X, …` name the event bare and bind to the event symbol; `obj.X` in the
  same positions already writes `calls` to the event. An event whose only uses are bare `RaiseEvent`
  statements would otherwise read as unreferenced. The description names fields, properties and
  `AddressOf`; this is the fourth member kind the same mechanism binds, recorded here so it can be struck.
- Q6 (not asked; decided): The source digest. → A: **Unchanged.** The digest covers every compiled input, in
  or out of the repository (Stage A FR-005 as reworded by 002 FR-108: provenance of the files the extractor
  chose). This feature scopes *identity*, not provenance: what compiled is stamped; what is the solution's
  is registered. The two scopes differ exactly by the out-of-repo inputs, and a package upgrade that changes
  an injected file still changes the digest with no fact change (002 FR-110's shape). The description's
  phrase "the source digest already scopes to selected files" is true of the selection rule (documents
  outside `obj/`, project files, the solution file, the four build files); it is not a repository-root
  scope, and this feature does not make it one.
- Q7 (not asked; decided): The extractor version stamp. → A: **Becomes 0.2.0.** The fact set for an
  unchanged source changes under both rules (rows leave, edges arrive). Article IV defines determinism per
  extractor version; a run that produces a different fact set from the same digest under the same version
  would read as non-determinism. One line in the extraction project file; no schema change.

### Session 2026-09-13 — STOP 1 rulings (Architect)

- **Q1 accepted as stated, with one addition**: the prefix comparison is on **resolved full paths,
  case-insensitive and separator-normalised**, and the SDK-file Red **exercises the relative form the
  2026-09-12 refusal printed** (an include with `..` segments; the Red records that line). FR-201 carries the
  addition; the Red is recorded in the test header.
- **Q2 and Q3 as answered.**
- **Q4 accepted as a stated limit, not a property**: a bare name bound to an external member writes
  nothing. It is recorded under the extractor's known limits (plan, "Known limits") and **carried to MemOS
  060's `codemem_references` description update**, which must state it. FR-209 is reworded accordingly;
  the description's non-NULL invariant still holds and is still tested, as a consequence of the limit.
- Q5–Q7 stand as proposed (not struck).
- **Proceed**: tasks, Red-first, the suite, then the quickstart **against a copy of the map** — GameRoom,
  CodeMem, then MemOS with its key. A refusal on an in-repo pair stops the work.

## User Scenarios & Testing *(mandatory)*

Actors: the **Operator**, who runs the extractor; the **Architect**, who reads the map, the run records and
the review gates; the **Consumer**, MemOS's read-only tools over the map (`codemem_references`,
`codemem_orphans`, `codemem_symbol_detail`).

### User Story 1 - A solution is mapped as its own code, not as what the SDK injected (Priority: P1)

An Operator extracts MemOS, a solution with two test projects. Today the run refuses before writing: the test
SDK injects one generated program file into each test project at build time, the file lives in the NuGet
cache outside the repository, and the two copies carry one doc-comment id. After this feature, a symbol
declared in a file outside the solution's repository root is not a solution symbol: it gets no registry
row and no parts, so it cannot collide with itself. Anything that references such a symbol still records
the reference, as an external target named by doc-comment id — the treatment framework symbols already
get. MemOS joins the map, or it refuses on a collision that is MemOS's own.

**Why this priority**: MemOS is the only solution the Consumer serves that is not in the map, and this
rule is the only thing between MemOS and its map (task 142376). The refusal that happened is evidence about
a foreign file, not about MemOS's code, and the full Article VI (A) amendment must stay deferred on
honest grounds until an in-repo collision arrives.

**Independent Test**: Copy the fixture, inject one file from a `.nuget/packages/` path outside the copy
into both projects, reference one of its members from a fixture type, extract: the run completes, no row
carries that path, the reference is an edge with a NULL target. Then place two in-repo declarations of one
id in the copy and extract through the executable: exit 1, one line naming both locations.

**Acceptance Scenarios**:

1. **Given** a solution whose two projects both compile one file located under a `.nuget/packages/`
   directory outside the scope root, **When** the extractor runs, **Then** it completes (exit 0), no
   `code_symbols` row and no `code_parts` row carries a path under that directory, and the ten counts
   balance (both residuals 0).
2. **Given** scenario 1 with a solution type calling a member declared in that file, **When** the
   extractor runs, **Then** a `calls` edge exists from the calling method with `target_symbol_id` NULL and
   `target_doc_comment_id` equal to the member's doc-comment id.
3. **Given** a solution inside a repository whose working directory is above the solution directory, with
   one source file linked from a sibling directory inside that repository and one injected file outside
   it, **When** the extractor runs, **Then** the linked file's type is a row (path with `..` segments), the
   injected file's types are not, and `solutions.repo_root` reads the repository working directory.
4. **Given** the same solution outside any repository, **When** the extractor runs, **Then** the scope root
   is the solution directory: a file linked from above the solution directory contributes no row.
5. **Given** two projects of one solution, both inside the scope root, each declaring a type with the same
   doc-comment id, **When** the executable runs, **Then** exit code 1, exactly one stderr line that names
   the id and both locations (path, line, column), nothing on stdout, and a fresh map path holds no user
   table afterwards (Stage A's refusal, unchanged).
6. **Given** a map holding a solution extracted before this feature, whose registry has rows for
   out-of-repo declarations, **When** the solution is extracted again, **Then** those rows are retired
   (`is_active = 0`, rows kept), counted in `symbols_retired`, no rename candidate is written, and every
   other row is matched.
7. **Given** MemOS, **When** the Operator runs `--solution-key MemOS` against `MemOS.sln`, **Then** exactly
   one of: (a) the run completes and the ten counts are recorded, or (b) the run refuses with exit 1 on an
   in-repo pair, both locations named and recorded. Either outcome is the deliverable; (b) is the Article
   VI (A) evidence and stops the work — the amendment is not opened here.

---

### User Story 2 - A member used only by its bare name is no longer invisible (Priority: P2)

A Consumer asks `codemem_references` for a private field that a class reads and writes on four lines and
gets four occurrences with path, line and column. A method wired only by `AddressOf StepCornered` (no
receiver) is no longer an orphan, because the wiring site is now an occurrence that targets it. A
WithEvents member used as `Timer1.Start()` has an occurrence at `Timer1`. Nothing about `Handles` wiring
changes: the handler's own outbound `handles` edge is written as before.

**Why this priority**: 059 measured the gap on the live map — 261 of 262 unreferenced GameRoom fields are
used by name, and `codemem_orphans` had to exclude fields and properties from examination because a bare
use leaves no edge (059 fact 4, FR-014). Until this ships, a references count reads low and an orphan can
be a false positive for any symbol whose only uses are bare (task 142358).

**Independent Test**: Extract the committed fixture and read `calls` edges: `Fields.Total` → field `a`
(`Return a`); `MainForm.MainForm_Shown` and `MainForm.OnTick` → property `Timer1`; `MainForm.MainForm_Load`
→ method `OnTick` (`AddressOf OnTick`) and → `Timer1`; `MainForm.Dispose` → field `components` twice, on two
consecutive lines. Seven occurrences; each carries a non-NULL target and a span whose text is the member's
name at the recorded line and column. The `handles` edges are exactly Stage A's five.

**Acceptance Scenarios**:

1. **Given** a method that reads a field by its bare name, **When** the extractor runs, **Then** one
   `calls` edge exists from the method to the field's row, located at the identifier (path, 0-based offset
   and length, 1-based line and column), and the span text equals the field's name.
2. **Given** a method that writes a field by its bare name (plain or compound assignment), **When** the
   extractor runs, **Then** one `calls` edge exists per identifier, the same shape as scenario 1; a read and
   a write of the same field on different lines are two occurrences.
3. **Given** a method that gets or sets a property by its bare name, **When** the extractor runs, **Then**
   one `calls` edge targets the property's row (never an accessor), one per identifier.
4. **Given** an `AddressOf Member` with no receiver, **When** the extractor runs, **Then** one `calls` edge
   targets the method's row, located at the member identifier; when the `AddressOf` sits in an
   `AddHandler`, the handler's outbound `handles` edge is also written exactly as before (I3's counts hold).
5. **Given** a bare identifier bound to a local, a parameter, a range variable, a type, a namespace or a
   type parameter, **When** the extractor runs, **Then** no edge is written for it.
6. **Given** a bare identifier bound to a member declared outside the solution or outside the scope root,
   **When** the extractor runs, **Then** no edge is written for it (Q4).
7. **Given** a solution extracted before this feature, **When** it is extracted again, **Then** the run
   writes no rename candidate, the `rename_candidates` table has the rows it had, every symbol is matched
   (no retirement caused by this rule), and both residuals are 0.
8. **Given** the same source and the same build, **When** the extractor runs twice into two maps, **Then**
   the canonical fact sets are equal (I2 with the new edges included).

---

### User Story 3 - The Operator re-extracts the live map and the consumers' answers move (Priority: P3)

The Operator re-extracts GameRoom and CodeMem into the live map, then extracts MemOS. The 059 orphan
baseline (GameRoom 306: 97 app, 209 tests; CodeMem 125: 6 libraries, 119 tests) moves, and the movement is
explainable by kind: the test platform's generated module and its `Main` leave each list under rule 1;
methods reached only by bare `AddressOf` leave the list under rule 2; fields and properties become
examinable on the MemOS side once the tool stops excluding them. `codemem_references` on a field with known
uses returns them.

**Why this priority**: the two rules are only worth their evidence on the live map; the acceptance of both
PM tasks is a before/after count there.

**Independent Test**: The orphan statement 059 shipped, run over the live map before and after (read-only
reads; the after-run counts and the retired rows are read from the run records).

**Acceptance Scenarios**:

1. **Given** the live map with GameRoom and CodeMem at the 059 baseline, **When** each is re-extracted,
   **Then** each run completes with residuals 0 · 0, `symbols_retired = 2` (the generated module and its
   `Main`), `rename_candidates = 0`, and the ten counts are recorded in the quickstart.
2. **Given** the re-extracted map, **When** the 059 orphan statement is run, **Then** the totals differ from
   306 and 125, and the difference is accounted for by kind and project in the recorded table.
3. **Given** the re-extracted map, **When** `codemem_references` (or its statement) is asked about a field
   with known bare uses (`_balance` in `GameRoom/Games/Blackjack/BlackjackControl.vb`), **Then** the
   occurrences on its use lines are returned with path, line and column.
4. **Given** MemOS, **When** it is extracted with `--solution-key MemOS`, **Then** the outcome of US1
   scenario 7 is recorded verbatim (the summary line, or the one refusal line).

---

### Edge Cases

- **Partial type with one part outside the scope root**: the symbol is a row with its in-scope parts only;
  the out-of-scope part is not a part and does not enter the body hash — the same treatment generated and
  embedded trees already get.
- **A project file outside the scope root**: the project contributes no project row, no symbols and no
  edges; `depends_on` edges to it from in-scope projects carry a NULL target; a compile error in it still
  refuses the run (the green gate covers every project that compiled).
- **Solution in a repository with no commit**: the run writes `commit_sha` NULL and `repo_root` NULL (Stage
  A FR-006 unchanged), so the scope root is the solution directory — the same rule as no repository; stated
  so a reader is not surprised.
- **A NuGet cache inside the repository** (a repository-local `RestorePackagesPath`, or a committed
  `.nuget/packages/`): the injected file is in-repo and therefore mapped; two test projects then collide
  and the run refuses, naming both locations. That is the honest outcome under one rule; no second, path-
  text rule (`.nuget/packages/` as a literal) is added.
- **A linked file inside the repository but outside the solution directory**: mapped when a repository is
  present (scope root = repository); not mapped when none is (scope root = solution directory). US1
  scenarios 3 and 4 pin both.
- **Bare name that is also the expression of an invocation** (`Run(1)`, `Refresh` without parentheses,
  `Items(0)` on a parameterised property): the invocation rule already records the occurrence at the same
  identifier; the bare-name rule records the same occurrence; canonicalisation keeps one. A parenthesis-less
  call is parsed as an invocation and is already covered (verified 2026-09-13).
- **Bare name in a field initializer, a parameter default, a property accessor, a lambda, an attribute
  argument or an object-initializer member (`With {.Label = s}`)**: the source is the nearest enclosing row
  symbol (Stage A FR-016): the field, the method, the property, the attributed member's row.
- **Bare name in an `Implements`, `Inherits`, `Imports` or `Handles` clause**: those are other verbs; no
  `calls` occurrence (Stage A FR-017's exclusion, extended to the new shape). An attribute's name binds to a
  constructor and is never a bare-name occurrence.
- **`NameOf(member)`**: the compiler binds the identifier to the member; it is an occurrence like `NameOf(obj.member)` is today.
- **Ambiguous or unbound bare name**: never written (Stage A FR-017: unresolved or candidate-ambiguous
  bindings are not written).
- **Escaped identifier** (`[Stop]`): the occurrence span is the identifier token; span text includes the
  brackets, as it already does for member access. Not changed here; recorded.

## Requirements *(mandatory)*

### Functional Requirements

**Rule 1 — out-of-repo declarations are not solution symbols (task 142376)**

- **FR-201**: The extractor MUST resolve one **scope root** per run: the repository working directory when
  the solution's base directory lies inside a repository with a commit (the same value written to
  `solutions.repo_root`), otherwise the solution's base directory. The rule lives in one place and every
  scoping decision consults it (Article XII). **The prefix comparison is on resolved full paths** (relative
  segments resolved), **case-insensitive and separator-normalised** (STOP 1 ruling).
- **FR-202**: A declaring document (a compiled source file) whose full path does not lie under the scope
  root MUST NOT be a declaring part of any symbol: no `code_symbols` row is minted or matched from it, no
  `code_parts` row is written for it, and no edge is sourced from an occurrence in it. Stage A FR-007/FR-010's
  "every declaring reference in a compiled source document outside `obj/`" becomes "… outside `obj/` and
  inside the scope root".
- **FR-203**: A project whose project file does not lie under the scope root MUST contribute no project row,
  no symbols and no edges. Its compile diagnostics still count toward the green gate (Stage A FR-004
  unchanged).
- **FR-204**: A reference from inside the scope root to a symbol excluded by FR-202 or FR-203 MUST be written
  exactly as a reference to a framework symbol is: `target_symbol_id` NULL, `target_doc_comment_id` present
  (Stage A Clarifications: external targets).
- **FR-205**: The duplicate doc-comment id refusal (Stage A FR-019's step 1, Article VI (A)) is unchanged for
  in-scope declarations: two in-scope declarations of one id MUST still refuse with exit 1, one stderr line
  naming the id and both locations, nothing written. This feature adds the production-route test that proves
  it (Article XIII); none existed.
- **FR-206**: The compiled-inputs enumeration, the source digest and the dirty flag (Stage A FR-005/FR-006,
  002 FR-107/FR-108) are unchanged: an out-of-scope compiled document is still a compiled input (Q6).
- **FR-207**: On re-extraction of a solution whose registry holds rows for declarations that are now out of
  scope, those rows MUST be retired by the ordinary Article VI step 4 (not observed → `is_active = 0`, row
  kept) and no rename candidate may arise from them (no new symbol exists to satisfy (B)).

**Rule 2 — bare-name references are recorded (task 142358)**

- **FR-208**: Stage A FR-017's `calls` rule gains a fourth occurrence shape: a **simple name** (an identifier,
  or a generic name) that is not the member name of a member-access expression, not part of a qualified name,
  not an attribute name, and not inside an `Implements`, `Inherits`, `Imports` or `Handles` clause, and that
  the compiler binds — with no candidate ambiguity — to a field (constants and enum members included), a
  property (WithEvents members included), an event, or a method (ordinary, `Declare`, or a reduced extension
  method). The occurrence's source is the nearest enclosing row symbol (FR-016); its target is the bound
  member; its verb is `calls`; its location is the identifier token.
- **FR-209**: An occurrence under FR-208 is written only when its target is a row of this run. **A bare name
  bound to an external member writes nothing: this is a stated limit of the extractor** (STOP 1 ruling), not
  a designed property; it is recorded in the plan's known limits and carried to MemOS 060's
  `codemem_references` description. As a consequence every written bare-name occurrence carries a non-NULL
  `target_symbol_id`, and that consequence is tested.
- **FR-210**: One identifier yields one occurrence: a property get and set are not split into accessors; a
  read and a write are not distinguished; a compound assignment is one occurrence (Q2).
- **FR-211**: `AddressOf Member` with no receiver yields one occurrence targeting the method under FR-208;
  the `handles` edge an `AddHandler` statement produces (Stage A FR-017) is unchanged and is written in
  addition.
- **FR-212**: A declaration is never an occurrence: a WithEvents member's declaration, a field's declarator,
  a property statement and an event statement write nothing under FR-208 (Q3).
- **FR-213**: Canonicalisation (Stage A plan: deduplicate by source, verb, target, via, path, offset, length)
  MUST fold a bare-name occurrence that coincides with an invocation occurrence at the same identifier into
  one edge.
- **FR-214**: Every occurrence under FR-208 MUST satisfy Stage A FR-018: a path relative to the solution
  directory, a span whose text is exactly the member's surface name, a line and column inside the referencing
  file (I12 covers the new edges without change).
- **FR-215**: Rule 2 writes no symbol row, retires none, and writes no rename candidate; the ten counts of
  Article VIII are unchanged in meaning and MUST balance on every run (residuals 0 · 0).

**Cross-cutting**

- **FR-216**: Schema version stays 2; no column, table, trigger, CLI argument or environment variable is
  added. If a schema column proves unavoidable for rule 2, the work stops and says why (description).
- **FR-217**: The extractor's stamped version becomes 0.2.0 (Q7); `extract_runs.extractor_version` records
  it on every run of the new executable.
- **FR-218**: The extractor MUST NOT open, read or write `memos.sqlite`, `code_map_solutions`, or any file
  other than the map (Article IX; description). The MemOS-side registry binding of a new solution id is an
  operator step outside this feature.
- **FR-219**: Every behaviour change above is a Red-first test against real SQLite and the real compiled
  fixture (Article II, III); every guard carries its fire demonstration; the in-repo refusal test records its
  fire since it cannot go Red first (the guard already exists).

### Key Entities *(include if feature involves data)*

- **Scope root**: one absolute directory per run; the repository working directory (when `repo_root` would
  be written) or the solution's base directory. Not stored: `solutions.repo_root` already records the
  repository case, and the fallback is derivable from `last_seen_path`.
- **Declaring file**: the source document of a declaring reference, or the project file of a project row.
  In scope when its full path lies under the scope root.
- **Bare-name occurrence**: a `code_edges` row of verb `calls` whose occurrence is a simple name bound to a
  member row; indistinguishable in the schema from a member-access `calls` row, by design — a consumer asks
  "who references X", not "by which syntax".
- **External target**: a `code_edges` row with `target_symbol_id` NULL and `target_doc_comment_id` present;
  now also the shape of a reference to an out-of-scope declaration.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-201**: On the fixture copy with an injected out-of-scope file in both projects, the run exits 0, 0
  symbol rows and 0 part rows carry a path under the injected directory, and 1 `calls` edge targets the
  injected member with a NULL target.
- **SC-202**: On the fixture copy with two in-scope declarations of one id, the executable exits 1 with
  exactly 1 stderr line containing the id and both `path:line,column` locations, 0 stdout lines, and the
  fresh map path holds 0 user tables.
- **SC-203**: On the fixture copy inside a repository whose working directory is one level above the
  solution, a type linked from `../Shared/` is a row and an injected file outside the repository is not;
  `solutions.repo_root` equals the working directory.
- **SC-204**: On the committed fixture, the seven expected bare-name occurrences (US2 Independent Test) exist
  with non-NULL targets, at the expected path, line and column, with span text equal to the member name; the
  `handles` edge count is still 5 (I3) and I12 passes over every edge.
- **SC-205**: On the fixture copy with a `vbCrLf` use added, 0 edges target
  `F:Microsoft.VisualBasic.Constants.vbCrLf`.
- **SC-206**: Two consecutive runs of the fixture: run 2 has `rename_candidates = 0`, `symbols_retired = 0`,
  residuals 0 · 0, and the `rename_candidates` row count is unchanged between the runs.
- **SC-207**: Live map, GameRoom and CodeMem re-extracted: each run's `symbols_retired = 2`,
  `rename_candidates = 0`, residuals 0 · 0; the orphan totals (059 statement) differ from 306 and 125 and
  the per-kind, per-project deltas are recorded; `codemem_references` on `_balance` returns its bare uses.
- **SC-208**: MemOS: one recorded outcome — the summary line with its ten counts, or the one refusal line
  naming an in-repo pair.
- **SC-209**: The full suite: every Stage A and 002 test still green; the fixture extraction still under
  SC-010's 60 s; the whole suite under 4 minutes on this machine.
- **SC-210**: Review gates: header block and XML docs on every new or changed file; no SQL outside a
  repository method; the tripwire and SQL-location gates green; no new abstraction without three call sites.

## Assumptions

- **The scope root fallback is the solution directory.** When no repository is present (or it has no
  commit, so `repo_root` is NULL), the tree the Operator pointed at is the solution's own directory. The
  alternative — no scoping at all without a repository — would leave a non-repository solution with two test
  projects refusing on the injected file forever, and would make rule 1 untestable without creating
  repositories in every test. The fallback is what the fixture tests exercise; one test creates a repository
  to exercise the primary case.
- **The prefix test follows the platform's case rule** (case-insensitive on Windows), like the existing
  `obj/` rule, and compares normalised full paths with a trailing separator on the root so `C:\repo2\` is not
  under `C:\repo\`.
- **The NuGet cache is outside every repository** in the three solutions this feature serves (it is under
  the user profile); the invariant "a symbol declared under `.nuget/packages/` never gets a row" holds
  through the scope root, not through a path-text rule. A repository-local cache is an edge case recorded
  above, not a supported configuration.
- **Bare-name occurrences use `calls`** because it is the verb the map already uses for member use through
  member access; a new verb would need a schema CHECK change (forbidden here) and would split one question
  ("who uses X") across two verbs.
- **Rows only (Q4)** is the one deliberate asymmetry with member access; it is stated in FR-209 and tested
  (SC-205).
- **Events are included (Q5)**; if struck at the plan stop, FR-208 loses one word and one test assertion.
- **The digest is unchanged (Q6)**; the live map's next runs therefore have digests equal to the previous
  runs' where the source did not change (GameRoom), which makes the fact-set difference attributable to the
  extractor version alone (Q7).
- **No fixture source file changes.** The committed fixture already contains every bare-name shape rule 2
  needs (a bare field read, a WithEvents member used bare, a bare `AddressOf`, a field read twice in one
  method); everything else runs on temporary copies.
- **The operator steps against the live map** are performed after the plan stop, with a file copy of the
  map taken first; the extractor is the only writer, and MemOS reads the map read-only, so no MemOS process
  needs to stop.

## Out of Scope

- The Article VI (A) amendment (project identity in the symbol key, assembly identity on edge targets, a
  schema bump — task 132039). If MemOS refuses on an in-repo pair, that refusal is recorded as its trigger
  and the work stops; it is not opened here.
- Any schema change; any new verb; any CLI argument.
- `code_map_solutions`, `memos.sqlite`, the MemOS registry binding of the new solution id, the
  `codemem_references` description repair (058 F2, in the MemOS fixpack), and making fields and properties
  examinable in `codemem_orphans` (MemOS side, after this ships).
- Changing MemOS's test projects to avoid the collision.
- Recording a reference's read/write direction, or the accessor a property use binds through.
- Recording bare names bound to external members (Q4).
- A literal `.nuget/packages/` path rule.
- The escaped-identifier span text (recorded as an existing limit).

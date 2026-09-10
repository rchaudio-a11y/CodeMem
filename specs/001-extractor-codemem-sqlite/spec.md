# Feature Specification: CodeMem Stage A — Extractor and codemem.sqlite

**Feature Branch**: `001-extractor-codemem-sqlite`

**Created**: 2026-09-09

**Status**: Draft

**Input**: User description: "CodeMem stage A — the extractor and codemem.sqlite. Build a console tool that
loads a VB.NET solution through the compiler, verifies it compiles with zero errors, and writes what the
compiler knows into one SQLite file, codemem.sqlite, under the constitution (v1.2.1). Consumers of the
file are out of scope; this feature ends when the file is correct." (Full description, including the
verb rules, run order, fixture contents and invariants I1–I15, is reproduced in the sections below.)

**Governing document**: `.specify/memory/constitution.md` v1.2.1. Where this specification and the
constitution appear to differ, the constitution wins and the difference is a defect in this document.

## Clarifications

### Session 2026-09-09

Six decisions were put to the Architect before this document was written, followed by clarifications on
the written draft. All are binding.

- Q: How is a span represented? → A: Both forms, in separate typed columns: a 0-based character offset
  plus length (the compiler's native span) AND a 1-based start line plus 1-based start column. The
  Article VI (C) tiebreak proximity is measured on the offset, not the line.
- Q: What text goes into a hash? → A: Token text only. All trivia — whitespace, line endings, comments,
  XML documentation — is excluded. The symbol's own identifier token is excluded from its own hash, so a
  rename does not alter it and Article VI (B) can fire.
- Q: How far into a body do `uses` edges reach? → A: Declaration signatures (parameter, return, field,
  property and event types) plus explicit local `As` clauses inside bodies. Casts, generic arguments,
  `TypeOf`/`GetType` operands and expression result types are not `uses`.
- Q: Are compiler-generated symbols written? → A: No. Every implicitly declared symbol (implicit default
  constructor, auto-property backing field, the synthesized pair behind a `WithEvents` declaration, the
  synthesized delegate type behind an `Event` declared without one) is skipped. A `calls` edge whose
  target would be an implicit constructor targets the containing type instead.
- Q: How does an edge refer to a symbol that is not declared in the solution's source? → A: Every edge
  carries a nullable `target_symbol_id` and an always-present `target_doc_comment_id`. When the target
  is source-declared, both are set; when it is external (framework, package, metadata), only the
  documentation-comment id is set. The symbol registry stays source-only.
- Q: What is the target of a `handles` edge that resolves through a `WithEvents` member? → A: The event
  itself (for `Handles Button1.Click`, the event `Click` on the type of `Button1`), plus a nullable
  `via_symbol_id` naming the `WithEvents` member (`Button1`). The I3 count is per `Handles` item: a
  handler with ten items yields ten edges, each with its own `via_symbol_id`. `Handles MyBase.X` and
  `AddHandler` edges leave `via_symbol_id` null.
- Q: When one of two identical-body overloads is renamed, is a rename candidate written? (I7) → A: I7 is
  rewritten to the case Article VI (C) actually addresses. The fixture gains two non-overloaded methods
  with identical signatures and identical bodies differing only in name; I7 renames both in one run, so
  two retired rows satisfy (B) for each new symbol. Candidates are written with offset-proximity
  evidence and rank, and the test asserts the ranks. No constitution change.
- Q: What identifies a solution within the map across runs? → A: The solution file's name without
  extension, by default; an optional `--solution-key <name>` argument overrides it when two solutions
  would otherwise collide. The key survives adding or removing projects and moving the checkout. The
  `solutions` row is surrogate id + unique key + name; `repo_root` and the last-seen path are labels
  refreshed each run, never part of the key — the surrogate id is what consumers join on, and the
  solution is allowed to move. `GameRoom.sln` and `GameRoom.vbproj` keying to the same `GameRoom` is a
  feature, not a collision.
- Q: What makes the dirty-tree flag true? → A: Compiled inputs differ from HEAD: any compiled source
  document, project file, or the solution file is modified, staged, or untracked. A change to any other
  file (README, notes, ignored output) leaves the flag false. The source digest stays the primary
  provenance and the flag stays informational; the set of compiled inputs is the same set the digest is
  computed over, so one enumeration feeds both — one door, not two lists that drift.
- Q: What is the stable identity of a project symbol? → A: The synthetic id
  `Project:<project file name without extension>`. It survives folder moves and assembly-name changes;
  renaming the project file retires the old row and mints a new one.
- Q: Is there a size or time bound for real solutions in this feature? → A: No. The fixture's
  60-second target (SC-010) is the only timing criterion, and it is a test-loop budget rather than a
  scale claim — it keeps Red/Green cycles fast and underpins loading the fixture once per collection.
  The acceptance runner reports elapsed time so real-solution performance is observed, not gated;
  in-memory staging is acceptable.

## User Scenarios & Testing *(mandatory)*

Actors: the **Operator**, who runs the extractor against a solution; the **Architect**, who reviews the
map and accepts or rejects rename candidates; the **Consumer**, who later queries the file (its needs
shape what is written, but no consumer is built here).

### User Story 1 - First extraction of a compiled solution (Priority: P1)

An Operator points the extractor at a solution that compiles and at a path for the map file. The
extractor creates the file with its full schema and identity if absent, extracts every source-declared
symbol, every declaring part, and every relationship the verb rules define, and publishes them under one
run stamp. The Operator sees one summary line and exit code 0.

**Why this priority**: This is the product. Every other story refines or protects what this one writes.

**Independent Test**: Run the extractor once against the fixture solution into a fresh map file. Query
the file: the stamp, symbols, parts, edges and counts are all present and correct; nothing else is
needed for the file to be useful.

**Acceptance Scenarios**:

1. **Given** no file at the `--db` path and a fixture solution that compiles, **When** the extractor
   runs, **Then** the file exists with the full schema, one `map_identity` row, one `solutions` row, one
   completed `extract_runs` row, and symbols/parts/edges for the fixture; exit code is 0.
2. **Given** the fixture, **When** the run completes, **Then** every `Handles` item and every
   `AddHandler` statement in the fixture has exactly one `handles` edge, the total equals the count in
   source, and no handler method has zero incoming edges (I3).
3. **Given** the fixture, **When** the run completes, **Then** every edge row has a non-null path and
   span, and that span lies on a line containing the referenced identifier (I12).
4. **Given** two fresh map files, **When** the same fixture is extracted into each, **Then** the two
   logical fact sets are identical — symbols keyed by documentation-comment id with their kind, name,
   container, path, spans and hash; parts; edges; and the ten counts — differing only in surrogate ids,
   run ids, timestamps and `map_identity` (I2).
5. **Given** the fixture, **When** the run completes, **Then** both residuals are zero (I8, first half).

---

### User Story 2 - The extractor refuses rather than corrupts (Priority: P2)

An Operator runs the extractor under adverse conditions: the solution does not compile; another
extractor already holds the file; the run's own counts do not reconcile; the process is interrupted
mid-run. In every case the previously published map is untouched, and the Operator learns why from a
distinct exit code.

**Why this priority**: A wrong map is worse than no map. The constitution (Articles V and VIII) makes
these refusals non-negotiable, and every later story assumes them.

**Independent Test**: Each refusal is one test with a before/after comparison of the map file or of its
published tables. Each can run without any of the reconciliation behaviour in Story 3.

**Acceptance Scenarios**:

1. **Given** a copy of the fixture with an injected compile error, **When** the extractor runs, **Then**
   it prints the diagnostics, exits with code 2, and the map file is byte-identical before and after
   (I1).
2. **Given** an extractor holding the lock on a map file, **When** a second extractor starts against the
   same file, **Then** the second exits with code 3 without waiting and without writing (I10).
3. **Given** a staging count deliberately corrupted through a test seam, **When** validation runs,
   **Then** the extractor exits with code 4, the published tables are unchanged, and a failed
   `extract_runs` row records the non-zero residual (I8, second half).
4. **Given** a test seam that aborts the process after staging completes and, separately, one that
   aborts during publication, **When** the run is aborted at either point, **Then** the published tables
   equal the previous run's exactly (I9).

---

### User Story 3 - Identity survives re-extraction (Priority: P3)

An Operator re-runs the extractor after the source changes. Symbols that are still there keep their ids;
a symbol that vanished is retired but its row stays; a symbol that looks like a rename of a retired one
is minted a new id and a rename candidate is written for the Architect to judge. A second run over
unchanged source changes nothing but observation columns.

**Why this priority**: Anything a Consumer later anchors to a symbol id depends on this. It is P3 only
because a single run (Story 1) is already valuable and Story 2 must hold first.

**Independent Test**: Run the fixture, edit it in a defined way, run again, and compare registry rows
by id.

**Acceptance Scenarios**:

1. **Given** a map after one run, **When** the unchanged fixture is extracted again, **Then** every
   symbol keeps its id, `first_seen_run_id` is unchanged, `last_seen_run_id` advances, and the counts show
   every symbol matched with zero new and zero retired (I5).
2. **Given** a map after one run, **When** a method in the fixture is renamed and the fixture is
   extracted again, **Then** the old row has `is_active = 0` with its id intact, a new row is minted,
   exactly one `rename_candidates` row names both ids, no other registry row changes identity or active
   state, and a third run over the renamed source does not create a second candidate (I6).
3. **Given** a map after one run, **When** only the designer part of the Form is edited (a token-level
   change, not a comment or whitespace change) and the fixture is extracted again, **Then** the Form
   symbol's `body_hash` changes and the `part_hash` of its non-designer part does not (I4).
4. **Given** a map after one run and a fixture containing two non-overloaded methods with identical
   signatures and identical bodies differing only in name, **When** both are renamed in one run and the
   fixture is extracted again, **Then** both old rows are retired with ids intact, two new ids are
   minted, each new symbol has exactly two `rename_candidates` rows — one per retired row — each carrying
   offset-based proximity evidence and a rank, the nearer retired row ranks first, and no candidate is
   applied to the registry (I7).
5. **Given** a map after one run, **When** the extractor runs again, **Then** the `map_identity` GUID is
   unchanged (I14).
6. **Given** a map after run 1 containing method X, **When** X is renamed to Y and the fixture is
   extracted (run 2), then Y is renamed back to X and the fixture is extracted (run 3), **Then** after
   run 3 X's original row is active again with its original id and its `first_seen_run_id` unchanged,
   Y's row is retired, `symbols_reactivated = 1`, no new row is minted, and no rename candidate is
   written in run 3 (I15).

---

### User Story 4 - One file, many solutions (Priority: P4)

An Operator extracts a second, unrelated solution into the same map file. The first solution's rows are
untouched, ids never collide, and each solution's facts are scoped by its own `solution_id`.

**Why this priority**: The constitution (Article IX) requires it, and the Consumer's later value grows
with every solution mapped. It depends on Stories 1 and 3 being correct.

**Independent Test**: Extract fixture A, snapshot A's rows, extract fixture B, compare.

**Acceptance Scenarios**:

1. **Given** a map holding fixture A, **When** fixture B is extracted into the same file, **Then** every
   row belonging to A is byte-identical to its snapshot, and no id in any table is shared between A and B
   (I11).

---

### User Story 5 - Operator acceptance against a real solution (Priority: P5)

An Operator sets an environment variable naming a real solution. A Skip-armed runner extracts it and
prints the summary line plus: `handles` edges written versus `Handles` items and `AddHandler` statements
counted by an independent syntax walk; partial types found; both residuals. Without the variable, the
runner is skipped.

**Why this priority**: It is the only check against a solution the fixture does not anticipate. It runs
only when an Operator arms it and never gates the build.

**Independent Test**: Set `CODEMEM_ACCEPT_SOLUTION` to a path, run the test suite, inspect the printed
report; unset it and confirm the runner reports Skipped.

**Acceptance Scenarios**:

1. **Given** `CODEMEM_ACCEPT_SOLUTION` unset, **When** the suite runs, **Then** the acceptance runner is
   reported as Skipped, not passed and not failed.
2. **Given** `CODEMEM_ACCEPT_SOLUTION` naming a compiling solution, **When** the suite runs, **Then** the
   runner prints the summary line, the `handles` written-versus-counted comparison, the number of partial
   types found, both residuals, and the elapsed wall-clock time of the run.

---

### Edge Cases

- A symbol whose only declaration is in a file the compiler excluded from the build (not in the
  compilation's documents): not a source-declared symbol of this compilation; not written.
- A partial type declared across three or more files, including one in a linked file outside the
  project directory: one symbol row, one part per declaring reference, `body_hash` over all parts in
  (path, span) order.
- A `Handles` clause naming an event on a `WithEvents` member declared on a base class: one edge per item,
  target is the event on the member's declared type, `via_symbol_id` is the base-class member when that
  base class is source-declared in this solution, and null when the base class is external (the member
  then has no row, per Clarification Q5's nullable rule).
- An `AddHandler` whose delegate target is a lambda or a delegate-typed expression rather than a method
  reference: no source method to draw the edge from; not written.
- A `Handles` item or `AddHandler` whose event or target the compiler cannot resolve (error-free but
  candidate-ambiguous, or bound to an error symbol): not written, consistent with the `calls` rule.
- `Imports` supplied at project level rather than in a file: there is no statement occurrence to carry a
  path and span; not written.
- The project's root namespace and the global namespace: not declared by any statement; not rows.
  `part_of` edges from top-level types carry the namespace's documentation-comment id only.
- Two symbols in one solution with the same documentation-comment id (the compiler reports this as an
  error for duplicate definitions; if it ever occurs in an error-free compilation, e.g. via conditional
  compilation): the run refuses before reconciliation — exit code 1, both locations printed, nothing
  written — rather than guessing which row is which. This is not a residual mismatch and does not use
  exit code 4. Namespaces are merged before this check and never trigger it.
- A solution whose git working tree is absent, or whose repository cannot be read: commit sha and dirty
  flag are null; the source digest is still present.
- The map file exists but was created by a newer or older schema version: the run refuses and exits
  non-zero without writing; schema migration is not part of this feature.
- The map file path is on a location the extractor cannot lock (read-only medium, unsupported file
  system): the run refuses with exit code 3 semantics — reports and exits without writing.
- A second extractor started while the first is inside its publication transaction: exits 3 immediately;
  the first's publication completes or rolls back atomically regardless.
- Cancellation (Ctrl+C) at any point before publication: nothing published; previous snapshot stands.
- An empty solution (compiles, no source-declared symbols): a completed run with all counts zero, both
  residuals zero, and a stamp; this is a valid, publishable run.

## Requirements *(mandatory)*

### Functional Requirements

**Invocation and inputs**

- **FR-001**: The extractor MUST accept `--solution <path>` naming a `.sln` or `.vbproj` and `--db <path>`
  naming the map file, plus optional `--configuration Debug|Release` (default Debug),
  `--framework <tfm>` (default: the project's first target framework), and `--solution-key <name>`
  (default: the solution file's name without extension; see FR-032).
- **FR-002**: When the `--db` file is absent, the extractor MUST create it with the full schema and one
  `map_identity` row holding a freshly minted GUID; when present, it MUST open it and MUST NOT rewrite
  `map_identity`.
- **FR-003**: The extractor MUST open no database in any role other than the map named by `--db`
  (Article IX).

**Compilation gate and provenance**

- **FR-004**: The extractor MUST obtain a compilation for every project in the solution and, if any
  diagnostic of severity Error exists in any project, MUST print those diagnostics, exit with code 2, and
  write nothing — including no `extract_runs` row (Article V, I1).
- **FR-005**: The extractor MUST enumerate the *compiled inputs* of a run exactly once — every compiled
  source document, every project file of the solution, and the solution file — and MUST compute the
  source digest as SHA-256 over each input's solution-relative path and text, ordered by path, with
  line endings normalized (see Assumptions). The digest is the primary provenance of a run and MUST be
  recorded on every run. The same enumeration feeds FR-006; there is one list, not two (Article XII).
- **FR-006**: When the solution lies inside a git working tree, the extractor MUST record the HEAD commit
  sha and a dirty-tree flag obtained in-process (never by launching an external git process); otherwise
  both MUST be null. The flag MUST be true exactly when any compiled input (the FR-005 enumeration) is
  modified relative to HEAD, staged, or untracked; changes to any other file MUST NOT set it. The flag
  is informational: the digest covers every compiled input regardless of git's opinion, so the flag can
  be honest without having to be complete. The flag's true/false semantics are untested by
  construction: producing a modified compiled input inside a git working tree would dirty the
  repository under test, so only non-null-when-a-repository-exists is asserted — the same class as the
  duplicate-doc-comment-id guard.

**Symbols**

- **FR-007**: The extractor MUST write one `code_symbols` row per symbol declared in the solution's source
  of these kinds: namespace, class, module, structure, interface, enum, enum member, delegate, method,
  constructor, property, field, event; plus one row of kind `project` per project in the solution,
  whose `doc_comment_id` is the synthetic id `Project:<project file name without extension>`, located
  at the project file (offset 0, length 0, line 1, column 1), with a null container. A namespace is
  one row per solution regardless of how many projects declare it; its parts are every `Namespace`
  block in every project, and its primary declaration is the first part in (path, span) order.
- **FR-008**: The extractor MUST NOT write any implicitly declared symbol (Clarification Q4).
- **FR-009**: Each symbol row MUST carry: `solution_id`, `doc_comment_id` (the compiler's
  documentation-comment id), `kind`, `name` (the VB surface name; `New` for constructors), `container_id` (the declaring symbol's row, null when the
  container has no row), `project_symbol_id` (the row of kind `project` for the compilation that
  declared the symbol; null exactly for rows of kind `namespace` and `project`, which span or are a
  project), primary declaration path and span (Clarification Q1: offset, length, start line, start
  column), `body_hash`, `is_active`, `first_seen_run_id`, `last_seen_run_id`.
- **FR-010**: The primary declaration of a symbol with several declaring references MUST be the first in
  (path, span) order, so that it is the same on every run.
- **FR-011**: A `WithEvents` declaration MUST be written as exactly one symbol — the source-declared
  member that owns the declaring reference — never as its synthesized pair.

**Parts and hashes**

- **FR-012**: The extractor MUST write one `code_parts` row per declaring syntax reference of each written
  symbol, carrying `symbol_id`, path, span (as in FR-009) and `part_hash`.
- **FR-013**: `part_hash` MUST be computed over the part's token text only — all trivia excluded, and the
  symbol's own identifier token excluded (Clarification Q2). For a declaration statement with several
  declarators (`Dim a, b As Integer`), every declarator's identifier token is excluded from every
  sibling's hash, not only the symbol's own: renaming `b` MUST NOT change `a`'s identity.
- **FR-014**: `body_hash` MUST be the hash over the symbol's parts' hash inputs (the token text each
  `part_hash` was computed over) concatenated in (path, span) order, so that an edit to any one part — including a designer partial — changes it, and
  an edit to one part leaves the other parts' `part_hash` values unchanged (I4).

**Edges**

- **FR-015**: The extractor MUST write one `code_edges` row per occurrence, carrying `solution_id`, the
  source symbol's row, the verb, `target_symbol_id` (nullable), `target_doc_comment_id` (always present),
  `via_symbol_id` (nullable, `handles` only), and the occurrence's path and span (Clarification Q6, Q5).
- **FR-016**: The source of an edge whose occurrence lies inside a property accessor, a field or property
  initializer, or a lambda MUST be the nearest enclosing symbol that has a row (the property, the field,
  the enclosing method).
- **FR-017**: The extractor MUST emit exactly these verbs and no others, each by its rule:
  - `part_of`: symbol → its containing symbol.
  - `calls`: method, constructor or property (via its accessor) → the symbol the compiler binds for an
    invocation, object-creation or member-access expression. Unresolved or candidate-ambiguous bindings
    are not written. An implicit-constructor target is redirected to the containing type (Q4).
  - `uses`: symbol → a type named in its parameter, return, field, property or event declaration, or in
    an explicit local `As` clause in its body (Q3).
  - `implements`: type → each interface it lists; member → each interface member named in its
    `Implements` clause.
  - `extends`: type → its base type from `Inherits`.
  - `imports`: each top-level type in a file → each namespace named by an `Imports` statement in that
    file.
  - `depends_on`: project symbol → project symbol, one per project reference.
  - `handles`: from a `Handles` clause, one edge per `Handles` item, method → event, with
    `via_symbol_id` = the `WithEvents` member when the item names one; and from an `AddHandler` statement
    whose delegate target is a method reference, target method → the event the expression binds to,
    `via_symbol_id` null. Both are edges of the same verb (Q5).
- **FR-018**: Every symbol row (project rows excepted — their span is the project file itself) and
  every edge row MUST carry a non-null path and span whose text is exactly the surface name of what
  the row evidences: a symbol's own `name` (`New` for constructors); for `part_of`, the **source**
  symbol's name; for `calls`, `uses`, `implements`, `extends`, `imports` and `handles`, the target's
  surface name (the VB keyword for a special type such as `Integer`, the type name for a constructor,
  generic arity and parameter lists stripped); for `depends_on`, the referenced project's file name
  (I12, Article VII).

**Reconciliation**

- **FR-019**: The extractor MUST reconcile observed symbols against the registry rows of the solution
  being extracted only — every step below is scoped by `solution_id` — in the Article VI precedence:
  (A) identity match on `solution_id` + `doc_comment_id` against an active row keeps the id and
  refreshes name, container, project, location, hash and `last_seen_run_id`; (A′) reactivation: an
  observed symbol with no (A) match whose identity equals a retired row, where no active row carries
  that identity, reactivates that row — `is_active = 1`, id kept, the same columns refreshed,
  `first_seen_run_id` unchanged; where several retired rows carry the identity, the most recently
  retired is reactivated; any observed symbol without an (A) or (A′) match is minted a new id with
  `first_seen_run_id` = this run; any active row of this solution not observed is marked `is_active = 0`
  and stays. Rows of other solutions are neither read for matching nor retired (FR-031).
- **FR-020**: For each new symbol, the extractor MUST evaluate Article VI (B): if a row retired in this
  run has the same kind, the same container and an equal `body_hash`, a `rename_candidates` row MUST be
  written naming the retired id, the new id and the evidence. The registry MUST NOT be altered by a
  candidate; acceptance is a separate human operation outside this feature.
- **FR-021**: Where more than one retired row satisfies (B) for one new symbol, the extractor MUST write
  the candidates with the offset-based proximity of their primary declarations as evidence and a rank
  (Article VI (C), Q1). Proximity alone MUST NOT create a candidate.
- **FR-022**: `rename_candidates` rows MUST be append-only across runs; a candidate is written only in the
  run in which its retired row was retired, so a later run over the same source writes no duplicate (I6).
- **FR-023**: Ids in every table MUST be assigned by autoincrement and MUST never be reused; no code path
  MAY delete from, drop, recreate, or cascade a delete into `code_symbols` (Article VI, I13).

**Counts and validation**

- **FR-024**: Every `extract_runs` row MUST record the ten counts of Article VIII: `symbols_observed`,
  `symbols_matched`, `symbols_reactivated`, `symbols_new`, `symbols_retired`, `registry_active_before`,
  `notes_orphaned` (written as 0 until a notes table exists), `rename_candidates`,
  `unaccounted_observed` and `unaccounted_registry`. Every count is scoped to the solution being extracted; in particular
  `registry_active_before` is the number of active rows carrying this run's `solution_id` before
  reconciliation, never the whole registry.
- **FR-025**: The two residuals MUST be computed by code independent of the reconciliation logic, from the
  other counts alone, and written even when zero: `unaccounted_observed = symbols_observed −
  (symbols_matched + symbols_reactivated + symbols_new)`; `unaccounted_registry =
  registry_active_before − (symbols_matched + symbols_retired)` — reactivated rows were not active
  before, so the registry-side formula is unchanged.
- **FR-026**: If either residual is non-zero, the extractor MUST exit with code 4, MUST NOT modify any
  published table, and MUST write one `extract_runs` row marked failed carrying the counts (I8).

**Publication and concurrency**

- **FR-027**: The extractor MUST take an exclusive extractor lock on the map before extraction; a second
  extractor finding the lock held MUST exit with code 3 immediately, without waiting and without writing
  (I10).
- **FR-028**: The extractor MUST extract and reconcile into a staging area that is not visible to readers,
  validate (FR-024–FR-026), and only then publish.
- **FR-029**: Publication MUST occur in one transaction that, in order: inserts the `extract_runs` row;
  applies registry updates (refresh matched, reactivate, insert new, retire unobserved); replaces `code_parts` and
  `code_edges` for this `solution_id` only; inserts `rename_candidates`. A reader MUST observe either the
  previous completed run or the new one, never an intermediate state (I9).
- **FR-030**: Any failure before or during publication — extraction error, count mismatch, cancellation,
  database error, process abort — MUST leave every published table equal to the previous run's.

**Multi-solution scope**

- **FR-031**: Every fact row MUST carry `solution_id`, and the extractor's write scope for a run MUST be
  exactly the rows of the solution being extracted; rows of other solutions MUST be byte-identical
  before and after (I11).
- **FR-032**: The `solutions` row MUST be keyed by the solution key: the solution file's name without
  extension unless `--solution-key` is given. Repeat runs with the same key MUST reconcile against the
  same registry regardless of checkout location or project membership; runs with a different key MUST
  be separate solutions and MUST NOT touch each other's rows. `repo_root` and the last-seen solution
  path are labels refreshed on every run and MUST NOT participate in the key. A `.sln` and a `.vbproj`
  sharing a base name (`GameRoom.sln`, `GameRoom.vbproj`) key to the same solution `GameRoom` by design.

**Output and exit codes**

- **FR-033**: On success the extractor MUST print exactly one line containing: solution, run id, the nine
  counts, the source digest, and the commit sha or `null`; and MUST exit 0.
- **FR-034**: Exit codes MUST be: 0 success; 2 compilation errors; 3 lock held or unobtainable (an
  existing but unwritable or read-only location); 4 residual mismatch; 1 for any other failure (usage
  error, a `--db` path whose directory does not exist, unreadable solution, schema-version mismatch,
  database error). No failure MAY exit 0.

**Fixture and tests**

- **FR-035**: A fixture solution MUST be committed under the test project's `Fixtures/` directory
  containing at minimum: a WinForms Form with a `.Designer.vb` partial and `WithEvents` controls; three
  `Handles`-clause handlers, one of which handles two events; one `AddHandler` wiring; two method
  overloads with identical bodies; two non-overloaded methods with identical signatures and identical
  bodies that differ only in name; an interface with two implementers; an `Inherits` chain of two; an
  `Imports` statement; and two projects joined by a project reference. Both projects MUST share a root
  namespace so that the per-solution namespace merge (FR-007) is exercised by I2 and I5.
- **FR-036**: The fixture MUST be loaded once per test collection and shared read-only; tests that mutate
  source (I1, I4, I6, I7, I9, I15, and any other test that edits source) MUST operate on a copy.
- **FR-037**: Each invariant I1–I15 MUST be one test, written Red first per Article II, run against real
  SQLite and the real compiled fixture per Article III, with no mock of the compiler or the database.
- **FR-038**: The I13 tripwire MUST first assert a positive count of INSERT/UPDATE statements in the Core
  repository source and then assert zero occurrences of each destructive form reaching `code_symbols`:
  DELETE, DROP, CREATE TABLE after DROP, ON DELETE CASCADE.
- **FR-039**: An acceptance runner MUST exist that, when `CODEMEM_ACCEPT_SOLUTION` is set, extracts that
  solution and prints the summary line plus `handles` edges written versus `Handles` items and
  `AddHandler` statements counted by an independent syntax walk, partial types found, both residuals,
  and the elapsed wall-clock time of the run; and when unset, reports Skipped. It MUST be declared in
  the plan's Carve-Out Register as a Skip-armed runner. Elapsed time is reported, never gated.

### Key Entities *(include if feature involves data)*

Column sets follow constitution Articles V–IX; the DDL is proposed in the plan, not here.

- **map_identity**: One row. A GUID minted when the file is created and never rewritten. Identifies the
  map itself across copies and moves.
- **solutions**: One row per solution key ever extracted into this map. Surrogate `id` — the column
  downstream consumers (MemOS) join on, stable for the life of the row; `key` (unique: file name
  without extension, or the `--solution-key` override); `name`; and two labels refreshed on every run
  and never part of the key: `repo_root` and the last-seen solution path. A solution is allowed to
  move. Created timestamp; first completed run id (null until the first completed publication).
- **extract_runs**: One row per attempted run that reached validation. The stamp: solution, source
  digest (always), commit sha and dirty flag (nullable), build configuration, target framework, extractor
  version, schema version, timestamp; the ten counts; an outcome (completed / failed).
- **code_symbols**: The identity registry. One row per source-declared symbol per solution, plus one per
  project. Identity is `solution_id` + `doc_comment_id`; ids are never reused; rows are retired, never
  deleted, and a retired row is reactivated when its identity reappears. Carries kind, name,
  container, declaring project (null for namespace and project rows), primary declaration location,
  `body_hash`, `is_active`, first/last seen run.
- **code_parts**: Observation table, replaced per run per solution. One row per declaring reference of a
  symbol: location and `part_hash`.
- **code_edges**: Observation table, replaced per run per solution. One row per occurrence: source
  symbol, verb, target (nullable row id + always-present documentation-comment id), `via_symbol_id` for
  `handles`, occurrence location.
- **rename_candidates**: Append-only proposals. Retired id, new id, run id, evidence (hash equality;
  offset proximity and rank when applicable). Never applied by the extractor.

Location, wherever it appears, is: path (solution-relative, forward slashes), start offset (0-based),
length, start line (1-based), start column (1-based).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A solution with a compile error leaves the map file byte-identical, 100% of the time, with
  exit code 2 and the diagnostics printed.
- **SC-002**: Two extractions of the same source into two fresh maps produce identical logical fact sets
  on every attempt (0 differing rows outside surrogate ids, run ids, timestamps and map identity).
- **SC-003**: 100% of `Handles` items and method-referencing `AddHandler` statements in the fixture yield
  exactly one `handles` edge; 0 handler methods have zero incoming edges.
- **SC-004**: 100% of edge rows and non-project symbol rows carry a span whose text equals the surface
  name of what the row evidences (FR-018); 0 rows lack a path or span.
- **SC-005**: Across a rename of one fixture method, exactly 1 rename candidate is written, exactly 1 row
  is retired, exactly 1 row is minted, and 0 other rows change identity or active state.
- **SC-006**: Both residuals are 0 on every clean run; a corrupted count is detected 100% of the time with
  exit 4 and 0 published rows changed.
- **SC-007**: An interrupted run — at any injected abort point — leaves 0 published rows changed.
- **SC-008**: A second lock attempt against a held map is refused in under 1 second without waiting on
  the first (measured in-process); the second extractor process exits with code 3 within 2 seconds
  including runtime start-up.
- **SC-009**: Extracting a second solution leaves 100% of the first solution's rows byte-identical, and 0
  ids are shared between solutions.
- **SC-010**: Extracting the fixture solution into a fresh map completes in under 60 seconds on a
  developer workstation. This is a test-loop budget, not a scale claim: it exists so Red/Green cycles
  under Article II stay fast, and it is the reason the fixture is loaded once per collection (FR-036),
  a decision carried forward into the plan.
- **SC-011**: The `map_identity` GUID is identical after 2, 3 and N runs.
- **SC-012**: The I13 tripwire reports a positive number of write statements and 0 destructive
  statements reaching `code_symbols`, and is shown to go red when a DELETE against `code_symbols` is
  injected (Article II fire demonstration).
- **SC-013**: A rename followed by a revert leaves the symbol on its original id: exactly 1
  reactivation, 0 new rows, 0 candidates in the reverting run.

## Assumptions

- **Paths are solution-relative with forward slashes.** Every stored path is relative to the solution
  file's directory and uses `/`, so the same source checked out at two locations or on two machines
  yields the same fact set (Article IV). A linked file outside the solution directory is stored with
  `..` segments.
- **Source digest normalization.** The digest covers each compiled input's solution-relative path and
  its text with line endings normalized to LF, so a CRLF and an LF checkout of the same commit share a
  digest. Byte-order marks are excluded. The plan fixes the exact concatenation. Because project and
  solution files are compiled inputs, a change to a build option (e.g. Option Strict) changes the
  digest even when no source document changed — which is correct, since it changes the compilation.
- **I2 compares two fresh maps, not two runs into one map.** A second run into the same map necessarily
  differs in `last_seen_run_id` and in the counts (all matched rather than all new); those are I5's
  concern. Article IV's determinism claim is read as: same input into empty state, same fact set.
- **A project file rename is a project rename.** Because a project's identity is its file name
  (Clarifications), renaming `Foo.vbproj` to `Bar.vbproj` retires the `Project:Foo` row and mints
  `Project:Bar`; moving the file to another folder or changing its assembly name does not. The
  `Project:` prefix cannot collide with a compiler-issued documentation-comment id, which is always a
  single letter followed by a colon.
- **Solution identity is the Operator's responsibility when names collide.** The default key (file name
  without extension) is right for the common case; two distinct solutions sharing a file name must be
  told apart with `--solution-key`, and the extractor does not detect the collision on its own. A
  `.sln` and its same-named `.vbproj` are not a collision: they are the same solution, extracted at
  different scopes, and share one registry.
- **Accessor and initializer attribution.** Property accessors, field/property initializers and lambdas
  are not symbol rows; an occurrence inside one is attributed to the nearest enclosing symbol that has a
  row (FR-016). This is why the `calls` rule says "property (via its accessor)".
- **`rename_candidates` is append-only and never cleaned.** Nothing in this feature deletes or resolves a
  candidate; acceptance is a later, human-initiated operation.
- **Failed runs are recorded; refused runs are not.** A run that fails validation (exit 4) writes a
  failed `extract_runs` row so the failure is visible. A run refused before extraction — compile errors
  (exit 2), lock held (exit 3), usage or schema errors (exit 1) — writes nothing at all.
- **A refused run may leave a freshly created map file.** The map is opened — and, when absent,
  created with its schema and identity — before compilation, so the lock is taken before any long
  work and a second extractor is refused within SC-008's budget. A run refused after that point
  (compile errors, schema mismatch) leaves an empty-schema file at a new `--db` path; it never writes
  a run or fact row.
- **Exit code 1 for everything not otherwise specified.** The feature names 0, 2, 3 and 4; any other
  failure exits 1. No failure exits 0.
- **Schema version mismatch is a refusal, not a migration.** A map created by a different schema version
  is refused with exit 1; migration is a later feature.
- **Locking is file-based and exclusive.** The lock protects one map file; two extractors against two
  different maps do not interact. The lock is released on any exit, including abort.
- **Documentation-comment ids are unique within a solution's compilation.** The compiler guarantees this
  for error-free code in ordinary cases; the run fails validation if a duplicate is ever observed rather
  than choosing a row.
- **The overload pair in the fixture still earns its place.** With the token-hash rule, two overloads
  never share a hash (their parameter lists differ), so renaming one yields exactly one candidate, as in
  I6. The pair remains in the fixture to demonstrate that; the ambiguity case I7 now uses two
  non-overloaded, identically-signed methods (Clarifications, I7).
- **No solution-size or run-time bound applies in this feature.** Correctness is the deliverable
  (Clarifications). The only timing target is SC-010 for the fixture; real-solution timing is observed
  through the acceptance runner's elapsed-time line, not gated. Consequently in-memory staging is
  acceptable for this feature, and a later feature sets performance targets from observed numbers.
- **A symbol's project is compiler fact — the compilation that declared it — recorded as a column.**
  `part_of` follows `ContainingSymbol` and ends at the namespace; two `part_of` edges from one source
  would break that rule, so the declaring project is `project_symbol_id` on the row, not a verb.
  Namespaces and project rows carry null: a namespace spans projects, a project is one.
- **The test framework, hashing algorithm for `body_hash`/`part_hash`, and lock mechanism are plan-level
  choices** constrained by Articles III, XI and XV; the spec fixes only their observable behaviour.
- **Known limit of (B) — a renamed container blinds its members.** (B) requires the retired row and the
  new symbol to share a container. Renaming a class retires the class and every member; the members
  are minted new ids under a new container id, so their (B) comparison fails on container even though
  their hashes match, and no member candidate is written. The class itself still gets its candidate.
  Recorded as a limit, not a defect: a later feature may compare containers through the class's own
  candidate. Nothing in this feature attempts it.

## Out of Scope

- Any server, service, or MCP surface over the map.
- Any notes or prose table; `notes_orphaned` is written as 0 (Article VIII).
- Any user interface.
- The verbs `produces`, `configures`, `validates`, or any verb not listed in FR-017.
- Per-run history of observations: `code_parts` and `code_edges` hold only the latest run per solution.
- Reading or writing any database other than the one named by `--db`.
- Accepting, rejecting or applying rename candidates.
- Schema migration between versions.
- Extracting C# projects, or anything the VB compiler does not compile.

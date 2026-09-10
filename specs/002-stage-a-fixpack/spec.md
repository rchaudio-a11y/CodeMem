# Feature Specification: CodeMem Fixpack 002 — Stage A Review Corrections

**Feature Branch**: `002-stage-a-fixpack`

**Created**: 2026-09-10

**Status**: Draft

**Input**: User description: "CodeMem fixpack 002 — eight corrections from the stage A code review at
specs/001-extractor-codemem-sqlite/Reports/report.md. No new capability. Constitution v1.2.1 governs; no
amendment. Every behaviour change below is a Red-first test per Article II." (The eight items F2, F5, F6,
F9, F8, F3, F4 (cheap half), F10, the additive `sdk_version` column with a v1→v2 in-place upgrade, and the
out-of-scope list F1, F7, F4-full, F11 are reproduced in the sections below.)

**Governing document**: `.specify/memory/constitution.md` v1.2.1, unamended. Where this specification and
the constitution appear to differ, the constitution wins and the difference is a defect in this document.

**Parent feature**: `specs/001-extractor-codemem-sqlite/` (Stage A). This fixpack corrects Stage A in place;
its requirements are numbered FR-101 onward so they never collide with Stage A's FR-001–FR-039. Where a
Stage A requirement changes, the change is stated here and the Stage A number is named.

## Clarifications

### Session 2026-09-10

The feature description is the review's own recommendation list, already decided by the Architect. One
decision the description left implicit (how far the build-file walk goes on a path without a drive root)
is recorded as an Assumption. The following were put to the Architect:

- Q: What exactly should `sdk_version` record on a run? → A: The .NET SDK version resolved for the
  solution directory (honouring `global.json`), the same answer `dotnet --version` gives there, for
  example `10.0.303`; never obtained by launching a process. Whether it can be read in-process from the
  resolver data the build host uses is a research item to be **verified, not assumed**; the plan records
  the verified mechanism.
- Q: How is the rule "runs written at schema version 2 must carry `sdk_version`" enforced on a map
  upgraded from version 1, given that a cross-column CHECK cannot be added to an existing table in
  place? → A: A trigger on `extract_runs`, on **both** fresh and upgraded maps, so every map has one
  shape and a reader (MemOS attaching the map, any other tool) never has to ask whether a map was born at
  v2 or upgraded to it. The trigger is schema-owned (Article XII holds: the rule lives in the database,
  not in code) and nothing is dropped or rebuilt (Article XIV holds). Rejected: a table rebuild (needs
  foreign keys off, impossible inside the publication transaction, so a second publication mechanism for
  one column); an inline CHECK on fresh maps plus a trigger on upgraded ones (two shapes to document
  forever); code-only enforcement (what Article XII was written to stop). Rider: the trigger is part of
  the v2 DDL in `contracts/schema.sql`, so schema creation and the upgrade produce identical
  `sqlite_master` contents; and the fire demonstration inserts a v2 run row without `sdk_version`
  through a raw statement, not the repository, so what is shown to fire is the trigger.

## User Scenarios & Testing *(mandatory)*

Actors: the **Operator**, who runs the extractor; the **Architect**, who reviews the map and the review
gates; the **Consumer**, who later reads the map (unchanged by this feature except for one added column).

### User Story 1 - The registry tells the truth after a kind change, on an upgraded map (Priority: P1)

An Operator re-extracts a solution into an existing map after a type changed from one declaration kind to
another (a `Class` became a `Structure`, say). The symbol keeps its id, because its identity did not
change, and its row now reads the new kind, because the compiler fact did. The same Operator points a
new extractor at the map they already have, created under schema version 1: it opens, is upgraded in
place, extracts, and reads schema version 2 afterwards; nothing in it was deleted.

**Why this priority**: A stale `kind` is a wrong compiler fact in the registry (review F2), and the
schema change this feature needs must reach the map that already exists at `C:\_DB\codemem.sqlite`
without refusing it. Both touch what Consumers anchor to.

**Independent Test**: Run the fixture twice on a copy whose one type changes kind between runs; read the
row. Separately, take a map created by the Stage A extractor, run the new extractor against it, read
`map_identity.schema_version` and count rows before and after.

**Acceptance Scenarios**:

1. **Given** a map after one run, **When** one fixture type is changed from `Class` to `Structure` and the
   fixture is extracted again, **Then** the row keeps its `id`, its `kind` reads `structure`, and the run's
   `symbols_matched` includes it (no retirement, no new row, no candidate).
2. **Given** the map of scenario 1, **When** the same type is changed again to a third `T:` kind (an
   `Interface` or `Module`) and extracted, **Then** the row still keeps its `id` and reads the third kind.
3. **Given** a map created at schema version 1 with completed runs, **When** the new extractor runs
   against it, **Then** the run completes with exit code 0, `map_identity.schema_version` reads 2, every
   pre-existing row of every table is still present with its values, the added column reads NULL on
   pre-existing run rows, and the map's `sqlite_master` contents equal those of a fresh v2 map.
4. **Given** a map created at schema version 1, **When** the extractor is aborted (test seam) after the
   upgrade began and before it committed, **Then** the map still reads schema version 1 with all rows
   intact, and the next run upgrades and completes.
5. **Given** a map at a schema version greater than 2, **When** the extractor runs, **Then** it is refused
   with exit code 1 and nothing is written (Stage A FR-034 unchanged).

---

### User Story 2 - The extractor starts up safely on any path (Priority: P2)

An Operator names a map path that is unusual but legal (it contains a `;`), or a path that exists but is
empty (a 0-byte file), or a path whose earlier first run was interrupted mid-initialization, or a path that
is simply invalid. In every case the extractor either produces a correct map on that exact path or refuses
with exit code 1 and one line on stderr — never an unhandled exception, never a map elsewhere.

**Why this priority**: Review F5, F6 and F9 are all ways the shipped executable can behave outside its
own exit-code contract (Stage A FR-034) or open something other than the requested map (Stage A FR-003,
Article IX).

**Independent Test**: Each case is one executable run with a before/after look at the named path and the
exit code.

**Acceptance Scenarios**:

1. **Given** a `--db` path whose directory or file name contains `;`, **When** the extractor runs,
   **Then** it creates and populates the map at exactly that path, exit code 0, and no other file is
   created.
2. **Given** a 0-byte file at `--db`, **When** the extractor runs, **Then** the file is treated as a fresh
   map: schema and identity are created in it, extraction completes, exit code 0.
3. **Given** a SQLite file at `--db` that contains no user tables (an interrupted first initialization),
   **When** the extractor runs, **Then** it is treated as fresh and the run completes. **Given** a SQLite
   file that contains tables but no `map_identity`, **When** the extractor runs, **Then** it is refused
   with exit 1 naming the path and the file is byte-identical before and after.
4. **Given** a fresh path, **When** the extractor is aborted (test seam) during first-run initialization,
   **Then** the file left behind is either usable-looking and complete (full schema plus identity row) or
   recognisably not a map (no `map_identity` table), and the next run on the same path succeeds.
5. **Given** a `--db` value that is not a valid path (illegal characters), **When** the executable runs,
   **Then** it exits 1 with exactly one stderr line and nothing on stdout — no stack trace.
6. **Given** any preflight failure before the lock is taken (unreadable path, access denied, a file that
   is not a database), **When** the executable runs, **Then** exit code 1 with one stderr line; the
   existing mappings for compile errors (2), held lock (3) and residual mismatch (4) are unchanged and are
   re-proven by the existing invariants I1, I10 and I8.

---

### User Story 3 - The digest covers the build files the extractor chose (Priority: P3)

An Operator adds a `Directory.Build.props` above their solution. The next run's digest differs, because a
file that can change the compilation now counts as a compiled input, even though no source document
changed. The run also stamps which SDK compiled it. The Architect reads the spec and sees the digest
described honestly: provenance of the selected inputs, not a fingerprint of the evaluated compilation.

**Why this priority**: Review F4 found that equal digests did not imply equal compilations. The cheap half
— the four well-known build files walked upward, plus the SDK version — closes the most common gap; the
full evaluated manifest is out of scope and recorded as a known limit.

**Independent Test**: Extract a fixture copy, add an empty `Directory.Build.props` in the directory above
the copy, extract again: the digest changes and the fact set does not. Read `sdk_version` on both runs.

**Acceptance Scenarios**:

1. **Given** a fixture copy with no build files above it, **When** an empty `Directory.Build.props` is
   placed in the directory above the copy and the fixture is extracted again, **Then** the second run's
   `source_digest` differs from the first and the logical fact set (symbols, parts, edges, ten counts as
   matched) is unchanged.
2. **Given** a solution directory with `Directory.Build.props`, `Directory.Build.targets`,
   `Directory.Packages.props` and `global.json` present at various levels between it and the drive root,
   **When** the extractor runs, **Then** every one of them is a compiled input (it participates in the
   digest and in the dirty-tree flag), each recorded under its solution-relative path with `..` segments.
3. **Given** any completed or failed run, **When** its `extract_runs` row is read, **Then** `sdk_version`
   is non-empty.
4. **Given** the parent feature's FR-005 wording, **When** this feature is done, **Then** the requirement
   reads "a digest over the selected compiled inputs" and the selection rule is written down (FR-107).

---

### User Story 4 - The shipped executable cannot be crashed from the environment alone (Priority: P4)

An Operator runs the extractor on a machine where `CODEMEM_TEST_ABORT_AT` happens to be set (inherited,
copied, or malicious). The run completes normally. Only a test that also supplies a matching per-run nonce
can trigger the abort. The dependency graph the executable ships with carries no known High-severity
advisory that an explicit override can clear.

**Why this priority**: Review F8 (a test seam active in production) and F3 (two vulnerable transitive
packages) are operational-reliability and security findings; neither changes what the map contains.

**Independent Test**: One executable run with the abort variable set and the nonce absent (completes);
one with both set and matching (aborts, as I9 already proves). A package audit command whose output is
recorded.

**Acceptance Scenarios**:

1. **Given** `CODEMEM_TEST_ABORT_AT=DuringPublish` and no `CODEMEM_TEST_NONCE`, **When** the executable
   runs, **Then** it completes with exit code 0 and a published run.
2. **Given** `CODEMEM_TEST_NONCE` set and `CODEMEM_TEST_ABORT_AT` unset, **When** the executable runs,
   **Then** it completes normally.
3. **Given** both variables set and the nonce equal to the value the test passed, **When** the executable
   runs, **Then** it aborts at the named point exactly as invariant I9 requires today.
4. **Given** both set but the nonce different from the test's value, **When** the executable runs,
   **Then** it completes normally.
5. **Given** `--help`, **When** printed, **Then** both variables appear under the test-only heading with
   the rule that both are required.
6. **Given** the solution's package graph, **When** the vulnerability audit is run with transitive
   packages included, **Then** GHSA-h4j7-5rxr-p4wc and GHSA-447r-wph3-92pm are no longer reported, the
   audit output is recorded in the quickstart, and the full test suite is green. If the out-of-process
   build host still loads the older versions at run time, that is recorded as a finding in the quickstart,
   not forced.

---

### User Story 5 - The source-policy gates cannot be slipped past by spelling (Priority: P5)

The Architect relies on the tripwire and the SQL-location gate as review gates. A lowercase `delete from
code_symbols`, or one with a tab in it, must trip them exactly as the canonical spelling does. Each gate is
re-fired after the change.

**Why this priority**: Review F10 — the gates are useful but lexically bypassable. Lowest priority because
the runtime schema tests already hold the stronger behavioural line.

**Independent Test**: Inject `delete   from code_symbols` (lowercase, extra whitespace) into a repository
file → the tripwire goes red; inject `select 1` into a non-repository file → the location gate goes red;
revert both → green; record both demonstrations.

**Acceptance Scenarios**:

1. **Given** a repository file containing a destructive statement against `code_symbols` in any letter
   case with any run of whitespace between tokens, **When** the tripwire runs, **Then** it fails naming
   the form found.
2. **Given** a non-repository file containing a SQL keyword literal in any letter case, **When** the
   location gate runs, **Then** it fails naming the file.
3. **Given** the unchanged repository, **When** both gates run, **Then** both pass, and the positive
   write count the tripwire asserts first is still positive.

---

### Edge Cases

- A v1 map that another extractor currently holds: the upgrade needs the write lock; the second process
  is refused with exit 3 exactly as today, and the upgrade happens in whichever run first holds the lock.
- A v1 map with a `failed` run row: the added column reads NULL on that row too; failed rows are never
  referenced and stay unreferenced.
- A file at `--db` that is a valid SQLite database but not a map (has tables, none of them
  `map_identity`): refused, exit 1, one stderr line naming the path; nothing is written (FR-105,
  Article IX). The Operator who pointed at the wrong file is protected by the refusal, not by the absence
  of deletes.
- A file at `--db` that is not SQLite at all (random bytes): the open fails; exit 1 with one stderr line.
- Both `Directory.Build.props` in the solution directory and another above it: both are inputs, each under
  its own relative path (`Directory.Build.props` and `../Directory.Build.props`).
- A build file that is unreadable (permissions): the run refuses with exit 1 rather than silently
  omitting an input from the digest.
- A solution on a UNC or root-less path: the upward walk stops when the parent directory is null.
- `global.json` present but `sdk_version` differs from what it pins (roll-forward): `sdk_version` records
  what compiled, not what was requested; `global.json` is in the digest either way.
- Nonce values with leading or trailing whitespace: compared exactly; not equal is inert.
- A kind change combined with a rename in one run: the rename path (B) applies — the old row retires, a
  new row is minted, and a candidate requires equal kind, so no candidate is written across a kind change.
  Recorded, not a defect: (B) compares kinds by design.
- A kind change on a namespace or project row: impossible by construction (those kinds have distinct id
  prefixes); no rule needed.
- The fresh-map initialization seam fires after the schema but before the identity row: because both are
  one transaction (FR-106), this state cannot persist; the file rolls back to "no `map_identity` table"
  and is fresh on the next run. Should a file ever be found with the table present but empty (a file
  produced by other means), it is not a map and is refused (FR-105); CodeMem does not repair databases
  it did not make.

## Requirements *(mandatory)*

### Functional Requirements

**Kind refresh (F2)**

- **FR-101**: On an Article VI (A) identity match, the extractor MUST refresh `kind` together with the
  other refreshed columns (name, container, project, location, hash, last seen). Stage A FR-019's list of
  refreshed columns gains `kind`.
- **FR-102**: A kind change MUST NOT retire the row, mint a new id, or write a rename candidate; it is a
  match and is counted in `symbols_matched`.
- **FR-103**: The same refresh MUST apply on an (A′) reactivation.

**Connection string (F5)**

- **FR-104**: The extractor MUST open the map through a typed connection description in which the path is
  a value, never a fragment of delimited text, so that any legal file path — including one containing `;`
  or `=` — opens exactly that file.

**Fresh-map initialization (F6)**

- **FR-105**: A `--db` path MUST count as fresh when the file is absent, is empty (0 bytes), or is a
  SQLite file containing **no user tables at all** (the state an interrupted first initialization can
  leave behind under FR-106). A SQLite file that contains any table but no `map_identity` table, or a
  `map_identity` table with no row, is **not a map and not fresh**: the run MUST refuse with exit 1,
  naming the path, and write nothing — creating a map inside a database that is something else would
  open a database in a role other than the map (Article IX). Stage A FR-002's "absent" becomes "absent or
  demonstrably empty".
- **FR-106**: Schema creation and the identity row MUST be written inside one transaction, so that an
  interruption leaves either a complete map or a file that FR-105 still recognises as fresh; the next run
  on the same path MUST succeed.

**Compiled inputs and SDK stamp (F4, cheap half)**

- **FR-107**: The compiled-inputs enumeration (Stage A FR-005) MUST additionally include, when present,
  every `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props` and `global.json`
  found in the solution directory and in each ancestor directory up to the root of its path, each under
  its solution-relative path (with `..` segments). The one enumeration still feeds both the digest and
  the dirty-tree flag.
- **FR-108**: Stage A FR-005 is reworded: the digest is "a digest over the selected compiled inputs" —
  provenance of the files the extractor chose, not a fingerprint of the evaluated compilation. The
  selection rule is FR-107 plus Stage A's documents, project files and solution file.
- **FR-109**: Every `extract_runs` row written at schema version 2 MUST record `sdk_version`, non-empty:
  the .NET SDK version resolved for the solution directory, honouring `global.json` (the value
  `dotnet --version` reports there, e.g. `10.0.303`), obtained in-process and never by launching a
  process; when no SDK version can be resolved the run MUST fail with exit 1 rather than stamp a
  substitute (Assumptions). The column is **nullable**, because rows written before the column existed did not
  record it and NULL is the honest value (Article X: nullable over sentinel); the obligation on new rows
  is owned by the schema, not by code (Article XII): a trigger on `extract_runs` that aborts the insert
  of any row whose own `schema_version` is 2 or higher and whose `sdk_version` is NULL. The trigger is
  part of the v2 DDL in `contracts/schema.sql` and exists identically on fresh and upgraded maps
  (Clarifications, Q2).
- **FR-110**: Adding, removing or editing any FR-107 file MUST change the digest; doing so with content
  that does not alter the compilation MUST leave the logical fact set unchanged.

**Schema version 2 and in-place upgrade**

- **FR-111**: `sdk_version` is an additive column; the schema version becomes 2; a fresh map is created
  at version 2.
- **FR-112**: A map at version 1 MUST be upgraded in place, inside a transaction under the extractor's
  write lock, by adding the nullable column (existing rows read NULL), creating the FR-109 trigger, and
  setting `map_identity.schema_version = 2`; it MUST NOT be refused, and no row, table, column or index
  MAY be deleted, dropped or rebuilt (Article XIV). After the upgrade the map's `sqlite_master` contents
  MUST equal those of a map created fresh at version 2 (one shape; Clarifications, Q2). Stage A
  FR-034's "schema-version mismatch → exit 1" now applies to versions other than 1 and 2.
- **FR-113**: An interruption during the upgrade MUST leave the map at version 1 with every row intact;
  the next run MUST upgrade and complete.
- **FR-114**: A map at a version greater than 2 MUST still be refused with exit 1 and no write.

**Preflight inside the error boundary (F9)**

- **FR-115**: Option and path normalisation and the open-or-create of the map MUST execute inside the
  top-level error boundary; any failure there MUST map to exit 1 with exactly one stderr line and nothing
  on stdout. The mappings to exit 2, 3 and 4 are unchanged.

**Abort seam nonce (F8)**

- **FR-116**: `CODEMEM_TEST_ABORT_AT` MUST be honoured only when `CODEMEM_TEST_NONCE` is also set and
  equals, exactly, the value the test supplied for that run; either variable alone, or a non-matching
  nonce, MUST be inert. Stage A's contract for the abort seam is amended accordingly.
- **FR-117**: `--help` MUST document both variables and the both-required rule under the test-only
  heading; the plan's Carve-Out Register row MUST be updated to name the nonce.

**Vulnerable transitives (F3)**

- **FR-118**: The extraction library MUST pin the two named transitive packages at the lowest versions
  that clear GHSA-h4j7-5rxr-p4wc and GHSA-447r-wph3-92pm; the transitive vulnerability audit MUST report
  neither, and its output MUST be recorded in the quickstart.
- **FR-119**: If the out-of-process build host still resolves the older versions at run time, that MUST be
  recorded in the quickstart as a finding, not forced.
- **FR-120**: The full test suite MUST remain green after the pins.

**Gate patterns (F10)**

- **FR-121**: The tripwire and the SQL-location gate MUST match their patterns case-insensitively with
  runs of whitespace normalised to one space, over string literals as today.
- **FR-122**: Both gates' fire demonstrations MUST be re-run with a lowercase, oddly spaced injection and
  re-recorded beside the tests.

**Tests (Article II)**

- **FR-123**: Every behaviour change above MUST be a Red-first test against real SQLite and the real
  compiled fixture, with the Red reason recorded, per Stage A FR-037. The kind-change test MUST take one
  fixture type through two `T:` kinds across runs.

### Key Entities *(include if feature involves data)*

- **extract_runs** (changed): gains `sdk_version`, nullable, guarded by a trigger that refuses a row with
  `schema_version` 2 or higher and no `sdk_version`; the trigger is part of the v2 DDL and is identical on
  fresh and upgraded maps. Pre-existing rows in an upgraded map read NULL. All other columns unchanged.
- **map_identity** (changed value): `schema_version` reads 2 on fresh and upgraded maps; the GUID is
  untouched by an upgrade (I14 still holds).
- **Compiled input** (extended set): the Stage A set plus the four well-known build files found upward
  from the solution directory. Location, normalisation and ordering rules are Stage A's.
- **Test nonce**: a per-run secret the test supplies in `CODEMEM_TEST_NONCE`; the abort seam is armed only
  when it matches. Never stored in the map.
- **Symbol row kind**: now an observation column refreshed per run for a matched identity, alongside name
  and location; identity remains `solution_id` + `doc_comment_id`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-101**: Across a kind change of one fixture type through two further kinds, 0 rows change id, 0 rows
  are retired, 0 candidates are written, and the row's kind equals the compiler's kind after every run.
- **SC-102**: A map created at schema version 1 with N rows per table has exactly N rows per table after
  the upgrading run (plus that run's own rows), and reads version 2; 100% of pre-existing values are
  unchanged.
- **SC-103**: 100% of the following start-up cases end in exit 0 with a correct map at the named path or
  exit 1 with one stderr line: path with `;`, 0-byte file, file without `map_identity`, invalid path,
  unreadable path. 0 unhandled exceptions from the executable.
- **SC-104**: An interruption during first-run initialization or during the upgrade is followed by a
  successful run on the same path 100% of the time.
- **SC-105**: Adding an empty `Directory.Build.props` above the fixture changes the digest on 100% of runs
  and changes 0 rows of the logical fact set.
- **SC-106**: 100% of run rows written at schema version 2 carry a non-empty `sdk_version`; 100% of
  rows written at version 1 read NULL after upgrade; a v2 run row inserted without it through a raw
  statement (not the repository) is refused by the trigger, on a fresh map and on an upgraded map alike
  (fire demonstration); and the `sqlite_master` contents of a fresh v2 map and an upgraded map are equal.
- **SC-107**: With the abort variable set and no nonce, 100% of runs complete; with both set and matching,
  the abort fires as I9 requires.
- **SC-108**: The transitive vulnerability audit reports 0 High-severity advisories for the two named
  packages, and the suite passes with the same count as before plus this feature's new tests.
- **SC-109**: Both gates go red on a lowercase, oddly spaced injection and green on revert; both fire
  demonstrations are recorded.

## Assumptions

- **`sdk_version` is the resolved SDK, not the runtime.** The stamp is the .NET SDK version the SDK
  resolver selects for the solution directory (Clarifications, Q1). It is never empty and never a
  placeholder such as `unknown`, and it is never the extractor's own runtime description; if no SDK
  version can be resolved the run fails with exit 1, because a stamp that cannot say what compiled is
  worse than no run (Article V). How the value is read in-process without launching `dotnet` is a
  research item the plan must verify on this machine before the column is implemented; if it cannot be
  read in-process, the plan brings the finding back to the Architect rather than substituting a value.
- **The upward walk** starts at the solution's base directory (the directory of the `.sln` or `.vbproj`
  given) and stops when the parent directory is null (drive root, or the top of a UNC path). Every level
  is checked for the four names, case-insensitively on Windows.
- **Only the four named files** are added. Files they import in turn (`.props`/`.targets` reached through
  `<Import>`) are the "full half" of F4 and out of scope.
- **`sdk_version` on upgraded rows is NULL**, because those runs did not record it and Article X rules
  nullable over sentinel. The Consumer filters on `IS NULL`. New rows cannot be null: the trigger in
  FR-109 ties the obligation to the row's own `schema_version`, so a v2 run that omits it is refused by
  the database. SQLite's `ALTER TABLE ADD COLUMN` cannot add a table-level CHECK that references another
  column, which is why the rule is a trigger rather than a CHECK, on every map (Clarifications, Q2);
  the choice is made here, not in the plan.
- **The upgrade runs inside the same `BEGIN IMMEDIATE` transaction** the extractor already takes, so it
  is serialized with other writers and rolled back on any failure like any other publication step.
- **Fresh-map initialization** also moves inside a transaction on the same connection; taking the write
  lock first means two processes racing to create one new path are serialized, and the loser sees a
  complete map. The alternative in the review (build in a sibling file and rename) is not required by
  this feature.
- **The nonce is opaque text** chosen by the test per run (a GUID is fine); the extractor never generates
  or stores one.
- **Package pins** are made in the extraction library's project file only, at the lowest versions that
  clear the two advisories; no other package changes.
- **Gate normalisation** applies to both the scanned text and the patterns; the vacuous-Red rule (count
  positive writes first) is kept.
- **Out of scope, recorded as known limits of the project**: F1 (project-scoped identity), F7
  (per-project target framework), F4 full (evaluated build manifest, imported props/targets, analyzers,
  reference identities), F11 (profiling and real-solution evidence). Each stays in the review report as
  open; none is addressed or partially addressed here.

## Out of Scope

- Any change to identity semantics (F1); a `T:` id shared across projects is still refused.
- Per-project or evaluated target-framework stamping (F7).
- Imported `.props`/`.targets`, analyzer and generator inputs, resolved references (F4 full).
- Profiling, batching, or acceptance runs against real solutions (F11).
- Any new verb, table, column beyond `sdk_version`, or CLI argument.
- A constitution amendment.

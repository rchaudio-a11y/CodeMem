# CodeMem Constitution

CodeMem is a VB.NET code-map extractor. It loads a compiled VB.NET solution through Roslyn
(Microsoft.CodeAnalysis.VisualBasic + MSBuildWorkspace) and writes what the compiler knows —
symbols, their declaring parts, and typed relationships between them — into one SQLite file,
codemem.sqlite. Consumers query that file. A read-only MCP server over the same file comes later.
There is no UI in this repository.

## Core Principles

### I. Library-First

CodeMem is partitioned by concern into class libraries; executables only wire. Initial projects:
CodeMem.Core (schema, contracts, repositories), CodeMem.Extractor (console entry point),
CodeMem.Tests. Adding a project requires written justification in the active plan. One class,
module, interface or enum per .vb file.

Rationale: A concern that lives in its own library can be tested and replaced on its own, and an
executable that only wires has nowhere to hide logic.

### II. Test-First

No implementation before a failing test. Order: write test → run → confirm Red → report Red to the
Architect → implement → confirm Green. A guard is trusted only after it has been shown to fire:
inject the defect, watch it go red, revert, confirm green, record the demonstration beside the
guard. A test that asserts something does NOT happen over a mechanism that does not yet exist
passes vacuously and MUST NOT be counted as Red.

Rationale: A test that has never failed is a claim about the code, not evidence about it.

### III. Integration-First

Tests run against real SQLite and a real compiled fixture solution checked into the test project.
No mocks of Roslyn, no mocks of SQLite. Unit tests are allowed where they materially improve
isolation; they never substitute for the fixture-solution integration tests.

Rationale: The behaviour worth verifying lives inside Roslyn and SQLite themselves, and a mock of
either asserts only what the author already believed.

### IV. Compiler Fact Only

Every row CodeMem writes is derived deterministically from the Roslyn semantic model or syntax tree
of a solution that compiled. No model inference, no heuristics, no LLM, no network. A relationship
verb is emitted only if it has a deterministic extraction rule written in the plan; a verb without
one is not emitted. Determinism is defined over the canonical logical fact set — the symbols, parts,
edges and reconciliation counts — for the same source digest, build configuration, schema version
and extractor version: two such runs produce an identical fact set. Operational metadata
(timestamps, run ids, surrogate ids, map_identity) is excluded from the comparison; byte-level
equality of the database file is not claimed.

Rationale: A map is only worth querying if every row on it can be re-derived by anyone holding the
same source.

### V. Green Only, Stamped

The extractor runs only against a solution that has just compiled successfully; on any build
failure it exits without writing and the previous snapshot stands. Every run writes one
extract_runs row before touching any other fact table, carrying at minimum: solution, a normalized
source digest computed over every compiled source file (always present); the commit sha and a
dirty-tree flag (nullable, recorded when a repository is present), build configuration, target
framework, extractor version, schema version, timestamp. Consumers derive currency from the stamp;
the word "stale" is never stored.

Publication is atomic. A run stages its results and validates them (Article VIII) before
publication; publication occurs in one transaction; a reader sees either the previous completed
run or the new completed run and never an intermediate state. A failure at any point before
publication — extraction error, count mismatch, cancellation, database error — leaves the previous
snapshot untouched. Concurrent extractor processes against one file are serialized by a lock; a
second extractor does not wait indefinitely and does not proceed — it reports and exits.

Rationale: A snapshot that cannot say which build it came from can be neither trusted nor
superseded. A snapshot that can be half-replaced is not a snapshot.

### VI. Reconcile, Never Truncate

code_symbols is an identity registry. A run reconciles observed symbols against it in a fixed
precedence:

(A) Identity match — automatic. An observed symbol whose solution_id and DocumentationCommentId
equal a registered active row keeps that row's id; labels and observation columns are refreshed.

(A′) Reactivation — automatic. An observed symbol with no (A) match whose solution_id and
DocumentationCommentId equal a RETIRED row, where no active row carries that identity, reactivates
that row: is_active = 1, the id is kept, labels and observation columns are refreshed, last_seen_run
advances, first_seen_run is unchanged. Where more than one retired row carries the identity, the
most recently retired is reactivated. A reactivation is neither new nor matched; it is counted as
symbols_reactivated.

(B) Rename candidate — proposal only, never applied. An observed symbol with no (A) match, whose
kind and container match a row retired in this run and whose content hash (Article VII, all
declaring references) equals that row's, is minted a NEW id, and a rename_candidates row is written
naming the retired id, the new id and the evidence. The registry is not changed by a candidate;
acceptance is a separate, human-initiated operation.

(C) Tiebreak within (B) — evidence only. Where more than one retired row satisfies (B), the
candidate rows record the offset proximity of their primary declarations as evidence and a rank.
Proximity alone never creates a candidate.

Any observed symbol not matched by (A) or (A′) is minted a new id. Any registered active row not observed
this run is marked is_active = 0 and its row stays. Ambiguity resolves to a new id, never to a
guess: a false match is worse than an unnecessary identity.

Ids are AUTOINCREMENT and are never reused. Only observation tables (code_edges, code_parts) are
replaced wholesale per run, scoped to the solution being extracted. DELETE, DROP, table recreation,
or any cascading delete reaching code_symbols is prohibited on every path.

Rationale: Anything anchored to a symbol id outlives the run that created it, so the id must
outlive the run too.

### VII. Evidence on Every Row

Every code_edges row carries the source file path and span of the occurrence that produced it.
Every code_symbols row carries the path and span of its primary declaration and a content hash
computed across ALL of its declaring references (partial types included). A row a reader cannot
trace back to a line is a defect.

Rationale: A fact a reader cannot check against the source is indistinguishable from a guess.

### VIII. Counts That Reconcile

Every extract_runs row records: symbols_observed, symbols_matched, symbols_reactivated,
symbols_new, symbols_retired, registry_active_before, notes_orphaned, rename_candidates,
unaccounted_observed and unaccounted_registry. The two residuals are computed independently of the
logic they audit — unaccounted_observed = symbols_observed − (symbols_matched +
symbols_reactivated + symbols_new); unaccounted_registry = registry_active_before −
(symbols_matched + symbols_retired), unchanged because a reactivated row was not active before —
and both are written even when zero. A run in which either residual is non-zero is reported as a failed run, not a partial one,
and is not published (Article V). Import and load surfaces report what was processed, not only
what failed. Until a notes table exists, notes_orphaned is written as 0; it is a column, not a
lookup.

Rationale: Two independently computed residuals, one per partition, are what turn a silent partial
write into a visible failure.

### IX. One File, Many Solutions, One Writer

codemem.sqlite holds every solution it maps. Every fact row carries solution_id; the extractor's
write scope for a run is exactly WHERE solution_id = the solution being extracted. The extractor is
the sole writer of fact tables. A one-row map_identity table holds a GUID minted when the file is
first created and never rewritten. CodeMem opens no database in any role other than its own map.
The constraint is on role, not path: tests create throwaway map files at temporary paths and those
are still codemem.sqlite in role.

Rationale: A single writer against a single file is the cheapest guarantee that no run can
half-overwrite another solution's map.

### X. Absence Must Be Representable

Nullable over sentinel; zero never doubles as "nobody said." Anything queryable lives in a typed
column, never behind a parse of a blob, JSON or delimited string. Types widen and never narrow
across a schema version.

Rationale: A schema that cannot say "unknown" forces every consumer to invent its own answer.

### XI. Anti-Abstraction

Microsoft.Data.Sqlite used directly; no ORM. All SQL in named repository methods in CodeMem.Core;
parameters named and bound. One canonical model per concept. Duplication is expected until a
pattern has three call sites in hand; anticipated call sites do not count.

Lineage: Rule of Three — Fowler, Refactoring (1999).

Rationale: An abstraction drawn from two cases encodes a coincidence; the third case is what
reveals the shape.

### XII. One Door for Every Rule

Validation lives at one entry point every code path walks. A second site consults the owning door;
it never restates the rule. Applies to documents as well as code. The schema owns structural
invariants — NOT NULL, foreign keys, UNIQUE, CHECK. Code does not restate them; it lets the
constraint fire and surfaces the error with the constraint's name. Code owns semantic invariants
the schema cannot express. Neither door restates the other's rule.

Rationale: A rule stated in two places is a rule that will be amended in one.

### XIII. Production-Route Reachability

A guard is demonstrated reachable from the production path it protects: at least one test exercises
it through the real entry point (the extractor's main path or a repository's public method),
proving refusals propagate. A rule with correct logic and no production caller is absent, not
partial.

Rationale: A guard nothing calls protects nothing, however correct its logic.

### XIV. Archive, Never Delete

Retired code moves to _Archive/; deprecated members receive <Obsolete> and stay until reviewed.
Retired rows are marked inactive, never deleted. A row is inactive; a file is archived; the words
are not interchangeable.

Rationale: What was removed and why is itself evidence, and deletion is the one edit that cannot be
reviewed after the fact.

### XV. Compiled .NET, No Foreign Runtime

VB.NET on .NET 8; C# permitted only as a separate project behind a Core contract. No Python, Node
or other runtime dependency. PowerShell and SQL files are permitted as build/dev-time artifacts
only.

Rationale: One runtime means one toolchain to install and one compiler to answer to, with no
interpreter standing between the source and the map.

## Cross-Cutting Hard Rules

- Option Strict On, Option Explicit On, Option Infer Off in every project.
- Every .vb file starts with a header block: filename, project, description, author (RCH Automation
  LLC), created date.
- XML documentation comments on every public class, method, property, event and interface member.
  GenerateDocumentationFile is enabled in every project for the XML output; the VB compiler emits
  no diagnostic for a missing comment (verified 2026-09-10, amendment v1.1.0), so enforcement is
  a Review Gate, not a build step.
- Naming: classes/methods/properties PascalCase; interfaces I-prefixed; private fields _camelCase;
  parameters and locals camelCase. SQL identifiers snake_case, tables plural, primary key id,
  foreign key <entity>_id, index ix_<table>_<cols>.
- Diagnose-Before-Fix: symptom → source → cause → diagnosis → proposed fix → wait for confirmation
  → then fix.
- Propose-Before-Building for anything touching more than one file, changing a signature, or making
  an architectural choice.
- No placeholder values presented as real; an unwired surface is disabled or labeled.

## Review Gates

Every plan, task list and implementation review verifies:

- Option settings present in every touched project file.
- No SQL outside a named repository method.
- Every new guard carries its fire demonstration (Art. II) and a production-route test (Art. XIII).
- No rule validated at two doors.
- Every extract_runs field in Art. V and every count in Art. VIII is written and tested.
- No path can DELETE from, DROP, recreate, or cascade a delete into code_symbols (Art. VI),
  asserted by a test that first counts a positive number of write statements in the repository and
  then asserts the absence of each destructive form.
- Both Article VIII residuals are written, tested, and shown to block publication when non-zero
  (Art. V).
- Rename candidates are never applied by the extractor, asserted through the production entry
  point (Art. XIII).
- Header block and XML docs on every new or modified file. This gate is the enforcement mechanism
  for the XML documentation rule; the compiler does not enforce it.
- Any new abstraction justified by three call sites in hand.

## Governance

This constitution is the highest authority in this repository. Where it is silent, ask the
Architect; silence is not permission. A rule is not changed by having been broken.

Amendments require Architect approval, a dated entry with rationale, and the migration path for
code the change puts out of compliance.

Versioning: MAJOR for removed or redefined principles, MINOR for new articles or expanded
obligations, PATCH for wording.

### Amendment Record

**2026-09-10 — v1.1.0 (MINOR).** Obligations expand in Articles V, VI and VIII; wording is
clarified in IV, IX, XII, the Cross-Cutting Hard Rules and the Review Gates. No article is removed
or redefined.

Rationale: two independent reviews of v1.0.0 found that determinism was overstated against run
metadata, that publication was not atomic, that symbol matching had no precedence and no ambiguity
rule and was inconsistent with the requirement that rename matches be proposed rather than
applied, that a dirty flag is not provenance, that the observed-side residual left the registry
side unaudited, that SQLite has no TRUNCATE, and that the XML-documentation rule named a mechanism
whose enforcement was unverified.

XML-documentation enforcement, verified empirically: a scratch VB.NET class library (net8.0,
Option Strict On, GenerateDocumentationFile enabled) containing one public class with one public
undocumented member built with 0 warnings and 0 errors, including under TreatWarningsAsErrors with
NoWarn cleared; the XML file was produced with an empty members element, confirming the setting
was in effect. A C# control project of identical shape emitted CS1591 for both members, confirming
the probe detects the diagnostic where a compiler has one. Finding: the VB compiler emits no
diagnostic for a missing XML comment. Branch taken: enforcement moved to the Review Gates.

Migration path: none required — no source code exists yet; this amendment lands before the first
table.

**2026-09-10 — v1.2.0 (MINOR).** One obligation added: Article VI gains (A′) Reactivation, and
Article VIII gains the count symbols_reactivated with the observed-side residual extended to include
it. One vacuous clause removed: Article VI (C) no longer speaks of a candidate arising when no
retired row satisfies (B), which "Proximity alone never creates a candidate" already forbade. No
article is removed or redefined.

Rationale: Article XIV names deactivate and reactivate as the two legs of one pair; a revert that
mints a third identity treats the return leg as a new arrival. Before this amendment, renaming X to
Y and reverting Y to X produced three ids for one symbol and a rename candidate pointing at the
wrong row.

Migration path: none — no code exists.

**2026-09-10 — v1.2.1 (PATCH).** Article V: "before touching any other table" → "before touching any
other fact table". Article IX defines fact tables as the rows carrying solution_id; map_identity and
solutions are identity setup, not run facts. Wording only; no obligation changes.

Rationale: the literal wording was unsatisfiable under the schema's foreign keys — extract_runs
references solutions, and the schema-version check reads map_identity — so no publication order
could honour it. The intent, that no fact row precedes the stamp, is unchanged.

Migration path: none.

**Version**: 1.2.1 | **Ratified**: 2026-09-10 | **Last Amended**: 2026-09-10

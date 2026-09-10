# Data Model: CodeMem Stage A

**Feature**: `001-extractor-codemem-sqlite` | **DDL**: [contracts/schema.sql](contracts/schema.sql)
(schema version 1) | **Spec**: [spec.md](spec.md) Key Entities

Column types and constraints are in the DDL and are not restated here (Article XII: the schema owns
structural invariants). This document covers meaning, lifecycle, the reconciliation algorithm, the
counts, and the in-memory staging records.

## Entities

### map_identity (one row)

Minted when the file is created; `map_guid` never rewritten (I14). `schema_version` is compared to
`SchemaVersion.Current` on every open; mismatch → exit 1.

### solutions

Identity of a solution inside the map. `key` is the only identity column (spec FR-032). `name` is the
display name (defaults to the key). `repo_root` and `last_seen_path` are labels refreshed by every
completed publication and never consulted for identity. `first_run_id` is null until the first
completed publication sets it. Rows are never deleted.

**Lifecycle**: `EnsureByKey` (insert if absent) at run setup → labels refreshed at publication.

### extract_runs

One row per run that reached validation. `outcome = 'completed'` rows are the stamps consumers derive
currency from; `outcome = 'failed'` rows record a residual mismatch (exit 4) and are never referenced
by any symbol. Refused runs (exit 1/2/3) write no row.

`source_digest` is always present. `commit_sha` and `is_dirty` are both null or both non-null
(schema `CHECK`).

### code_symbols — the identity registry

One row per source-declared symbol per solution, plus one `project` row per project. Identity for
matching is (`solution_id`, `doc_comment_id`) among **active** rows; the partial unique index enforces
that at most one active row carries an identity. `id` is the durable handle consumers anchor to.

**States**: `is_active = 1` (observed in the latest completed run of its solution) ⇄
`is_active = 0` (retired: not observed in a later run). Deactivate and reactivate are the two legs of
one pair (Article XIV, v1.2.0): a retired row whose identity reappears while no active row holds it
is **reactivated** on its original id (Article VI (A′)); where several retired rows carry the identity,
the most recently retired one is. A new id is minted only when no row — active or retired — carries
the identity.

**Refreshed on (A) match or (A′) reactivation**: `name`, `container_id`, `project_symbol_id`,
location (5 columns), `body_hash`, `last_seen_run_id`; reactivation also sets `is_active = 1`.
**Never changed**: `id`, `solution_id`, `doc_comment_id`, `kind`, `first_seen_run_id`.

`project_symbol_id` is the row of kind `project` for the compilation that declared the symbol — a
compiler fact stored as a column because `part_of` ends at the namespace. Null exactly for
`namespace` and `project` rows (schema `CHECK`).

### code_parts, code_edges — observation tables

Replaced wholesale per run, scoped by `solution_id`: `DELETE … WHERE solution_id = @solution_id`
then insert. Their ids autoincrement and are never reused; they are not durable handles. `code_edges`
references symbols by `source_symbol_id` (always a row), `target_symbol_id` (null when the target is
external), `target_doc_comment_id` (always), and `via_symbol_id` (`handles` only).

### rename_candidates — append-only proposals

Written only in the run that retired `retired_symbol_id`. Never updated or deleted by the extractor.
`rank = 1` is the nearest candidate for a given `new_symbol_id`; a unique candidate has rank 1.

## Location (shared shape)

`SourceLocation` (Core `Structure`): `Path` (solution-relative, `/`), `StartOffset` (0-based),
`Length`, `StartLine` (1-based), `StartColumn` (1-based). Built by one function from a Roslyn
`Location`/`TextSpan` + `SyntaxTree` — the single door for every path/span in the map.

## Staging records (Core `Records/`, in memory, never persisted as-is)

| Record | Fields | Notes |
|--------|--------|-------|
| `ObservedSymbol` | `DocCommentId`, `Kind`, `Name`, `ContainerDocCommentId` (nullable), `ProjectDocCommentId` (nullable: null for namespace and project), `Primary As SourceLocation`, `BodyHash`, `Parts As List(Of ObservedPart)` | container and project resolved to row ids at publication. **Namespaces are merged across compilations** by `DocCommentId`: parts accumulate, primary = first in (path, span) order, `BodyHash` recomputed over the merged parts. No other kind merges. |
| `ObservedPart` | `Location`, `PartHash` | |
| `ObservedEdge` | `SourceDocCommentId`, `Verb`, `TargetDocCommentId`, `ViaDocCommentId` (nullable), `Location` | symbol ids resolved at publication; a target/via doc id with no row → null id |
| `RegistryRow` | every `code_symbols` column | snapshot of active rows for the solution |
| `RunStamp` | `SolutionId`, `SourceDigest`, `CommitSha?`, `IsDirty?`, `BuildConfiguration`, `TargetFramework`, `ExtractorVersion`, `SchemaVersion`, `StartedUtc`, `FinishedUtc` | |
| `RunCounts` | the ten counts | residuals filled by `CountAuditor` only |
| `RenameCandidate` | `RetiredSymbolId`, `NewDocCommentId` (→ id at publication), `BodyHash`, `SamePath`, `OffsetDistance?`, `Rank` | |

## Reconciliation algorithm (`Reconciler.Reconcile(staged, snapshot, retired)`) — Article VI, FR-019–FR-022

Inputs: `staged` = observed symbols for this solution, namespaces already merged across
compilations; `snapshot` = active registry rows for this solution
(`registry_active_before = snapshot.Count`); `retired` = retired rows of this solution whose
`doc_comment_id` is among the staged ids not present in `snapshot`.

1. **Duplicate guard**: if two staged symbols share `DocCommentId` → refuse (exit 1) with both
   locations. Not a residual. (Namespaces were merged before staging and cannot trigger this.)
2. **(A) Identity match**: for each staged symbol, look up `snapshot` by `DocCommentId`. Hit → mark
   *matched*; emit a refresh (name, container, project, location, hash, last_seen). Miss → step 2′.
3. **(A′) Reactivation**: for each staged symbol that missed (A), look up `retired` by
   `DocCommentId`. Hit → choose the most recently retired (order by `last_seen_run_id` desc, `id`
   desc), mark *reactivated*; emit a reactivation (same refresh as (A) plus `is_active = 1`;
   `first_seen_run_id` untouched). Miss → mark *new*.
4. **Retire**: every snapshot row not hit in step 2 → *retired this run*.
5. **(B) Rename candidates**: for each *new* symbol `n` (never a reactivated one), the set
   `R = { r ∈ retired this run : r.kind = n.kind ∧ r.container_id = container_id_of(n) ∧ r.body_hash = n.BodyHash }`.
   `container_id_of(n)` is the id the container resolves to after step 2 — a matched container's
   existing id, or null. (A new container never equals a retired row's old container id; this is the
   spec's recorded "renamed container blinds its members" limit.) Each `r ∈ R` produces one candidate.
6. **(C) Evidence and rank**: for each `n` with `|R| ≥ 1`, order `R` by (`same_path` desc,
   `offset_distance` asc nulls last, `retired id` asc); assign `rank` 1..|R|. `same_path` =
   `r.path = n.Primary.Path`; `offset_distance` = `|r.start_offset − n.Primary.StartOffset|` when
   same path, else null. No threshold filters `R`; proximity never creates a candidate.
7. **Counts**: `symbols_observed = staged.Count`; `symbols_matched`, `symbols_reactivated`,
   `symbols_new`, `symbols_retired` from steps 2–4; `registry_active_before = snapshot.Count`;
   `rename_candidates = Σ|R|`; `notes_orphaned = 0`. Residuals are **not** computed here.

Output: `ReconciliationResult` = refreshes, reactivations, inserts (with parts/edges keyed by doc
id), retirements, candidates, counts.

## Count audit (`CountAuditor.Audit(counts)`) — Article VIII, FR-025

Separate class, no reference to `Reconciler`, computes from the ten counts alone:

```text
unaccounted_observed = symbols_observed − (symbols_matched + symbols_reactivated + symbols_new)
unaccounted_registry = registry_active_before − (symbols_matched + symbols_retired)
```

The registry-side formula does not include reactivations: a reactivated row was not active before,
so it is neither in `registry_active_before` nor among the retirements.

Both are written even when 0. Either ≠ 0 → the run is failed (exit 4). The unit test for this class
carries its fire demonstration: feed counts with a deliberate +1, assert non-zero.

## Publication order (inside the one `BEGIN IMMEDIATE` transaction)

1. `extract_runs` row (Article V: first) → `run_id`.
2. `UPDATE code_symbols` for each refresh (matched rows).
3. `UPDATE code_symbols SET is_active = 1, …` for each reactivation (same columns as a refresh).
4. `INSERT code_symbols` for each new symbol, in ordinal order of (kind rank: project, namespace,
   type kinds, members), so projects and containers are inserted before the rows that reference
   them; build `doc id → id` map from snapshot + reactivations + inserts and resolve `container_id`
   and `project_symbol_id` from it.
5. `UPDATE code_symbols SET is_active = 0` for each retirement.
6. `DELETE code_parts WHERE solution_id`; insert parts with resolved `symbol_id`.
7. `DELETE code_edges WHERE solution_id`; insert edges with resolved source/target/via ids.
8. `INSERT rename_candidates` with resolved `new_symbol_id`.
9. `UPDATE solutions` labels; set `first_run_id` if null.
10. `COMMIT`.

No statement in this list, nor anywhere in Core, is `DELETE`/`DROP` against `code_symbols`, and no
foreign key declares `ON DELETE CASCADE` (I13 asserts both, after asserting a positive count of
`INSERT`/`UPDATE` statements exist).

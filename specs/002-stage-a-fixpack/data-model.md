# Data Model: CodeMem Fixpack 002

**Feature**: `002-stage-a-fixpack` | **DDL**: [contracts/schema.sql](contracts/schema.sql) (schema version
2) | **Parent**: [../001-extractor-codemem-sqlite/data-model.md](../001-extractor-codemem-sqlite/data-model.md)

Only what changes is described; Stage A's entities, reconciliation algorithm, counts and publication
order stand except where amended below.

## Schema version 2

### extract_runs (changed)

- New column `sdk_version TEXT NULL`: the .NET SDK version the host resolver selected for the solution
  directory at run time (e.g. `10.0.401`), research R21. NULL exactly on rows written at schema
  version 1 (before the column existed). Never a sentinel.
- Two triggers own the obligation (Article XII), identical on fresh and upgraded maps:
  `tr_extract_runs_sdk_version_insert` (BEFORE INSERT) and `tr_extract_runs_sdk_version_update`
  (BEFORE UPDATE OF `sdk_version`, `schema_version`), both `WHEN NEW.schema_version >= 2 AND
  NEW.sdk_version IS NULL` → `RAISE(ABORT, 'sdk_version is required for schema_version >= 2')`.
  The run row's own `schema_version` column carries the obligation, so a v1 row stays valid forever.

### map_identity (value change)

`schema_version` = 2 on a fresh map and after an upgrade. `map_guid` and `created_utc` untouched by an
upgrade (I14).

### code_symbols (behaviour change, no DDL change)

`kind` joins the columns refreshed on an (A) match and an (A′) reactivation: name, **kind**, container,
project, location, hash, last seen. Identity and the partial unique index unchanged.

## Migration 1 → 2 (one shape)

```text
ALTER TABLE extract_runs ADD COLUMN sdk_version TEXT NULL;
CREATE TRIGGER tr_extract_runs_sdk_version_insert ...;
CREATE TRIGGER tr_extract_runs_sdk_version_update ...;
UPDATE map_identity SET schema_version = 2 WHERE id = 1;
```

Applied inside the extractor's `BEGIN IMMEDIATE` transaction:

- on a **version-1 map**, as the first statements after the lock;
- on a **fresh map**, right after the version-1 DDL and the identity row (inserted at version 1).

Both paths execute byte-identical statements, so `sqlite_master` (`type`, `name`, `tbl_name`, `sql`) is
identical by construction (R22). Nothing is dropped, rebuilt or deleted; existing rows read NULL.
An abort between the statements rolls back to the prior state: version 1 intact, or an empty file that
is fresh again.

## Map states at open (`MapDatabase.InspectSchema`, under the write lock)

| State | Detection | Action |
|-------|-----------|--------|
| `Fresh` | file absent, 0 bytes, or `sqlite_master` has no user table | create v1, identity row (version 1), migrate to 2 |
| `Version1` | `map_identity.schema_version = 1` | migrate to 2 |
| `Current` | `= 2` | proceed |
| `Foreign` | user tables exist but no `map_identity` table, or the table has no row | refuse: `NotAMapException` → exit 1, nothing written |
| `Newer` | `schema_version > 2` (or < 1) | refuse: `SchemaVersionMismatchException` → exit 1 |
| not SQLite | first statement fails (`SQLITE_NOTADB`, 26) | exit 1 |

## Compiled inputs (extended set)

Stage A's set (source documents outside `obj/`, project files, the solution file) plus, walking from the
solution base directory to the root of its path, every existing `Directory.Build.props`,
`Directory.Build.targets`, `Directory.Packages.props`, `global.json`. Same record shape (`CompiledInput`:
relative path with `..` segments, full path, normalised text), same ordinal ordering, same digest
formula, same dirty-flag participation. FR-005's claim is now "a digest over the selected compiled
inputs".

## Run stamp (`RunStamp`, changed)

Gains `SdkVersion As String` (never Nothing on a v2 run). Resolution failure aborts the run with exit 1
before any fact-table write.

## Staging and reconciliation records

Unchanged. `ObservedSymbol.Kind` already exists; publication now writes it on refresh and reactivation.

## Test-only seam values

`RunPhase` gains `DuringInitialize` (fresh map: after the identity row, before the migration) and
`DuringUpgrade` (after the migration statements, before `schema_version` is set). Arming requires the
nonce (contracts/cli.md).

-- CodeMem map schema, version 2.
-- Contract: a fresh codemem.sqlite is created by running the VERSION 1 section, inserting the identity row
-- at schema_version 1, then running the MIGRATION 1 -> 2 section; a version-1 map is upgraded by running
-- the MIGRATION 1 -> 2 section alone. Both paths execute the same statements, so sqlite_master
-- (type, name, tbl_name, sql) is identical on every map (spec 002, Clarifications Q2; research R22).
-- Naming per constitution: snake_case, tables plural, pk id, fk <entity>_id, index ix_<table>_<cols>,
-- trigger tr_<table>_<purpose>.
-- Article XII: the schema owns structural invariants; code lets them fire and surfaces the error.
-- Article XIV: the migration adds; nothing here drops, rebuilds or deletes.
-- The two PRAGMAs are connection settings executed at open, before BEGIN IMMEDIATE; they are documentation here.

PRAGMA foreign_keys = ON;
PRAGMA journal_mode = DELETE;

-- ======================================================================================================
-- VERSION 1 (byte-identical to the CREATE statements the Stage A extractor executed)
-- ======================================================================================================
-- SQLite stores each CREATE statement's text, inline comments included, in sqlite_master.sql. Every existing
-- version-1 map holds Stage A's text, so the statements below (and SchemaRepository.CreateVersion1) must not
-- change even in a comment, or a fresh map and an upgraded map would no longer have one shape (R22; found at
-- implementation, 2026-09-10). Remarks this feature adds sit BETWEEN statements, where SQLite does not store them:
--   extract_runs.source_digest: a digest over the SELECTED compiled inputs (FR-108), not a fingerprint of the
--   evaluated compilation.
--   code_symbols.kind: refreshed on an (A) match and an (A') reactivation (FR-101, FR-103).

CREATE TABLE map_identity (
    id              INTEGER PRIMARY KEY CHECK (id = 1),
    map_guid        TEXT    NOT NULL,
    schema_version  INTEGER NOT NULL,
    created_utc     TEXT    NOT NULL
);

CREATE TABLE solutions (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    key             TEXT    NOT NULL UNIQUE,
    name            TEXT    NOT NULL,
    repo_root       TEXT    NULL,               -- label, refreshed each run, never part of the key
    last_seen_path  TEXT    NOT NULL,           -- label, refreshed each run, never part of the key
    created_utc     TEXT    NOT NULL,
    first_run_id    INTEGER NULL REFERENCES extract_runs(id)   -- set by the first completed publication
);

CREATE TABLE extract_runs (
    id                      INTEGER PRIMARY KEY AUTOINCREMENT,
    solution_id             INTEGER NOT NULL REFERENCES solutions(id),
    outcome                 TEXT    NOT NULL CHECK (outcome IN ('completed', 'failed')),
    source_digest           TEXT    NOT NULL,   -- always present; primary provenance (FR-005)
    commit_sha              TEXT    NULL,       -- null when no repository (FR-006)
    is_dirty                INTEGER NULL CHECK (is_dirty IN (0, 1)),  -- null exactly when commit_sha is null
    build_configuration     TEXT    NOT NULL,
    target_framework        TEXT    NOT NULL,
    extractor_version       TEXT    NOT NULL,
    schema_version          INTEGER NOT NULL,
    started_utc             TEXT    NOT NULL,
    finished_utc            TEXT    NOT NULL,
    symbols_observed        INTEGER NOT NULL,
    symbols_matched         INTEGER NOT NULL,
    symbols_reactivated     INTEGER NOT NULL,   -- Article VI (A'): retired rows made active again
    symbols_new             INTEGER NOT NULL,
    symbols_retired         INTEGER NOT NULL,
    registry_active_before  INTEGER NOT NULL,
    notes_orphaned          INTEGER NOT NULL,   -- written as 0 until a notes table exists (Article VIII)
    rename_candidates       INTEGER NOT NULL,
    unaccounted_observed    INTEGER NOT NULL,   -- observed - (matched + reactivated + new); written even when 0
    unaccounted_registry    INTEGER NOT NULL,   -- registry_active_before - (matched + retired); written even when 0
    CHECK ((commit_sha IS NULL) = (is_dirty IS NULL))
);
CREATE INDEX ix_extract_runs_solution_id ON extract_runs(solution_id);

CREATE TABLE code_symbols (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    solution_id         INTEGER NOT NULL REFERENCES solutions(id),
    doc_comment_id      TEXT    NOT NULL,
    kind                TEXT    NOT NULL CHECK (kind IN (
                            'namespace', 'class', 'module', 'structure', 'interface', 'enum',
                            'enum_member', 'delegate', 'method', 'constructor', 'property',
                            'field', 'event', 'project')),
    name                TEXT    NOT NULL,
    container_id        INTEGER NULL REFERENCES code_symbols(id),
    project_symbol_id   INTEGER NULL REFERENCES code_symbols(id),   -- the declaring project's row (kind = 'project')
    path                TEXT    NOT NULL,       -- solution-relative, forward slashes
    start_offset        INTEGER NOT NULL,       -- 0-based character offset
    length              INTEGER NOT NULL,
    start_line          INTEGER NOT NULL,       -- 1-based
    start_column        INTEGER NOT NULL,       -- 1-based
    body_hash           TEXT    NOT NULL,       -- SHA-256 hex over all parts (Article VII)
    is_active           INTEGER NOT NULL CHECK (is_active IN (0, 1)),
    first_seen_run_id   INTEGER NOT NULL REFERENCES extract_runs(id),
    last_seen_run_id    INTEGER NOT NULL REFERENCES extract_runs(id),
    -- null is legal exactly for the two kinds that span or are a project
    CHECK ((kind IN ('namespace', 'project')) = (project_symbol_id IS NULL))
);
-- Exactly one ACTIVE row per (solution, doc-comment id). Retired rows may share a doc-comment id
-- with a later active row; that later row is a new identity (Article VI).
CREATE UNIQUE INDEX ix_code_symbols_solution_id_doc_comment_id_active
    ON code_symbols(solution_id, doc_comment_id) WHERE is_active = 1;
CREATE INDEX ix_code_symbols_solution_id_is_active ON code_symbols(solution_id, is_active);
CREATE INDEX ix_code_symbols_container_id ON code_symbols(container_id);
CREATE INDEX ix_code_symbols_project_symbol_id ON code_symbols(project_symbol_id);

CREATE TABLE code_parts (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    solution_id     INTEGER NOT NULL REFERENCES solutions(id),
    symbol_id       INTEGER NOT NULL REFERENCES code_symbols(id),
    path            TEXT    NOT NULL,
    start_offset    INTEGER NOT NULL,
    length          INTEGER NOT NULL,
    start_line      INTEGER NOT NULL,
    start_column    INTEGER NOT NULL,
    part_hash       TEXT    NOT NULL
);
CREATE INDEX ix_code_parts_solution_id ON code_parts(solution_id);
CREATE INDEX ix_code_parts_symbol_id ON code_parts(symbol_id);

CREATE TABLE code_edges (
    id                      INTEGER PRIMARY KEY AUTOINCREMENT,
    solution_id             INTEGER NOT NULL REFERENCES solutions(id),
    source_symbol_id        INTEGER NOT NULL REFERENCES code_symbols(id),
    verb                    TEXT    NOT NULL CHECK (verb IN (
                                'part_of', 'calls', 'uses', 'implements', 'extends',
                                'imports', 'depends_on', 'handles')),
    target_symbol_id        INTEGER NULL REFERENCES code_symbols(id),   -- null when target is external
    target_doc_comment_id   TEXT    NOT NULL,                            -- always present
    via_symbol_id           INTEGER NULL REFERENCES code_symbols(id),   -- handles only: the WithEvents member
    path                    TEXT    NOT NULL,       -- occurrence location (Article VII)
    start_offset            INTEGER NOT NULL,
    length                  INTEGER NOT NULL,
    start_line              INTEGER NOT NULL,
    start_column            INTEGER NOT NULL,
    CHECK (via_symbol_id IS NULL OR verb = 'handles')
);
CREATE INDEX ix_code_edges_solution_id_verb ON code_edges(solution_id, verb);
CREATE INDEX ix_code_edges_source_symbol_id ON code_edges(source_symbol_id);
CREATE INDEX ix_code_edges_target_symbol_id ON code_edges(target_symbol_id);
CREATE INDEX ix_code_edges_target_doc_comment_id ON code_edges(target_doc_comment_id);

CREATE TABLE rename_candidates (
    id                  INTEGER PRIMARY KEY AUTOINCREMENT,
    solution_id         INTEGER NOT NULL REFERENCES solutions(id),
    run_id              INTEGER NOT NULL REFERENCES extract_runs(id),
    retired_symbol_id   INTEGER NOT NULL REFERENCES code_symbols(id),
    new_symbol_id       INTEGER NOT NULL REFERENCES code_symbols(id),
    body_hash           TEXT    NOT NULL,       -- the equal hash: the (B) evidence
    same_path           INTEGER NOT NULL CHECK (same_path IN (0, 1)),   -- (C) proximity evidence
    offset_distance     INTEGER NULL,           -- |retired.start_offset - new.start_offset| when same_path = 1
    rank                INTEGER NOT NULL CHECK (rank >= 1),            -- 1 = nearest; 1 when unique
    UNIQUE (run_id, retired_symbol_id, new_symbol_id)
);
CREATE INDEX ix_rename_candidates_new_symbol_id ON rename_candidates(new_symbol_id);
CREATE INDEX ix_rename_candidates_retired_symbol_id ON rename_candidates(retired_symbol_id);

-- (a fresh map inserts its identity row here, at schema_version 1, then continues below)

-- ======================================================================================================
-- MIGRATION 1 -> 2 (inside the extractor's BEGIN IMMEDIATE; applied to fresh maps and to version-1 maps)
-- ======================================================================================================

-- The .NET SDK version the host resolver selected for the solution directory (research R21).
-- NULL exactly on rows written at schema_version 1, before the column existed (Article X).
ALTER TABLE extract_runs ADD COLUMN sdk_version TEXT NULL;

-- The obligation on new rows is owned by the schema, keyed on the row's own schema_version (Article XII).
CREATE TRIGGER tr_extract_runs_sdk_version_insert
BEFORE INSERT ON extract_runs
WHEN NEW.schema_version >= 2 AND NEW.sdk_version IS NULL
BEGIN
    SELECT RAISE(ABORT, 'sdk_version is required for schema_version >= 2');
END;

CREATE TRIGGER tr_extract_runs_sdk_version_update
BEFORE UPDATE OF sdk_version, schema_version ON extract_runs
WHEN NEW.schema_version >= 2 AND NEW.sdk_version IS NULL
BEGIN
    SELECT RAISE(ABORT, 'sdk_version is required for schema_version >= 2');
END;

UPDATE map_identity SET schema_version = 2 WHERE id = 1;

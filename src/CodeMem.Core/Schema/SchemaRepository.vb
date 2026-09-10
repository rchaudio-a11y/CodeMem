' File: SchemaRepository.vb
' Project: CodeMem.Core
' Description: Creates the version-1 schema, applies the migration to version 2, and reads what a file at --db contains (contracts/schema.sql of fixpack 002, verbatim).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-10 (fixpack 002): CreateSchema became CreateVersion1; UpgradeToVersion2, SetSchemaVersion, CountUserTables and
' HasMapIdentityRow added; every method takes the MapDatabase so the work runs inside its BEGIN IMMEDIATE (research R22, R23).

Imports Microsoft.Data.Sqlite

''' <summary>
''' The one place CREATE TABLE, ALTER TABLE and CREATE TRIGGER appear in Core. Article XII: the schema owns structural invariants.
''' A fresh map runs <see cref="CreateVersion1"/>, gets its identity row at version 1, then runs <see cref="UpgradeToVersion2"/> exactly
''' as a version-1 map does, so sqlite_master is identical on every map (spec 002 Clarifications Q2).
''' </summary>
Public Module SchemaRepository

    ' The CREATE statements below are byte-identical to the text the Stage A extractor executed (the text SQLite stores in
    ' sqlite_master.sql), so a fresh map and an upgraded Stage A map keep one shape. Comments inside a CREATE statement are part of
    ' that stored text and must not change; comments between statements are not stored.
    Private Const Version1Ddl As String = "-- CodeMem map schema, version 1 (the Stage A DDL, verbatim).

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
"

    ' The MIGRATION 1 -> 2 section of contracts/schema.sql minus its final UPDATE (SetSchemaVersion is the last statement, after the
    ' DuringUpgrade seam). Byte-identical on the fresh and the upgrade path (research R22).
    Private Const MigrationToVersion2 As String = "-- The .NET SDK version the host resolver selected for the solution directory (research R21).
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
"

    ''' <summary>
    ''' Executes the version-1 DDL (tables and indexes; no PRAGMA) inside the open transaction of a fresh map.
    ''' </summary>
    ''' <param name="db">The open map, fresh (no user tables), with BEGIN IMMEDIATE taken.</param>
    Public Sub CreateVersion1(db As MapDatabase)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = Version1Ddl
            command.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Executes the migration statements 1 -> 2: ALTER TABLE extract_runs ADD COLUMN sdk_version, then the two triggers. Does not set
    ''' map_identity.schema_version; the caller does that with <see cref="SetSchemaVersion"/> after the DuringUpgrade seam (FR-113).
    ''' Nothing is dropped, rebuilt or deleted (Article XIV).
    ''' </summary>
    ''' <param name="db">The open map at version 1 (fresh-and-just-created, or a Stage A map), with BEGIN IMMEDIATE taken.</param>
    Public Sub UpgradeToVersion2(db As MapDatabase)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = MigrationToVersion2
            command.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Writes map_identity.schema_version.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="version">The version to record.</param>
    Public Sub SetSchemaVersion(db As MapDatabase, version As Integer)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "UPDATE map_identity SET schema_version = @version WHERE id = 1"
            command.Parameters.AddWithValue("@version", version)
            command.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Reads map_identity.schema_version. Call only after <see cref="HasMapIdentityRow"/> returned True.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <returns>The stored schema version.</returns>
    Public Function ReadSchemaVersion(db As MapDatabase) As Integer
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT schema_version FROM map_identity WHERE id = 1"
            Dim value As Object = command.ExecuteScalar()
            If value Is Nothing OrElse TypeOf value Is DBNull Then
                Throw New InvalidOperationException("map_identity has no row")
            End If
            Return CInt(CLng(value))
        End Using
    End Function

    ''' <summary>
    ''' Counts the user tables in sqlite_master (type = 'table', name not sqlite_%). Zero means the file is fresh (FR-105, research R23);
    ''' the first statement on a file that is not SQLite fails here with SQLITE_NOTADB.
    ''' </summary>
    ''' <param name="db">The open file.</param>
    ''' <returns>The number of user tables.</returns>
    Public Function CountUserTables(db As MapDatabase) As Integer
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'"
            Return CInt(CLng(command.ExecuteScalar()))
        End Using
    End Function

    ''' <summary>
    ''' True when a map_identity table exists and holds its one row (id = 1). False on a foreign database or an empty identity table.
    ''' </summary>
    ''' <param name="db">The open file.</param>
    ''' <returns>Whether the file carries a map identity.</returns>
    Public Function HasMapIdentityRow(db As MapDatabase) As Boolean
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'map_identity'"
            If CLng(command.ExecuteScalar()) = 0 Then Return False
        End Using
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT COUNT(*) FROM map_identity WHERE id = 1"
            Return CLng(command.ExecuteScalar()) = 1
        End Using
    End Function

End Module

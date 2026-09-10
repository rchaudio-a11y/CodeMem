' File: SchemaRepository.vb
' Project: CodeMem.Core
' Description: Creates the map schema (the DDL of contracts/schema.sql, verbatim) and reads the schema version.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports Microsoft.Data.Sqlite

''' <summary>
''' The one place CREATE TABLE appears in Core. Article XII: the schema owns structural invariants.
''' </summary>
Public Module SchemaRepository

    Private Const Ddl As String = "-- CodeMem map schema, version 1.
-- Contract: this DDL is what CodeMem.Core creates in a fresh codemem.sqlite (FR-002).
-- Naming per constitution: snake_case, tables plural, pk id, fk <entity>_id, index ix_<table>_<cols>.
-- Article XII: the schema owns structural invariants (NOT NULL, FK, UNIQUE, CHECK); code does not restate them.
-- Article VI: nothing here cascades a delete into code_symbols.

PRAGMA foreign_keys = ON;
PRAGMA journal_mode = DELETE;

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

    ''' <summary>
    ''' Executes the schema DDL against a fresh database.
    ''' </summary>
    ''' <param name="connection">An open connection to an empty file.</param>
    Public Sub CreateSchema(connection As SqliteConnection)
        Using command As SqliteCommand = connection.CreateCommand()
            command.CommandText = Ddl
            command.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Reads map_identity.schema_version.
    ''' </summary>
    ''' <param name="connection">An open connection.</param>
    ''' <returns>The stored schema version.</returns>
    Public Function ReadSchemaVersion(connection As SqliteConnection) As Integer
        Using command As SqliteCommand = connection.CreateCommand()
            command.CommandText = "SELECT schema_version FROM map_identity WHERE id = 1"
            Dim value As Object = command.ExecuteScalar()
            If value Is Nothing OrElse TypeOf value Is DBNull Then
                Throw New InvalidOperationException("map_identity has no row")
            End If
            Return CInt(CLng(value))
        End Using
    End Function

End Module

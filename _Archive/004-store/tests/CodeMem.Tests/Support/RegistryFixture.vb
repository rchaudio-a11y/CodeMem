' File: RegistryFixture.vb
' Project: CodeMem.Tests
' Description: A throwaway MemOS store holding exactly the code_map_solutions table of migration 029 §1, seeded per test (feature 004, research R54).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' The DDL is transcribed from MemOS's migration 029 §1 verbatim - a copy of a contract, not a reference to a project (spec Assumptions).
' Test SQL: this file is excluded from SqlLocationGateTests like MapQueries.

Imports System.IO
Imports Microsoft.Data.Sqlite

''' <summary>
''' Creates <c>&lt;temp&gt;\codemem-tests\store-&lt;guid&gt;.sqlite</c> with the registry table and its two indexes, nothing else; seeds rows the
''' test names; deletes the file on dispose. <see cref="WithoutTable"/> makes a store with one unrelated table for the RegistryAbsent refusal.
''' </summary>
Public Class RegistryFixture
    Implements IDisposable

    Private Const RegistryDdl As String = "-- MemOS migration 029 §1, transcribed verbatim: the registry table and its two indexes.
CREATE TABLE code_map_solutions (
    id                  INTEGER PRIMARY KEY,
    project_id          INTEGER NOT NULL REFERENCES projects(id),
    solution_key        TEXT    NOT NULL UNIQUE,   -- the value passed as --solution-key; never defaulted
    codemem_solution_id INTEGER NULL,              -- solutions.id in the map; NULL until the first run publishes
    extraction_scope    TEXT    NOT NULL,          -- anchored path of the .sln or .vbproj actually extracted
    state               TEXT    NOT NULL,          -- 'active' | 'inactive'
    notes               TEXT    NULL,
    created_at          TEXT    NOT NULL,
    updated_at          TEXT    NOT NULL
);

CREATE INDEX ix_code_map_solutions_project_id ON code_map_solutions(project_id);

CREATE UNIQUE INDEX ix_code_map_solutions_codemem_solution_id
    ON code_map_solutions(codemem_solution_id) WHERE codemem_solution_id IS NOT NULL;
"

    ''' <summary>The store file path.</summary>
    Public ReadOnly Property Path As String

    ''' <summary>
    ''' Creates a store holding the registry table.
    ''' </summary>
    Public Sub New()
        Me.New(True)
    End Sub

    Private Sub New(withRegistryTable As Boolean)
        Dim dir As String = IO.Path.Combine(IO.Path.GetTempPath(), "codemem-tests")
        Directory.CreateDirectory(dir)
        Path = IO.Path.Combine(dir, "store-" & Guid.NewGuid().ToString("N") & ".sqlite")
        Using connection As SqliteConnection = Open()
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = If(withRegistryTable, RegistryDdl, "CREATE TABLE not_a_registry (x)")
                command.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' A store with one unrelated table and no registry: the window between a MemOS merge and its migration going live.
    ''' </summary>
    ''' <returns>The fixture; the caller disposes it.</returns>
    Public Shared Function WithoutTable() As RegistryFixture
        Return New RegistryFixture(False)
    End Function

    ''' <summary>
    ''' Inserts one registry row.
    ''' </summary>
    ''' <param name="projectId">The MemOS project id.</param>
    ''' <param name="solutionKey">The solution key, exact.</param>
    ''' <param name="codememSolutionId">The map's solutions.id, or Nothing for an unbound row.</param>
    ''' <param name="state">active or inactive.</param>
    ''' <param name="extractionScope">The anchored scope text (not read by the bridge).</param>
    ''' <returns>The new row id.</returns>
    Public Function Seed(projectId As Long, solutionKey As String, codememSolutionId As Long?, state As String, extractionScope As String) As Long
        Using connection As SqliteConnection = Open()
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "INSERT INTO code_map_solutions (project_id, solution_key, codemem_solution_id, extraction_scope, state, notes, created_at, updated_at) " &
                    "VALUES (@project_id, @solution_key, @codemem_solution_id, @extraction_scope, @state, NULL, @now, @now); SELECT last_insert_rowid()"
                command.Parameters.AddWithValue("@project_id", projectId)
                command.Parameters.AddWithValue("@solution_key", solutionKey)
                command.Parameters.AddWithValue("@codemem_solution_id", If(codememSolutionId.HasValue, CObj(codememSolutionId.Value), CObj(DBNull.Value)))
                command.Parameters.AddWithValue("@extraction_scope", extractionScope)
                command.Parameters.AddWithValue("@state", state)
                command.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"))
                Return CLng(command.ExecuteScalar())
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Deletes the store file.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        SqliteConnection.ClearAllPools()
        Try
            If File.Exists(Path) Then File.Delete(Path)
        Catch ex As IOException
            ' best effort: a leaked temp file is not a test failure
        End Try
    End Sub

    Private Function Open() As SqliteConnection
        Dim builder As SqliteConnectionStringBuilder = New SqliteConnectionStringBuilder With {.DataSource = Path, .Pooling = False}
        Dim connection As SqliteConnection = New SqliteConnection(builder.ConnectionString)
        connection.Open()
        ' The bundled SQLite enforces foreign keys by default and this store has no projects table (the real one does): off, on this fixture's connections only.
        Using pragma As SqliteCommand = connection.CreateCommand()
            pragma.CommandText = "PRAGMA foreign_keys = OFF"
            pragma.ExecuteNonQuery()
        End Using
        Return connection
    End Function

End Class

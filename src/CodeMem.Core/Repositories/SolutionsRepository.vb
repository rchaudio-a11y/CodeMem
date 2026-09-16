' File: SolutionsRepository.vb
' Project: CodeMem.Core
' Description: The solutions table: identity by key, labels refreshed per run (FR-032).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-15 (feature 004, T018): ReadByKey made public and ReadAll / ReadById added for the bridge, SELECT-only (research R45); the three
' readers share RowOf (Article XI: three call sites in hand).

Imports Microsoft.Data.Sqlite

''' <summary>
''' Ensures a solution row by key and refreshes its labels.
''' </summary>
Public Module SolutionsRepository

    ''' <summary>
    ''' Returns the solution row for a key, inserting it when absent (key is UNIQUE).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="key">The solution key.</param>
    ''' <param name="name">Display name for a new row.</param>
    ''' <param name="lastSeenPath">Solution path label for a new row.</param>
    ''' <param name="createdUtc">Creation timestamp for a new row.</param>
    ''' <returns>The row.</returns>
    Public Function EnsureByKey(db As MapDatabase, key As String, name As String, lastSeenPath As String, createdUtc As String) As SolutionRecord
        Dim existing As SolutionRecord = ReadByKey(db, key)
        If existing IsNot Nothing Then Return existing
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "INSERT INTO solutions (key, name, repo_root, last_seen_path, created_utc, first_run_id) VALUES (@key, @name, NULL, @last_seen_path, @created_utc, NULL)"
            command.Parameters.AddWithValue("@key", key)
            command.Parameters.AddWithValue("@name", name)
            command.Parameters.AddWithValue("@last_seen_path", lastSeenPath)
            command.Parameters.AddWithValue("@created_utc", createdUtc)
            command.ExecuteNonQuery()
        End Using
        Return ReadByKey(db, key)
    End Function

    ''' <summary>
    ''' Refreshes the two label columns after a completed publication.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionId">The solution.</param>
    ''' <param name="repoRoot">Repository working directory, or Nothing.</param>
    ''' <param name="lastSeenPath">The solution path this run used.</param>
    Public Sub RefreshLabels(db As MapDatabase, solutionId As Long, repoRoot As String, lastSeenPath As String)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "UPDATE solutions SET repo_root = @repo_root, last_seen_path = @last_seen_path WHERE id = @id"
            command.Parameters.AddWithValue("@repo_root", If(repoRoot Is Nothing, CObj(DBNull.Value), CObj(repoRoot)))
            command.Parameters.AddWithValue("@last_seen_path", lastSeenPath)
            command.Parameters.AddWithValue("@id", solutionId)
            command.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Sets first_run_id when it is still null.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionId">The solution.</param>
    ''' <param name="runId">The completed run.</param>
    Public Sub SetFirstRunIfNull(db As MapDatabase, solutionId As Long, runId As Long)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "UPDATE solutions SET first_run_id = @run_id WHERE id = @id AND first_run_id IS NULL"
            command.Parameters.AddWithValue("@run_id", runId)
            command.Parameters.AddWithValue("@id", solutionId)
            command.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Reads the solution row with a key, exact.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="key">The solution key.</param>
    ''' <returns>The row, or Nothing.</returns>
    Public Function ReadByKey(db As MapDatabase, key As String) As SolutionRecord
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT id, key, name, repo_root, last_seen_path, created_utc, first_run_id FROM solutions WHERE key = @key"
            command.Parameters.AddWithValue("@key", key)
            Using reader As SqliteDataReader = command.ExecuteReader()
                If Not reader.Read() Then Return Nothing
                Return RowOf(reader)
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Reads the solution row with an id (feature 004).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="id">The solution id.</param>
    ''' <returns>The row, or Nothing.</returns>
    Public Function ReadById(db As MapDatabase, id As Long) As SolutionRecord
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT id, key, name, repo_root, last_seen_path, created_utc, first_run_id FROM solutions WHERE id = @id"
            command.Parameters.AddWithValue("@id", id)
            Using reader As SqliteDataReader = command.ExecuteReader()
                If Not reader.Read() Then Return Nothing
                Return RowOf(reader)
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Reads every solution row ordered by key (feature 004; 056 §1.1).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <returns>The rows.</returns>
    Public Function ReadAll(db As MapDatabase) As List(Of SolutionRecord)
        Dim rows As List(Of SolutionRecord) = New List(Of SolutionRecord)()
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT id, key, name, repo_root, last_seen_path, created_utc, first_run_id FROM solutions ORDER BY key"
            Using reader As SqliteDataReader = command.ExecuteReader()
                While reader.Read()
                    rows.Add(RowOf(reader))
                End While
            End Using
        End Using
        Return rows
    End Function

    Private Function RowOf(reader As SqliteDataReader) As SolutionRecord
        Dim record As SolutionRecord = New SolutionRecord With {
            .Id = reader.GetInt64(0),
            .Key = reader.GetString(1),
            .Name = reader.GetString(2),
            .RepoRoot = If(reader.IsDBNull(3), Nothing, reader.GetString(3)),
            .LastSeenPath = reader.GetString(4),
            .CreatedUtc = reader.GetString(5)}
        If Not reader.IsDBNull(6) Then record.FirstRunId = reader.GetInt64(6)
        Return record
    End Function

End Module

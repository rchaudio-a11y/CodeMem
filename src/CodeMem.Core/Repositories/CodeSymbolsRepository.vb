' File: CodeSymbolsRepository.vb
' Project: CodeMem.Core
' Description: The identity registry: read active rows, insert new rows. No DELETE, DROP or recreate here, ever (Article VI, I13).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-10 (fixpack 002, F2): RefreshMatched and Reactivate set kind = @kind (FR-101, FR-103); identity columns untouched.

Imports Microsoft.Data.Sqlite

''' <summary>
''' code_symbols access. Rows are inserted, refreshed, retired and reactivated; never deleted.
''' </summary>
Public Module CodeSymbolsRepository

    Private Const Columns As String = "id, solution_id, doc_comment_id, kind, name, container_id, project_symbol_id, path, start_offset, length, start_line, start_column, body_hash, is_active, first_seen_run_id, last_seen_run_id"

    ''' <summary>
    ''' Reads every active row of a solution, ordered by id.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionId">The solution.</param>
    ''' <returns>The registry snapshot.</returns>
    Public Function ReadActive(db As MapDatabase, solutionId As Long) As List(Of RegistryRow)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT " & Columns & " FROM code_symbols WHERE solution_id = @solution_id AND is_active = 1 ORDER BY id"
            command.Parameters.AddWithValue("@solution_id", solutionId)
            Return ReadRows(command)
        End Using
    End Function

    ''' <summary>
    ''' Inserts a new active row minted by this run.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionId">The solution.</param>
    ''' <param name="symbol">The staged symbol.</param>
    ''' <param name="containerId">Resolved container row id, or Nothing.</param>
    ''' <param name="projectSymbolId">Resolved project row id, or Nothing.</param>
    ''' <param name="runId">This run: first and last seen.</param>
    ''' <returns>The new row id.</returns>
    Public Function InsertNew(db As MapDatabase, solutionId As Long, symbol As ObservedSymbol, containerId As Long?, projectSymbolId As Long?, runId As Long) As Long
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "INSERT INTO code_symbols (solution_id, doc_comment_id, kind, name, container_id, project_symbol_id, path, start_offset, length, start_line, start_column, body_hash, is_active, first_seen_run_id, last_seen_run_id) " &
                "VALUES (@solution_id, @doc_comment_id, @kind, @name, @container_id, @project_symbol_id, @path, @start_offset, @length, @start_line, @start_column, @body_hash, 1, @run_id, @run_id); SELECT last_insert_rowid()"
            command.Parameters.AddWithValue("@solution_id", solutionId)
            command.Parameters.AddWithValue("@doc_comment_id", symbol.DocCommentId)
            command.Parameters.AddWithValue("@kind", SymbolKindNames.ToText(symbol.Kind))
            command.Parameters.AddWithValue("@name", symbol.Name)
            command.Parameters.AddWithValue("@container_id", NullableValue(containerId))
            command.Parameters.AddWithValue("@project_symbol_id", NullableValue(projectSymbolId))
            AddLocation(command, symbol.Primary)
            command.Parameters.AddWithValue("@body_hash", symbol.BodyHash)
            command.Parameters.AddWithValue("@run_id", runId)
            Return CLng(command.ExecuteScalar())
        End Using
    End Function

    ''' <summary>
    ''' Reads retired rows of a solution whose doc-comment id is in the given set, most recently retired first (last_seen_run_id desc, id desc).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionId">The solution.</param>
    ''' <param name="docIds">The doc-comment ids of interest.</param>
    ''' <returns>The matching retired rows.</returns>
    Public Function ReadRetiredByDocIds(db As MapDatabase, solutionId As Long, docIds As ISet(Of String)) As List(Of RegistryRow)
        Dim rows As List(Of RegistryRow)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT " & Columns & " FROM code_symbols WHERE solution_id = @solution_id AND is_active = 0 ORDER BY last_seen_run_id DESC, id DESC"
            command.Parameters.AddWithValue("@solution_id", solutionId)
            rows = ReadRows(command)
        End Using
        Return rows.FindAll(Function(r As RegistryRow) docIds.Contains(r.DocCommentId))
    End Function

    ''' <summary>
    ''' Refreshes a matched row's labels and observation columns (A): kind, name, container, project, location, hash, last_seen_run_id (FR-101).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="id">The row.</param>
    ''' <param name="kind">The compiler's kind this run.</param>
    ''' <param name="name">Surface name.</param>
    ''' <param name="containerId">Resolved container id or Nothing.</param>
    ''' <param name="projectSymbolId">Resolved project id or Nothing.</param>
    ''' <param name="location">Primary declaration.</param>
    ''' <param name="bodyHash">Body hash.</param>
    ''' <param name="runId">This run.</param>
    Public Sub RefreshMatched(db As MapDatabase, id As Long, kind As SymbolKind, name As String, containerId As Long?, projectSymbolId As Long?, location As SourceLocation, bodyHash As String, runId As Long)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "UPDATE code_symbols SET kind = @kind, name = @name, container_id = @container_id, project_symbol_id = @project_symbol_id, path = @path, start_offset = @start_offset, length = @length, start_line = @start_line, start_column = @start_column, body_hash = @body_hash, last_seen_run_id = @run_id WHERE id = @id"
            AddRefresh(command, id, kind, name, containerId, projectSymbolId, location, bodyHash, runId)
            command.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Reactivates a retired row (A'): the same refresh as a match (kind included, FR-103) plus is_active = 1; first_seen_run_id is untouched.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="id">The retired row.</param>
    ''' <param name="kind">The compiler's kind this run.</param>
    ''' <param name="name">Surface name.</param>
    ''' <param name="containerId">Resolved container id or Nothing.</param>
    ''' <param name="projectSymbolId">Resolved project id or Nothing.</param>
    ''' <param name="location">Primary declaration.</param>
    ''' <param name="bodyHash">Body hash.</param>
    ''' <param name="runId">This run.</param>
    Public Sub Reactivate(db As MapDatabase, id As Long, kind As SymbolKind, name As String, containerId As Long?, projectSymbolId As Long?, location As SourceLocation, bodyHash As String, runId As Long)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "UPDATE code_symbols SET is_active = 1, kind = @kind, name = @name, container_id = @container_id, project_symbol_id = @project_symbol_id, path = @path, start_offset = @start_offset, length = @length, start_line = @start_line, start_column = @start_column, body_hash = @body_hash, last_seen_run_id = @run_id WHERE id = @id"
            AddRefresh(command, id, kind, name, containerId, projectSymbolId, location, bodyHash, runId)
            command.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' Marks a row inactive. The row stays (Article VI).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="id">The row.</param>
    Public Sub Retire(db As MapDatabase, id As Long)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "UPDATE code_symbols SET is_active = 0 WHERE id = @id"
            command.Parameters.AddWithValue("@id", id)
            command.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub AddRefresh(command As SqliteCommand, id As Long, kind As SymbolKind, name As String, containerId As Long?, projectSymbolId As Long?, location As SourceLocation, bodyHash As String, runId As Long)
        command.Parameters.AddWithValue("@id", id)
        command.Parameters.AddWithValue("@kind", SymbolKindNames.ToText(kind))
        command.Parameters.AddWithValue("@name", name)
        command.Parameters.AddWithValue("@container_id", NullableValue(containerId))
        command.Parameters.AddWithValue("@project_symbol_id", NullableValue(projectSymbolId))
        AddLocation(command, location)
        command.Parameters.AddWithValue("@body_hash", bodyHash)
        command.Parameters.AddWithValue("@run_id", runId)
    End Sub


    Private Sub AddLocation(command As SqliteCommand, location As SourceLocation)
        command.Parameters.AddWithValue("@path", location.Path)
        command.Parameters.AddWithValue("@start_offset", location.StartOffset)
        command.Parameters.AddWithValue("@length", location.Length)
        command.Parameters.AddWithValue("@start_line", location.StartLine)
        command.Parameters.AddWithValue("@start_column", location.StartColumn)
    End Sub

    Private Function NullableValue(value As Long?) As Object
        If value.HasValue Then Return value.Value
        Return DBNull.Value
    End Function

    Private Function ReadRows(command As SqliteCommand) As List(Of RegistryRow)
        Dim rows As List(Of RegistryRow) = New List(Of RegistryRow)()
        Using reader As SqliteDataReader = command.ExecuteReader()
            While reader.Read()
                Dim row As RegistryRow = New RegistryRow With {
                    .Id = reader.GetInt64(0),
                    .SolutionId = reader.GetInt64(1),
                    .DocCommentId = reader.GetString(2),
                    .Kind = SymbolKindNames.Parse(reader.GetString(3)),
                    .Name = reader.GetString(4),
                    .Path = reader.GetString(7),
                    .StartOffset = reader.GetInt32(8),
                    .Length = reader.GetInt32(9),
                    .StartLine = reader.GetInt32(10),
                    .StartColumn = reader.GetInt32(11),
                    .BodyHash = reader.GetString(12),
                    .IsActive = reader.GetInt32(13) = 1,
                    .FirstSeenRunId = reader.GetInt64(14),
                    .LastSeenRunId = reader.GetInt64(15)}
                If Not reader.IsDBNull(5) Then row.ContainerId = reader.GetInt64(5)
                If Not reader.IsDBNull(6) Then row.ProjectSymbolId = reader.GetInt64(6)
                rows.Add(row)
            End While
        End Using
        Return rows
    End Function

End Module

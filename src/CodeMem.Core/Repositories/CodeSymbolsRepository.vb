' File: CodeSymbolsRepository.vb
' Project: CodeMem.Core
' Description: The identity registry: read active rows, insert new rows. No DELETE, DROP or recreate here, ever (Article VI, I13).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-10 (fixpack 002, F2): RefreshMatched and Reactivate set kind = @kind (FR-101, FR-103); identity columns untouched.
' 2026-09-15 (feature 004, T020): ReadById, Search (raw rows, uncapped, COR3), ReadProjects and ReadOrphans (written fresh from the
' semantics 059/060 state, research R45) for the bridge, SELECT-only; the reads share the joined column list and SymbolRowOf.
' 2026-09-16 (feature 004, T049): ReadTwins - the candidate twins of one row for symbol_detail's header; the folder decides.

Imports Microsoft.Data.Sqlite

''' <summary>
''' code_symbols access. Rows are inserted, refreshed, retired and reactivated; never deleted.
''' </summary>
Public Module CodeSymbolsRepository

    Private Const Columns As String = "id, solution_id, doc_comment_id, kind, name, container_id, project_symbol_id, path, start_offset, length, start_line, start_column, body_hash, is_active, first_seen_run_id, last_seen_run_id"
    Private Const ReadColumns As String = "s.id, s.solution_id, s.doc_comment_id, s.kind, s.name, s.container_id, s.project_symbol_id, s.path, s.start_offset, s.length, s.start_line, s.start_column, s.body_hash, s.is_active, s.first_seen_run_id, s.last_seen_run_id, c.name, p.name, so.key"
    Private Const ReadFrom As String = " FROM code_symbols s JOIN solutions so ON so.id = s.solution_id LEFT JOIN code_symbols c ON c.id = s.container_id AND c.is_active = 1 LEFT JOIN code_symbols p ON p.id = s.project_symbol_id AND p.is_active = 1"

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

    ''' <summary>
    ''' One row by id, active or retired: the caller decides what a retired row means (feature 004).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="id">The row id.</param>
    ''' <returns>The row, or Nothing.</returns>
    Public Function ReadById(db As MapDatabase, id As Long) As SymbolRecord
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT " & ReadColumns & ReadFrom & " WHERE s.id = @id"
            command.Parameters.AddWithValue("@id", id)
            Using reader As SqliteDataReader = command.ExecuteReader()
                If Not reader.Read() Then Return Nothing
                Return SymbolRowOf(reader)
            End Using
        End Using
    End Function

    ''' <summary>
    ''' The active rows of one row's solution sharing its kind, name, path and start line, the row itself included, ordered by id
    ''' (feature 004, T049). Whether they present as twins is the folder's decision, not this read's.
    ''' </summary>
    ''' <param name="db">Open map.</param>
    ''' <param name="id">The row.</param>
    ''' <returns>The candidate twins; empty when the row is missing or retired.</returns>
    Public Function ReadTwins(db As MapDatabase, id As Long) As List(Of SymbolRecord)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT " & ReadColumns & ReadFrom & " JOIN code_symbols a ON a.id = @id AND a.is_active = 1 AND a.solution_id = s.solution_id AND a.kind = s.kind AND a.name = s.name AND a.path = s.path AND a.start_line = s.start_line WHERE s.is_active = 1 ORDER BY s.id"
            command.Parameters.AddWithValue("@id", id)
            Return SymbolRowsOf(command)
        End Using
    End Function

    ''' <summary>
    ''' Active rows of the given solutions matching a name (case-insensitive, anywhere in the name) and/or a kind: raw rows, uncapped, ordered
    ''' name, kind, path, start_line, project_symbol_id, id so the bridge's folder can group twins in memory (feature 004, research R50, COR3).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionIds">The scope's solutions.</param>
    ''' <param name="name">The name filter, or Nothing.</param>
    ''' <param name="kind">The kind text, or Nothing.</param>
    ''' <returns>The rows.</returns>
    Public Function Search(db As MapDatabase, solutionIds As IList(Of Long), name As String, kind As String) As List(Of SymbolRecord)
        Using command As SqliteCommand = db.CreateCommand()
            Dim sql As String = "SELECT " & ReadColumns & ReadFrom & " WHERE s.is_active = 1 AND s.solution_id IN (" & Placeholders(command, solutionIds) & ")"
            If name IsNot Nothing Then
                sql &= " AND instr(lower(s.name), lower(@name)) > 0"
                command.Parameters.AddWithValue("@name", name)
            End If
            If kind IsNot Nothing Then
                sql &= " AND s.kind = @kind"
                command.Parameters.AddWithValue("@kind", kind)
            End If
            command.CommandText = sql & " ORDER BY s.name, s.kind, s.path, s.start_line, s.project_symbol_id, s.id"
            Return SymbolRowsOf(command)
        End Using
    End Function

    ''' <summary>
    ''' Every active project row of the given solutions, ordered by solution, name, id (feature 004; 060 byProject).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionIds">The scope's solutions.</param>
    ''' <returns>The rows.</returns>
    Public Function ReadProjects(db As MapDatabase, solutionIds As IList(Of Long)) As List(Of SymbolRecord)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT " & ReadColumns & ReadFrom & " WHERE s.is_active = 1 AND s.kind = 'project' AND s.solution_id IN (" & Placeholders(command, solutionIds) & ") ORDER BY s.solution_id, s.name, s.id"
            Return SymbolRowsOf(command)
        End Using
    End Function

    ''' <summary>
    ''' Active symbols of an examined kind that no recorded reference reaches (feature 004; the semantics 059 and 060 state): a reference is any
    ''' edge but part_of whose source is neither the symbol nor inside it; a reference to anything a container holds, from outside that
    ''' container, counts for the container; a handles source and a member implementing an interface member are not reported. Ordered by
    ''' solution key, declaring project name, path, line, id.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionIds">The scope's solutions.</param>
    ''' <param name="kind">One examined kind to narrow to, or Nothing.</param>
    ''' <param name="projectSymbolId">One project row to narrow to, or Nothing.</param>
    ''' <returns>The orphans.</returns>
    Public Function ReadOrphans(db As MapDatabase, solutionIds As IList(Of Long), kind As String, projectSymbolId As Long?) As List(Of OrphanRecord)
        Dim orphans As List(Of OrphanRecord) = New List(Of OrphanRecord)()
        Using command As SqliteCommand = db.CreateCommand()
            Dim inScope As String = Placeholders(command, solutionIds)
            Dim sql As String =
                "WITH RECURSIVE containment(member_id, container_id) AS (" &
                " SELECT s.id, s.container_id FROM code_symbols s WHERE s.is_active = 1 AND s.container_id IS NOT NULL AND s.solution_id IN (" & inScope & ")" &
                " UNION ALL SELECT c.member_id, p.container_id FROM containment c JOIN code_symbols p ON p.id = c.container_id WHERE p.is_active = 1 AND p.container_id IS NOT NULL" &
                "), referenced(symbol_id) AS (" &
                " SELECT e.target_symbol_id FROM code_edges e WHERE e.solution_id IN (" & inScope & ") AND e.verb <> 'part_of' AND e.target_symbol_id IS NOT NULL AND e.source_symbol_id <> e.target_symbol_id" &
                "  AND NOT EXISTS (SELECT 1 FROM containment i WHERE i.member_id = e.source_symbol_id AND i.container_id = e.target_symbol_id)" &
                " UNION SELECT m.container_id FROM containment m JOIN code_edges e ON e.target_symbol_id = m.member_id AND e.verb <> 'part_of'" &
                "  WHERE e.source_symbol_id <> m.container_id AND NOT EXISTS (SELECT 1 FROM containment i WHERE i.member_id = e.source_symbol_id AND i.container_id = m.container_id)" &
                ") SELECT s.solution_id, so.key, s.id, s.doc_comment_id, s.kind, s.name, c.id, c.name, s.path, s.start_line, p.id, p.name" &
                " FROM code_symbols s JOIN solutions so ON so.id = s.solution_id" &
                " LEFT JOIN code_symbols c ON c.id = s.container_id AND c.is_active = 1" &
                " LEFT JOIN code_symbols p ON p.id = s.project_symbol_id AND p.is_active = 1" &
                " WHERE s.is_active = 1 AND s.solution_id IN (" & inScope & ") AND s.kind NOT IN ('namespace', 'project')" &
                " AND s.id NOT IN (SELECT symbol_id FROM referenced)" &
                " AND NOT EXISTS (SELECT 1 FROM code_edges h WHERE h.source_symbol_id = s.id AND (h.verb = 'handles' OR (h.verb = 'implements' AND s.kind NOT IN ('class', 'module', 'structure', 'interface'))))"
            If kind IsNot Nothing Then
                sql &= " AND s.kind = @kind"
                command.Parameters.AddWithValue("@kind", kind)
            End If
            If projectSymbolId.HasValue Then
                sql &= " AND s.project_symbol_id = @project_symbol_id"
                command.Parameters.AddWithValue("@project_symbol_id", projectSymbolId.Value)
            End If
            command.CommandText = sql & " ORDER BY so.key, p.name, s.path, s.start_line, s.id"
            Using reader As SqliteDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim row As OrphanRecord = New OrphanRecord With {
                        .SolutionId = reader.GetInt64(0),
                        .SolutionKey = reader.GetString(1),
                        .Id = reader.GetInt64(2),
                        .DocCommentId = reader.GetString(3),
                        .Kind = reader.GetString(4),
                        .Name = reader.GetString(5),
                        .ContainerName = If(reader.IsDBNull(7), Nothing, reader.GetString(7)),
                        .Path = reader.GetString(8),
                        .StartLine = reader.GetInt32(9),
                        .ProjectName = If(reader.IsDBNull(11), Nothing, reader.GetString(11))}
                    If Not reader.IsDBNull(6) Then row.ContainerId = reader.GetInt64(6)
                    If Not reader.IsDBNull(10) Then row.ProjectSymbolId = reader.GetInt64(10)
                    orphans.Add(row)
                End While
            End Using
        End Using
        Return orphans
    End Function

    Private Function Placeholders(command As SqliteCommand, solutionIds As IList(Of Long)) As String
        Dim names As List(Of String) = New List(Of String)()
        For i As Integer = 0 To solutionIds.Count - 1
            Dim parameter As String = "@sol" & i.ToString(Globalization.CultureInfo.InvariantCulture)
            names.Add(parameter)
            command.Parameters.AddWithValue(parameter, solutionIds(i))
        Next
        If names.Count = 0 Then Return "NULL"
        Return String.Join(", ", names)
    End Function

    Private Function SymbolRowsOf(command As SqliteCommand) As List(Of SymbolRecord)
        Dim rows As List(Of SymbolRecord) = New List(Of SymbolRecord)()
        Using reader As SqliteDataReader = command.ExecuteReader()
            While reader.Read()
                rows.Add(SymbolRowOf(reader))
            End While
        End Using
        Return rows
    End Function

    Private Function SymbolRowOf(reader As SqliteDataReader) As SymbolRecord
        Dim row As SymbolRecord = New SymbolRecord With {
            .Id = reader.GetInt64(0),
            .SolutionId = reader.GetInt64(1),
            .DocCommentId = reader.GetString(2),
            .Kind = reader.GetString(3),
            .Name = reader.GetString(4),
            .Path = reader.GetString(7),
            .StartOffset = reader.GetInt32(8),
            .Length = reader.GetInt32(9),
            .StartLine = reader.GetInt32(10),
            .StartColumn = reader.GetInt32(11),
            .BodyHash = reader.GetString(12),
            .IsActive = reader.GetInt32(13) = 1,
            .FirstSeenRunId = reader.GetInt64(14),
            .LastSeenRunId = reader.GetInt64(15),
            .ContainerName = If(reader.IsDBNull(16), Nothing, reader.GetString(16)),
            .ProjectName = If(reader.IsDBNull(17), Nothing, reader.GetString(17)),
            .SolutionKey = reader.GetString(18)}
        If Not reader.IsDBNull(5) Then row.ContainerId = reader.GetInt64(5)
        If Not reader.IsDBNull(6) Then row.ProjectSymbolId = reader.GetInt64(6)
        Return row
    End Function

End Module

' File: CodeEdgesRepository.vb
' Project: CodeMem.Core
' Description: The code_edges observation table, replaced wholesale per run for one solution only (Article VI, FR-015, FR-031).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-15 (feature 004, T020): ReadOutbound, ReadInbound and ReadReferences for the bridge, SELECT-only (research R45); the three
' share the joined column list and RowOf. T028: ReadTypeUsages, the one statement of research R51 with its UNION deduplicating by edge id
' (DUP1), carrying the target's role and whether the source lies inside the type.

Imports Microsoft.Data.Sqlite

''' <summary>
''' Replaces a solution's edges. The DELETE here is scoped to solution_id and never touches code_symbols.
''' </summary>
Public Module CodeEdgesRepository

    Private Const ReadColumns As String = "e.id, e.solution_id, e.source_symbol_id, src.name, src.kind, e.verb, e.target_symbol_id, e.target_doc_comment_id, tgt.name, tgt.kind, e.via_symbol_id, v.name, e.path, e.start_offset, e.length, e.start_line, e.start_column"
    Private Const ReadFrom As String = " FROM code_edges e JOIN code_symbols src ON src.id = e.source_symbol_id LEFT JOIN code_symbols tgt ON tgt.id = e.target_symbol_id LEFT JOIN code_symbols v ON v.id = e.via_symbol_id"

    ''' <summary>
    ''' Deletes the solution's edges and inserts the staged ones, resolving symbol ids from doc-comment ids (a target or via with no row becomes null).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionId">The solution.</param>
    ''' <param name="edges">The staged edges in insert order.</param>
    ''' <param name="ids">Doc-comment id to row id for every row of the solution.</param>
    Public Sub ReplaceForSolution(db As MapDatabase, solutionId As Long, edges As IEnumerable(Of ObservedEdge), ids As IDictionary(Of String, Long))
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "DELETE FROM code_edges WHERE solution_id = @solution_id"
            command.Parameters.AddWithValue("@solution_id", solutionId)
            command.ExecuteNonQuery()
        End Using
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "INSERT INTO code_edges (solution_id, source_symbol_id, verb, target_symbol_id, target_doc_comment_id, via_symbol_id, path, start_offset, length, start_line, start_column) " &
                "VALUES (@solution_id, @source_symbol_id, @verb, @target_symbol_id, @target_doc_comment_id, @via_symbol_id, @path, @start_offset, @length, @start_line, @start_column)"
            command.Parameters.AddWithValue("@solution_id", solutionId)
            Dim pSource As SqliteParameter = command.Parameters.Add("@source_symbol_id", SqliteType.Integer)
            Dim pVerb As SqliteParameter = command.Parameters.Add("@verb", SqliteType.Text)
            Dim pTarget As SqliteParameter = command.Parameters.Add("@target_symbol_id", SqliteType.Integer)
            Dim pTargetDoc As SqliteParameter = command.Parameters.Add("@target_doc_comment_id", SqliteType.Text)
            Dim pVia As SqliteParameter = command.Parameters.Add("@via_symbol_id", SqliteType.Integer)
            Dim pPath As SqliteParameter = command.Parameters.Add("@path", SqliteType.Text)
            Dim pOffset As SqliteParameter = command.Parameters.Add("@start_offset", SqliteType.Integer)
            Dim pLength As SqliteParameter = command.Parameters.Add("@length", SqliteType.Integer)
            Dim pLine As SqliteParameter = command.Parameters.Add("@start_line", SqliteType.Integer)
            Dim pColumn As SqliteParameter = command.Parameters.Add("@start_column", SqliteType.Integer)
            For Each edge As ObservedEdge In edges
                Dim sourceId As Long
                If Not ids.TryGetValue(edge.SourceDocCommentId, sourceId) Then
                    Throw New InvalidOperationException("edge source has no row: " & edge.SourceDocCommentId)
                End If
                pSource.Value = sourceId
                pVerb.Value = EdgeVerbNames.ToText(edge.Verb)
                pTarget.Value = Lookup(ids, edge.TargetDocCommentId)
                pTargetDoc.Value = edge.TargetDocCommentId
                pVia.Value = Lookup(ids, edge.ViaDocCommentId)
                pPath.Value = edge.Location.Path
                pOffset.Value = edge.Location.StartOffset
                pLength.Value = edge.Location.Length
                pLine.Value = edge.Location.StartLine
                pColumn.Value = edge.Location.StartColumn
                command.ExecuteNonQuery()
            Next
        End Using
    End Sub

    Private Function Lookup(ids As IDictionary(Of String, Long), docId As String) As Object
        Dim id As Long
        If docId IsNot Nothing AndAlso ids.TryGetValue(docId, id) Then Return id
        Return DBNull.Value
    End Function

    ''' <summary>
    ''' Every edge whose source is the symbol, in location order (feature 004; 058 §3.3 outbound).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="symbolId">The source symbol.</param>
    ''' <returns>The edges.</returns>
    Public Function ReadOutbound(db As MapDatabase, symbolId As Long) As List(Of EdgeRecord)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT " & ReadColumns & ReadFrom & " WHERE e.source_symbol_id = @id ORDER BY e.path, e.start_line, e.start_column, e.id"
            command.Parameters.AddWithValue("@id", symbolId)
            Return RowsOf(command)
        End Using
    End Function

    ''' <summary>
    ''' Every edge whose target is the symbol, in location order (feature 004; 058 §3.3 inbound).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="symbolId">The target symbol.</param>
    ''' <returns>The edges.</returns>
    Public Function ReadInbound(db As MapDatabase, symbolId As Long) As List(Of EdgeRecord)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT " & ReadColumns & ReadFrom & " WHERE e.target_symbol_id = @id ORDER BY e.path, e.start_line, e.start_column, e.id"
            command.Parameters.AddWithValue("@id", symbolId)
            Return RowsOf(command)
        End Using
    End Function

    ''' <summary>
    ''' Every occurrence targeting the symbol except containment, in location order (feature 004; 058 §3.4).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="symbolId">The target symbol.</param>
    ''' <returns>The occurrences.</returns>
    Public Function ReadReferences(db As MapDatabase, symbolId As Long) As List(Of EdgeRecord)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT " & ReadColumns & ReadFrom & " WHERE e.target_symbol_id = @id AND e.verb <> 'part_of' ORDER BY e.path, e.start_line, e.start_column, e.id"
            command.Parameters.AddWithValue("@id", symbolId)
            Return RowsOf(command)
        End Using
    End Function

    Private Function RowsOf(command As SqliteCommand) As List(Of EdgeRecord)
        Dim rows As List(Of EdgeRecord) = New List(Of EdgeRecord)()
        Using reader As SqliteDataReader = command.ExecuteReader()
            While reader.Read()
                rows.Add(RowOf(reader))
            End While
        End Using
        Return rows
    End Function

    ''' <summary>
    ''' Everything the map records against one type (feature 004, research R51, DUP1): one statement whose arms are (a) every non-part_of edge
    ''' targeting the type, (b) calls to its active constructors, (c) calls and uses of its other active direct members, and (d) the
    ''' implements and extends edges targeting it (a subset of (a)) - a UNION, so an edge picked by two arms appears once; each row carries the
    ''' target's role (type, constructor, member) and whether the source lies in the type's containment subtree. Ordered by verb in the fixed
    ''' order, then path, line, column, id.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="typeId">The type's row id.</param>
    ''' <returns>The occurrences.</returns>
    Public Function ReadTypeUsages(db As MapDatabase, typeId As Long) As List(Of EdgeRecord)
        Dim rows As List(Of EdgeRecord) = New List(Of EdgeRecord)()
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText =
                "WITH RECURSIVE inside(id) AS (SELECT @type UNION ALL SELECT s.id FROM code_symbols s JOIN inside i ON s.container_id = i.id WHERE s.is_active = 1)," &
                " picked(edge_id, role) AS (" &
                " SELECT e.id, 'type' FROM code_edges e WHERE e.target_symbol_id = @type AND e.verb <> 'part_of'" &
                " UNION SELECT e.id, 'constructor' FROM code_edges e JOIN code_symbols t ON t.id = e.target_symbol_id WHERE e.verb = 'calls' AND t.is_active = 1 AND t.kind = 'constructor' AND t.container_id = @type" &
                " UNION SELECT e.id, 'member' FROM code_edges e JOIN code_symbols t ON t.id = e.target_symbol_id WHERE e.verb IN ('calls', 'uses') AND t.is_active = 1 AND t.kind <> 'constructor' AND t.container_id = @type" &
                " UNION SELECT e.id, 'type' FROM code_edges e WHERE e.target_symbol_id = @type AND e.verb IN ('implements', 'extends')" &
                ") SELECT " & ReadColumns & ", p.role, CASE WHEN e.source_symbol_id IN (SELECT id FROM inside) THEN 1 ELSE 0 END" &
                " FROM picked p JOIN code_edges e ON e.id = p.edge_id JOIN code_symbols src ON src.id = e.source_symbol_id LEFT JOIN code_symbols tgt ON tgt.id = e.target_symbol_id LEFT JOIN code_symbols v ON v.id = e.via_symbol_id" &
                " ORDER BY CASE e.verb WHEN 'calls' THEN 1 WHEN 'uses' THEN 2 WHEN 'implements' THEN 3 WHEN 'extends' THEN 4 WHEN 'imports' THEN 5 WHEN 'depends_on' THEN 6 WHEN 'handles' THEN 7 ELSE 8 END, e.path, e.start_line, e.start_column, e.id"
            command.Parameters.AddWithValue("@type", typeId)
            Using reader As SqliteDataReader = command.ExecuteReader()
                While reader.Read()
                    Dim row As EdgeRecord = RowOf(reader)
                    row.TargetRole = reader.GetString(17)
                    row.FromInside = reader.GetInt32(18) = 1
                    rows.Add(row)
                End While
            End Using
        End Using
        Return rows
    End Function

    Private Function RowOf(reader As SqliteDataReader) As EdgeRecord
        Dim row As EdgeRecord = New EdgeRecord With {
            .Id = reader.GetInt64(0),
            .SolutionId = reader.GetInt64(1),
            .SourceSymbolId = reader.GetInt64(2),
            .SourceName = reader.GetString(3),
            .SourceKind = reader.GetString(4),
            .Verb = reader.GetString(5),
            .TargetDocCommentId = reader.GetString(7),
            .TargetName = If(reader.IsDBNull(8), Nothing, reader.GetString(8)),
            .TargetKind = If(reader.IsDBNull(9), Nothing, reader.GetString(9)),
            .ViaName = If(reader.IsDBNull(11), Nothing, reader.GetString(11)),
            .Path = reader.GetString(12),
            .StartOffset = reader.GetInt32(13),
            .Length = reader.GetInt32(14),
            .StartLine = reader.GetInt32(15),
            .StartColumn = reader.GetInt32(16)}
        If Not reader.IsDBNull(6) Then row.TargetSymbolId = reader.GetInt64(6)
        If Not reader.IsDBNull(10) Then row.ViaSymbolId = reader.GetInt64(10)
        Return row
    End Function

End Module

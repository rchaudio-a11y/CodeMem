' File: CodeEdgesRepository.vb
' Project: CodeMem.Core
' Description: The code_edges observation table, replaced wholesale per run for one solution only (Article VI, FR-015, FR-031).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports Microsoft.Data.Sqlite

''' <summary>
''' Replaces a solution's edges. The DELETE here is scoped to solution_id and never touches code_symbols.
''' </summary>
Public Module CodeEdgesRepository

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

End Module

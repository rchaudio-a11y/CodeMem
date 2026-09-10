' File: MapQueries.vb
' Project: CodeMem.Tests
' Description: The only SQL in the test project: named read (and one write) methods over a map file.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports Microsoft.Data.Sqlite

''' <summary>
''' Named queries over a map file. Every other test file reaches the database through these methods.
''' </summary>
Public Module MapQueries

    Private ReadOnly FactTables As String() = New String() {"extract_runs", "code_symbols", "code_parts", "code_edges", "rename_candidates"}

    ''' <summary>
    ''' The tables <see cref="DumpTable"/> and <see cref="MapSnapshot.RowsForSolution"/> cover, in dump order.
    ''' </summary>
    ''' <returns>solutions plus the five fact tables.</returns>
    Public Function SnapshotTables() As String()
        Return New String() {"solutions", "extract_runs", "code_symbols", "code_parts", "code_edges", "rename_candidates"}
    End Function

    Private Function Open(db As String) As SqliteConnection
        Dim connection As SqliteConnection = New SqliteConnection("Data Source=" & db & ";Pooling=False")
        connection.Open()
        Return connection
    End Function

    ''' <summary>
    ''' Counts rows of a table scoped to a solution (solutions is scoped by id; map_identity is unscoped).
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <param name="table">Table name.</param>
    ''' <param name="solutionId">Solution id.</param>
    ''' <returns>The row count.</returns>
    Public Function CountRows(db As String, table As String, solutionId As Long) As Long
        Using connection As SqliteConnection = Open(db)
            Using command As SqliteCommand = connection.CreateCommand()
                If table = "map_identity" Then
                    command.CommandText = "SELECT COUNT(*) FROM map_identity"
                ElseIf table = "solutions" Then
                    command.CommandText = "SELECT COUNT(*) FROM solutions WHERE id = @solution_id"
                    command.Parameters.AddWithValue("@solution_id", solutionId)
                ElseIf Array.IndexOf(FactTables, table) >= 0 Then
                    command.CommandText = "SELECT COUNT(*) FROM " & table & " WHERE solution_id = @solution_id"
                    command.Parameters.AddWithValue("@solution_id", solutionId)
                Else
                    Throw New ArgumentOutOfRangeException(NameOf(table), table, "unknown table")
                End If
                Return CLng(command.ExecuteScalar())
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Reads every code_symbols row of a solution, active and retired, ordered by id.
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <param name="solutionId">Solution id.</param>
    ''' <returns>The rows.</returns>
    Public Function ReadSymbols(db As String, solutionId As Long) As List(Of SymbolRow)
        Dim rows As List(Of SymbolRow) = New List(Of SymbolRow)()
        Using connection As SqliteConnection = Open(db)
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "SELECT id, doc_comment_id, kind, name, container_id, project_symbol_id, path, start_offset, length, start_line, start_column, body_hash, is_active, first_seen_run_id, last_seen_run_id FROM code_symbols WHERE solution_id = @solution_id ORDER BY id"
                command.Parameters.AddWithValue("@solution_id", solutionId)
                Using reader As SqliteDataReader = command.ExecuteReader()
                    While reader.Read()
                        rows.Add(New SymbolRow With {
                            .Id = reader.GetInt64(0),
                            .DocCommentId = reader.GetString(1),
                            .Kind = reader.GetString(2),
                            .Name = reader.GetString(3),
                            .ContainerId = NullableLong(reader, 4),
                            .ProjectSymbolId = NullableLong(reader, 5),
                            .Path = reader.GetString(6),
                            .StartOffset = reader.GetInt32(7),
                            .Length = reader.GetInt32(8),
                            .StartLine = reader.GetInt32(9),
                            .StartColumn = reader.GetInt32(10),
                            .BodyHash = reader.GetString(11),
                            .IsActive = reader.GetInt32(12) = 1,
                            .FirstSeenRunId = reader.GetInt64(13),
                            .LastSeenRunId = reader.GetInt64(14)})
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    ''' <summary>
    ''' Reads every code_parts row of a solution with its symbol's doc-comment id, ordered by (path, start_offset).
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <param name="solutionId">Solution id.</param>
    ''' <returns>The rows.</returns>
    Public Function ReadParts(db As String, solutionId As Long) As List(Of PartRow)
        Dim rows As List(Of PartRow) = New List(Of PartRow)()
        Using connection As SqliteConnection = Open(db)
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "SELECT p.id, p.symbol_id, s.doc_comment_id, p.path, p.start_offset, p.length, p.start_line, p.start_column, p.part_hash FROM code_parts p JOIN code_symbols s ON s.id = p.symbol_id WHERE p.solution_id = @solution_id ORDER BY p.path, p.start_offset, p.id"
                command.Parameters.AddWithValue("@solution_id", solutionId)
                Using reader As SqliteDataReader = command.ExecuteReader()
                    While reader.Read()
                        rows.Add(New PartRow With {
                            .Id = reader.GetInt64(0),
                            .SymbolId = reader.GetInt64(1),
                            .SymbolDocCommentId = reader.GetString(2),
                            .Path = reader.GetString(3),
                            .StartOffset = reader.GetInt32(4),
                            .Length = reader.GetInt32(5),
                            .StartLine = reader.GetInt32(6),
                            .StartColumn = reader.GetInt32(7),
                            .PartHash = reader.GetString(8)})
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    ''' <summary>
    ''' Reads code_edges rows of a solution, optionally for one verb, with doc-comment ids joined in.
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <param name="solutionId">Solution id.</param>
    ''' <param name="verb">Verb text, or Nothing for all verbs.</param>
    ''' <returns>The rows ordered by id.</returns>
    Public Function ReadEdges(db As String, solutionId As Long, verb As String) As List(Of EdgeRow)
        Dim rows As List(Of EdgeRow) = New List(Of EdgeRow)()
        Using connection As SqliteConnection = Open(db)
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "SELECT e.id, e.source_symbol_id, src.doc_comment_id, e.verb, e.target_symbol_id, e.target_doc_comment_id, e.via_symbol_id, via.doc_comment_id, e.path, e.start_offset, e.length, e.start_line, e.start_column " &
                    "FROM code_edges e JOIN code_symbols src ON src.id = e.source_symbol_id LEFT JOIN code_symbols via ON via.id = e.via_symbol_id " &
                    "WHERE e.solution_id = @solution_id AND (@verb IS NULL OR e.verb = @verb) ORDER BY e.id"
                command.Parameters.AddWithValue("@solution_id", solutionId)
                command.Parameters.AddWithValue("@verb", If(verb Is Nothing, CObj(DBNull.Value), CObj(verb)))
                Using reader As SqliteDataReader = command.ExecuteReader()
                    While reader.Read()
                        rows.Add(New EdgeRow With {
                            .Id = reader.GetInt64(0),
                            .SourceSymbolId = reader.GetInt64(1),
                            .SourceDocCommentId = reader.GetString(2),
                            .Verb = reader.GetString(3),
                            .TargetSymbolId = NullableLong(reader, 4),
                            .TargetDocCommentId = reader.GetString(5),
                            .ViaSymbolId = NullableLong(reader, 6),
                            .ViaDocCommentId = NullableString(reader, 7),
                            .Path = reader.GetString(8),
                            .StartOffset = reader.GetInt32(9),
                            .Length = reader.GetInt32(10),
                            .StartLine = reader.GetInt32(11),
                            .StartColumn = reader.GetInt32(12)})
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    ''' <summary>
    ''' Reads every extract_runs row in the map, ordered by id.
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <returns>The rows.</returns>
    Public Function ReadRuns(db As String) As List(Of RunRow)
        Dim rows As List(Of RunRow) = New List(Of RunRow)()
        Using connection As SqliteConnection = Open(db)
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "SELECT id, solution_id, outcome, source_digest, commit_sha, is_dirty, build_configuration, target_framework, extractor_version, schema_version, started_utc, finished_utc, " &
                    "symbols_observed, symbols_matched, symbols_reactivated, symbols_new, symbols_retired, registry_active_before, notes_orphaned, rename_candidates, unaccounted_observed, unaccounted_registry FROM extract_runs ORDER BY id"
                Using reader As SqliteDataReader = command.ExecuteReader()
                    While reader.Read()
                        Dim dirty As Boolean? = Nothing
                        If Not reader.IsDBNull(5) Then dirty = reader.GetInt32(5) = 1
                        rows.Add(New RunRow With {
                            .Id = reader.GetInt64(0),
                            .SolutionId = reader.GetInt64(1),
                            .Outcome = reader.GetString(2),
                            .SourceDigest = reader.GetString(3),
                            .CommitSha = NullableString(reader, 4),
                            .IsDirty = dirty,
                            .BuildConfiguration = reader.GetString(6),
                            .TargetFramework = reader.GetString(7),
                            .ExtractorVersion = reader.GetString(8),
                            .SchemaVersion = reader.GetInt32(9),
                            .StartedUtc = reader.GetString(10),
                            .FinishedUtc = reader.GetString(11),
                            .SymbolsObserved = reader.GetInt32(12),
                            .SymbolsMatched = reader.GetInt32(13),
                            .SymbolsReactivated = reader.GetInt32(14),
                            .SymbolsNew = reader.GetInt32(15),
                            .SymbolsRetired = reader.GetInt32(16),
                            .RegistryActiveBefore = reader.GetInt32(17),
                            .NotesOrphaned = reader.GetInt32(18),
                            .RenameCandidates = reader.GetInt32(19),
                            .UnaccountedObserved = reader.GetInt32(20),
                            .UnaccountedRegistry = reader.GetInt32(21)})
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    ''' <summary>
    ''' Reads the map GUID.
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <returns>The GUID text.</returns>
    Public Function ReadMapGuid(db As String) As String
        Using connection As SqliteConnection = Open(db)
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "SELECT map_guid FROM map_identity WHERE id = 1"
                Return CStr(command.ExecuteScalar())
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Reads the schema version from map_identity.
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <returns>The schema version.</returns>
    Public Function ReadSchemaVersion(db As String) As Integer
        Using connection As SqliteConnection = Open(db)
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "SELECT schema_version FROM map_identity WHERE id = 1"
                Return CInt(CLng(command.ExecuteScalar()))
            End Using
        End Using
    End Function

    ''' <summary>
    ''' Reads the rename candidates written by one run, ordered by (new_symbol_id, rank).
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <param name="runId">Run id.</param>
    ''' <returns>The rows.</returns>
    Public Function ReadCandidates(db As String, runId As Long) As List(Of CandidateRow)
        Dim rows As List(Of CandidateRow) = New List(Of CandidateRow)()
        Using connection As SqliteConnection = Open(db)
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "SELECT id, run_id, retired_symbol_id, new_symbol_id, body_hash, same_path, offset_distance, rank FROM rename_candidates WHERE run_id = @run_id ORDER BY new_symbol_id, rank"
                command.Parameters.AddWithValue("@run_id", runId)
                Using reader As SqliteDataReader = command.ExecuteReader()
                    While reader.Read()
                        Dim distance As Integer? = Nothing
                        If Not reader.IsDBNull(6) Then distance = reader.GetInt32(6)
                        rows.Add(New CandidateRow With {
                            .Id = reader.GetInt64(0),
                            .RunId = reader.GetInt64(1),
                            .RetiredSymbolId = reader.GetInt64(2),
                            .NewSymbolId = reader.GetInt64(3),
                            .BodyHash = reader.GetString(4),
                            .SamePath = reader.GetInt32(5) = 1,
                            .OffsetDistance = distance,
                            .Rank = reader.GetInt32(7)})
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    ''' <summary>
    ''' Reads every solutions row, ordered by id.
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <returns>The rows.</returns>
    Public Function ReadSolutions(db As String) As List(Of SolutionRow)
        Dim rows As List(Of SolutionRow) = New List(Of SolutionRow)()
        Using connection As SqliteConnection = Open(db)
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "SELECT id, key, name, repo_root, last_seen_path, first_run_id FROM solutions ORDER BY id"
                Using reader As SqliteDataReader = command.ExecuteReader()
                    While reader.Read()
                        rows.Add(New SolutionRow With {
                            .Id = reader.GetInt64(0),
                            .Key = reader.GetString(1),
                            .Name = reader.GetString(2),
                            .RepoRoot = NullableString(reader, 3),
                            .LastSeenPath = reader.GetString(4),
                            .FirstRunId = NullableLong(reader, 5)})
                    End While
                End Using
            End Using
        End Using
        Return rows
    End Function

    ''' <summary>
    ''' Rewrites map_identity.schema_version (used only to provoke the schema-version refusal).
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <param name="n">The version to write.</param>
    Public Sub SetSchemaVersion(db As String, n As Integer)
        Using connection As SqliteConnection = Open(db)
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "UPDATE map_identity SET schema_version = @n WHERE id = 1"
                command.Parameters.AddWithValue("@n", n)
                command.ExecuteNonQuery()
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' The canonical logical fact set of a solution (Article IV): symbols keyed by doc id with kind, name, container doc id,
    ''' project doc id, location and hash; parts; edges; and the ten counts of the latest completed run. Surrogate ids, run ids,
    ''' timestamps and map identity are excluded.
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <param name="solutionId">Solution id.</param>
    ''' <returns>Sorted canonical lines.</returns>
    Public Function FactSet(db As String, solutionId As Long) As List(Of String)
        Dim lines As List(Of String) = New List(Of String)()
        Using connection As SqliteConnection = Open(db)
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "SELECT s.doc_comment_id, s.kind, s.name, c.doc_comment_id, p.doc_comment_id, s.path, s.start_offset, s.length, s.start_line, s.start_column, s.body_hash " &
                    "FROM code_symbols s LEFT JOIN code_symbols c ON c.id = s.container_id LEFT JOIN code_symbols p ON p.id = s.project_symbol_id WHERE s.solution_id = @solution_id AND s.is_active = 1"
                command.Parameters.AddWithValue("@solution_id", solutionId)
                Using reader As SqliteDataReader = command.ExecuteReader()
                    While reader.Read()
                        lines.Add("S|" & Join(reader, 11))
                    End While
                End Using
            End Using
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "SELECT s.doc_comment_id, p.path, p.start_offset, p.length, p.start_line, p.start_column, p.part_hash FROM code_parts p JOIN code_symbols s ON s.id = p.symbol_id WHERE p.solution_id = @solution_id"
                command.Parameters.AddWithValue("@solution_id", solutionId)
                Using reader As SqliteDataReader = command.ExecuteReader()
                    While reader.Read()
                        lines.Add("P|" & Join(reader, 7))
                    End While
                End Using
            End Using
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "SELECT src.doc_comment_id, e.verb, e.target_doc_comment_id, (e.target_symbol_id IS NOT NULL), via.doc_comment_id, e.path, e.start_offset, e.length, e.start_line, e.start_column " &
                    "FROM code_edges e JOIN code_symbols src ON src.id = e.source_symbol_id LEFT JOIN code_symbols via ON via.id = e.via_symbol_id WHERE e.solution_id = @solution_id"
                command.Parameters.AddWithValue("@solution_id", solutionId)
                Using reader As SqliteDataReader = command.ExecuteReader()
                    While reader.Read()
                        lines.Add("E|" & Join(reader, 10))
                    End While
                End Using
            End Using
            Using command As SqliteCommand = connection.CreateCommand()
                command.CommandText = "SELECT symbols_observed, symbols_matched, symbols_reactivated, symbols_new, symbols_retired, registry_active_before, notes_orphaned, rename_candidates, unaccounted_observed, unaccounted_registry " &
                    "FROM extract_runs WHERE solution_id = @solution_id AND outcome = 'completed' ORDER BY id DESC LIMIT 1"
                command.Parameters.AddWithValue("@solution_id", solutionId)
                Using reader As SqliteDataReader = command.ExecuteReader()
                    While reader.Read()
                        lines.Add("C|" & Join(reader, 10))
                    End While
                End Using
            End Using
        End Using
        lines.Sort(StringComparer.Ordinal)
        Return lines
    End Function

    ''' <summary>
    ''' Dumps every column of every row of a table belonging to a solution, ordered by id, one line per row.
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <param name="table">One of <see cref="SnapshotTables"/>.</param>
    ''' <param name="solutionId">Solution id.</param>
    ''' <returns>The row dump.</returns>
    Public Function DumpTable(db As String, table As String, solutionId As Long) As List(Of String)
        Dim lines As List(Of String) = New List(Of String)()
        Using connection As SqliteConnection = Open(db)
            Using command As SqliteCommand = connection.CreateCommand()
                If table = "solutions" Then
                    command.CommandText = "SELECT * FROM solutions WHERE id = @solution_id ORDER BY id"
                ElseIf Array.IndexOf(FactTables, table) >= 0 Then
                    command.CommandText = "SELECT * FROM " & table & " WHERE solution_id = @solution_id ORDER BY id"
                Else
                    Throw New ArgumentOutOfRangeException(NameOf(table), table, "unknown table")
                End If
                command.Parameters.AddWithValue("@solution_id", solutionId)
                Using reader As SqliteDataReader = command.ExecuteReader()
                    While reader.Read()
                        lines.Add(Join(reader, reader.FieldCount))
                    End While
                End Using
            End Using
        End Using
        Return lines
    End Function

    Private Function Join(reader As SqliteDataReader, count As Integer) As String
        Dim values As String() = New String(count - 1) {}
        For i As Integer = 0 To count - 1
            values(i) = If(reader.IsDBNull(i), "<null>", Convert.ToString(reader.GetValue(i), Globalization.CultureInfo.InvariantCulture))
        Next
        Return String.Join("|", values)
    End Function

    Private Function NullableLong(reader As SqliteDataReader, index As Integer) As Long?
        If reader.IsDBNull(index) Then Return Nothing
        Return reader.GetInt64(index)
    End Function

    Private Function NullableString(reader As SqliteDataReader, index As Integer) As String
        If reader.IsDBNull(index) Then Return Nothing
        Return reader.GetString(index)
    End Function

End Module

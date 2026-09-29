' File: RenameCandidatesRepository.vb
' Project: CodeMem.Core
' Description: Append-only rename candidate proposals (FR-020, FR-022): no UPDATE, no DELETE; and their read for the bridge (feature 006).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-29 (feature 006, T008): ReadCandidates, SELECT-only, by solution with two optional filters (spec Q5, research R76).

Imports Microsoft.Data.Sqlite

''' <summary>
''' Writes candidates in the run that retired their retired row, and reads them back. Never applied by the extractor or the bridge.
''' </summary>
Public Module RenameCandidatesRepository

    ''' <summary>
    ''' Inserts one candidate.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionId">The solution.</param>
    ''' <param name="runId">This run.</param>
    ''' <param name="retiredSymbolId">The retired row.</param>
    ''' <param name="newSymbolId">The new row.</param>
    ''' <param name="bodyHash">The equal hash.</param>
    ''' <param name="samePath">Same-path evidence.</param>
    ''' <param name="offsetDistance">Offset distance or Nothing.</param>
    ''' <param name="rank">Rank from 1.</param>
    Public Sub Insert(db As MapDatabase, solutionId As Long, runId As Long, retiredSymbolId As Long, newSymbolId As Long, bodyHash As String, samePath As Boolean, offsetDistance As Integer?, rank As Integer)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "INSERT INTO rename_candidates (solution_id, run_id, retired_symbol_id, new_symbol_id, body_hash, same_path, offset_distance, rank) " &
                "VALUES (@solution_id, @run_id, @retired_symbol_id, @new_symbol_id, @body_hash, @same_path, @offset_distance, @rank)"
            command.Parameters.AddWithValue("@solution_id", solutionId)
            command.Parameters.AddWithValue("@run_id", runId)
            command.Parameters.AddWithValue("@retired_symbol_id", retiredSymbolId)
            command.Parameters.AddWithValue("@new_symbol_id", newSymbolId)
            command.Parameters.AddWithValue("@body_hash", bodyHash)
            command.Parameters.AddWithValue("@same_path", If(samePath, 1, 0))
            command.Parameters.AddWithValue("@offset_distance", If(offsetDistance.HasValue, CObj(offsetDistance.Value), CObj(DBNull.Value)))
            command.Parameters.AddWithValue("@rank", rank)
            command.ExecuteNonQuery()
        End Using
    End Sub

    ''' <summary>
    ''' The candidates of one solution, optionally narrowed to one run and to one retired symbol, ordered run id descending, then retired
    ''' id, rank and new id (feature 006, spec Q5).
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionId">The solution.</param>
    ''' <param name="runId">One run, or Nothing for every run.</param>
    ''' <param name="retiredSymbolId">One retired symbol, or Nothing for every one.</param>
    ''' <returns>The rows; empty when none match.</returns>
    Public Function ReadCandidates(db As MapDatabase, solutionId As Long, runId As Long?, retiredSymbolId As Long?) As List(Of RenameCandidateRecord)
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT id, solution_id, run_id, retired_symbol_id, new_symbol_id, same_path, offset_distance, rank FROM rename_candidates " &
                "WHERE solution_id = @solution_id AND (@run_id IS NULL OR run_id = @run_id) AND (@retired_symbol_id IS NULL OR retired_symbol_id = @retired_symbol_id) " &
                "ORDER BY run_id DESC, retired_symbol_id, rank, new_symbol_id"
            command.Parameters.AddWithValue("@solution_id", solutionId)
            command.Parameters.AddWithValue("@run_id", If(runId.HasValue, CObj(runId.Value), CObj(DBNull.Value)))
            command.Parameters.AddWithValue("@retired_symbol_id", If(retiredSymbolId.HasValue, CObj(retiredSymbolId.Value), CObj(DBNull.Value)))
            Dim rows As List(Of RenameCandidateRecord) = New List(Of RenameCandidateRecord)()
            Using reader As SqliteDataReader = command.ExecuteReader()
                While reader.Read()
                    rows.Add(New RenameCandidateRecord With {
                        .Id = reader.GetInt64(0),
                        .SolutionId = reader.GetInt64(1),
                        .RunId = reader.GetInt64(2),
                        .RetiredSymbolId = reader.GetInt64(3),
                        .NewSymbolId = reader.GetInt64(4),
                        .SamePath = reader.GetInt32(5) = 1,
                        .OffsetDistance = If(reader.IsDBNull(6), CType(Nothing, Integer?), reader.GetInt32(6)),
                        .Rank = reader.GetInt32(7)})
                End While
            End Using
            Return rows
        End Using
    End Function

End Module

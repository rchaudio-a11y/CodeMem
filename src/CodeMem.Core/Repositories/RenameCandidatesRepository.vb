' File: RenameCandidatesRepository.vb
' Project: CodeMem.Core
' Description: Append-only rename candidate proposals (FR-020, FR-022): no UPDATE, no DELETE.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports Microsoft.Data.Sqlite

''' <summary>
''' Writes candidates in the run that retired their retired row. Never applied by the extractor.
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

End Module

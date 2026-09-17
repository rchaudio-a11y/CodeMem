' File: ExtractRunWarningsRepository.vb
' Project: CodeMem.Core
' Description: The extract_run_warnings table's only SQL: one parameterised INSERT per warning of a run, project_path bound as NULL when the warning names no project; one SELECT by run (005 FR-429, FR-430; schema version 3; contracts/extractor.md §5).
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' 2026-09-17 (T036): ReadByRun - the bridge counts (solutions) and lists (extract) a run's warnings through it; its first fact is X02 (5).

Imports Microsoft.Data.Sqlite

''' <summary>
''' Written inside the run's open transaction, after the run row, for a completed and a failed run alike.
''' </summary>
Public Module ExtractRunWarningsRepository

    ''' <summary>
    ''' Inserts every warning of a run.
    ''' </summary>
    ''' <param name="db">The open map, inside the run's transaction.</param>
    ''' <param name="runId">The run row just inserted.</param>
    ''' <param name="warnings">The warnings, ProjectPath already solution-relative or Nothing.</param>
    Public Sub InsertAll(db As MapDatabase, runId As Long, warnings As IEnumerable(Of RunWarningRecord))
        For Each warning As RunWarningRecord In warnings
            Using command As SqliteCommand = db.CreateCommand()
                command.CommandText = "INSERT INTO extract_run_warnings (run_id, code, project_path, message) VALUES (@run_id, @code, @project_path, @message)"
                command.Parameters.AddWithValue("@run_id", runId)
                command.Parameters.AddWithValue("@code", warning.Code)
                command.Parameters.AddWithValue("@project_path", If(warning.ProjectPath Is Nothing, CObj(DBNull.Value), CObj(warning.ProjectPath)))
                command.Parameters.AddWithValue("@message", warning.Message)
                command.ExecuteNonQuery()
            End Using
        Next
    End Sub

    ''' <summary>
    ''' Reads a run's warnings in insertion order.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="runId">The run.</param>
    ''' <returns>The rows; empty when the run has none.</returns>
    Public Function ReadByRun(db As MapDatabase, runId As Long) As List(Of RunWarningRecord)
        Dim result As List(Of RunWarningRecord) = New List(Of RunWarningRecord)()
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "SELECT id, run_id, code, project_path, message FROM extract_run_warnings WHERE run_id = @run_id ORDER BY id"
            command.Parameters.AddWithValue("@run_id", runId)
            Using reader As SqliteDataReader = command.ExecuteReader()
                While reader.Read()
                    result.Add(New RunWarningRecord With {
                        .Id = reader.GetInt64(0),
                        .RunId = reader.GetInt64(1),
                        .Code = reader.GetString(2),
                        .ProjectPath = If(reader.IsDBNull(3), Nothing, reader.GetString(3)),
                        .Message = reader.GetString(4)})
                End While
            End Using
        End Using
        Return result
    End Function

End Module

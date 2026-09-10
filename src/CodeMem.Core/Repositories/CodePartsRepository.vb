' File: CodePartsRepository.vb
' Project: CodeMem.Core
' Description: The code_parts observation table, replaced wholesale per run for one solution only (Article VI, FR-012, FR-031).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports Microsoft.Data.Sqlite

''' <summary>
''' Replaces a solution's parts. The DELETE here is scoped to solution_id and never touches code_symbols.
''' </summary>
Public Module CodePartsRepository

    ''' <summary>
    ''' Deletes the solution's parts and inserts the staged ones.
    ''' </summary>
    ''' <param name="db">The open map.</param>
    ''' <param name="solutionId">The solution.</param>
    ''' <param name="parts">Symbol row id paired with each staged part, in insert order.</param>
    Public Sub ReplaceForSolution(db As MapDatabase, solutionId As Long, parts As IEnumerable(Of KeyValuePair(Of Long, ObservedPart)))
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "DELETE FROM code_parts WHERE solution_id = @solution_id"
            command.Parameters.AddWithValue("@solution_id", solutionId)
            command.ExecuteNonQuery()
        End Using
        Using command As SqliteCommand = db.CreateCommand()
            command.CommandText = "INSERT INTO code_parts (solution_id, symbol_id, path, start_offset, length, start_line, start_column, part_hash) " &
                "VALUES (@solution_id, @symbol_id, @path, @start_offset, @length, @start_line, @start_column, @part_hash)"
            Dim pSolution As SqliteParameter = command.Parameters.AddWithValue("@solution_id", solutionId)
            Dim pSymbol As SqliteParameter = command.Parameters.Add("@symbol_id", SqliteType.Integer)
            Dim pPath As SqliteParameter = command.Parameters.Add("@path", SqliteType.Text)
            Dim pOffset As SqliteParameter = command.Parameters.Add("@start_offset", SqliteType.Integer)
            Dim pLength As SqliteParameter = command.Parameters.Add("@length", SqliteType.Integer)
            Dim pLine As SqliteParameter = command.Parameters.Add("@start_line", SqliteType.Integer)
            Dim pColumn As SqliteParameter = command.Parameters.Add("@start_column", SqliteType.Integer)
            Dim pHash As SqliteParameter = command.Parameters.Add("@part_hash", SqliteType.Text)
            For Each pair As KeyValuePair(Of Long, ObservedPart) In parts
                pSymbol.Value = pair.Key
                pPath.Value = pair.Value.Location.Path
                pOffset.Value = pair.Value.Location.StartOffset
                pLength.Value = pair.Value.Location.Length
                pLine.Value = pair.Value.Location.StartLine
                pColumn.Value = pair.Value.Location.StartColumn
                pHash.Value = pair.Value.PartHash
                command.ExecuteNonQuery()
            Next
        End Using
    End Sub

End Module

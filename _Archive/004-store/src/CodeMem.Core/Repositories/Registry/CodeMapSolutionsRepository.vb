' File: CodeMapSolutionsRepository.vb
' Project: CodeMem.Core
' Description: The only SQL that names a store table: reads every code_map_solutions row (feature 004, FR-304, research R45, INC1).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports Microsoft.Data.Sqlite

''' <summary>
''' One SELECT over the registry, nothing else in the store; a missing table is learned from the query's own error, never from sqlite_master.
''' </summary>
Public Module CodeMapSolutionsRepository

    ''' <summary>
    ''' Reads every registry row ordered by solution key.
    ''' </summary>
    ''' <param name="store">The open, read-only store.</param>
    ''' <returns>The rows.</returns>
    ''' <exception cref="RegistryTableMissingException">The store holds no code_map_solutions table.</exception>
    Public Function ReadAll(store As StoreDatabase) As List(Of RegistryRecord)
        Dim rows As List(Of RegistryRecord) = New List(Of RegistryRecord)()
        Try
            Using command As SqliteCommand = store.CreateCommand()
                command.CommandText = "SELECT id, project_id, solution_key, codemem_solution_id, extraction_scope, state, notes FROM code_map_solutions ORDER BY solution_key"
                Using reader As SqliteDataReader = command.ExecuteReader()
                    While reader.Read()
                        Dim row As RegistryRecord = New RegistryRecord With {
                            .Id = reader.GetInt64(0),
                            .ProjectId = reader.GetInt64(1),
                            .SolutionKey = reader.GetString(2),
                            .ExtractionScope = reader.GetString(4),
                            .State = reader.GetString(5),
                            .Notes = If(reader.IsDBNull(6), Nothing, reader.GetString(6))}
                        If Not reader.IsDBNull(3) Then row.CodememSolutionId = reader.GetInt64(3)
                        rows.Add(row)
                    End While
                End Using
            End Using
        Catch ex As SqliteException When ex.SqliteErrorCode = 1 AndAlso ex.Message.Contains("no such table")
            Throw New RegistryTableMissingException(ex.Message, ex)
        End Try
        Return rows
    End Function

End Module

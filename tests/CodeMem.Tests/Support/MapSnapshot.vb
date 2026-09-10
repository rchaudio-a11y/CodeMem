' File: MapSnapshot.vb
' Project: CodeMem.Tests
' Description: Byte and row snapshots of a map for before/after comparisons (I1, I8, I9, I11).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.IO
Imports System.Security.Cryptography

''' <summary>
''' Snapshots a map file as bytes or as per-table ordered row dumps.
''' </summary>
Public Module MapSnapshot

    ''' <summary>
    ''' SHA-256 hex of the file's bytes.
    ''' </summary>
    ''' <param name="path">File path.</param>
    ''' <returns>64 lowercase hex characters.</returns>
    Public Function FileBytesHash(path As String) As String
        Using stream As FileStream = File.OpenRead(path)
            Return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant()
        End Using
    End Function

    ''' <summary>
    ''' Per-table ordered row dumps of everything belonging to one solution.
    ''' </summary>
    ''' <param name="db">Map path.</param>
    ''' <param name="solutionId">Solution id.</param>
    ''' <returns>Table name to row lines.</returns>
    Public Function RowsForSolution(db As String, solutionId As Long) As Dictionary(Of String, List(Of String))
        Dim result As Dictionary(Of String, List(Of String)) = New Dictionary(Of String, List(Of String))(StringComparer.Ordinal)
        For Each table As String In MapQueries.SnapshotTables()
            result(table) = MapQueries.DumpTable(db, table, solutionId)
        Next
        Return result
    End Function

End Module

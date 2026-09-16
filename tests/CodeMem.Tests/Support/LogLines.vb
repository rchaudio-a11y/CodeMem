' File: LogLines.vb
' Project: CodeMem.Tests
' Description: Reads the extract.log beside the test host and picks one fact's lines by a marker (feature 004, T038; INC3, U2).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.IO

''' <summary>
''' The log is one file shared by every in-process fact and by the spawned executable (both live in the test output directory), so a fact
''' identifies its own lines by the unique key or path it used and counts those, never the file's length.
''' </summary>
Public Module LogLines

    ''' <summary>The log path beside the test host.</summary>
    ''' <returns>The path.</returns>
    Public Function LogPath() As String
        Return Path.Combine(AppContext.BaseDirectory, "extract.log")
    End Function

    ''' <summary>
    ''' The lines containing a marker, split into their tab-separated columns.
    ''' </summary>
    ''' <param name="marker">The unique key or path the fact used.</param>
    ''' <returns>The matching lines' columns.</returns>
    Public Function Containing(marker As String) As List(Of String())
        Dim result As List(Of String()) = New List(Of String())()
        If Not File.Exists(LogPath()) Then Return result
        Dim text As String
        Using stream As FileStream = New FileStream(LogPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Using reader As StreamReader = New StreamReader(stream)
                text = reader.ReadToEnd()
            End Using
        End Using
        For Each line As String In text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            If line.Contains(marker, StringComparison.Ordinal) Then result.Add(line.Split(vbTab(0)))
        Next
        Return result
    End Function

End Module

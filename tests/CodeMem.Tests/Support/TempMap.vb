' File: TempMap.vb
' Project: CodeMem.Tests
' Description: A throwaway codemem.sqlite at a unique temporary path, deleted on dispose (Article IX: role, not path).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.IO
Imports Microsoft.Data.Sqlite

''' <summary>
''' Mints a unique map path under the temp directory and removes the file (and any journal) on dispose.
''' </summary>
Public Class TempMap
    Implements IDisposable

    ''' <summary>The map file path. The file does not exist until an extractor creates it.</summary>
    Public ReadOnly Property Path As String

    ''' <summary>
    ''' Mints the path.
    ''' </summary>
    Public Sub New()
        Dim dir As String = IO.Path.Combine(IO.Path.GetTempPath(), "codemem-tests")
        Directory.CreateDirectory(dir)
        Path = IO.Path.Combine(dir, "map-" & Guid.NewGuid().ToString("N") & ".sqlite")
    End Sub

    ''' <summary>
    ''' Deletes the map file and its journal if present.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        SqliteConnection.ClearAllPools()
        For Each candidate As String In New String() {Path, Path & "-journal"}
            Try
                If File.Exists(candidate) Then File.Delete(candidate)
            Catch ex As IOException
                ' best effort: a leaked temp file is not a test failure
            End Try
        Next
    End Sub

End Class

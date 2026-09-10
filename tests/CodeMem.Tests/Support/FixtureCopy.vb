' File: FixtureCopy.vb
' Project: CodeMem.Tests
' Description: A temporary, restored copy of the fixture solution for tests that mutate source (FR-036).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.IO

''' <summary>
''' Copies <c>Fixtures/Sample</c> (without bin/ and obj/) to a temp directory, restores it, and lets a test edit one anchor at a time.
''' </summary>
Public Class FixtureCopy
    Implements IDisposable

    ''' <summary>Absolute path of the copied directory.</summary>
    Public ReadOnly Property Directory As String

    ''' <summary>Absolute path of the copied <c>Sample.sln</c>.</summary>
    Public ReadOnly Property SolutionPath As String

    ''' <summary>
    ''' Copies and restores the fixture.
    ''' </summary>
    Public Sub New()
        Directory = Path.Combine(Path.GetTempPath(), "codemem-tests", "fixture-" & Guid.NewGuid().ToString("N"))
        CopyTree(RepoPaths.FixtureDirectory(), Directory)
        SolutionPath = Path.Combine(Directory, "Sample.sln")
        DotnetCli.Run("restore """ & SolutionPath & """", Directory)
    End Sub

    ''' <summary>
    ''' Replaces exactly one occurrence of an anchor in a file of the copy.
    ''' </summary>
    ''' <param name="relativeFile">Path relative to the copy, forward slashes.</param>
    ''' <param name="find">The anchor text; must occur exactly once.</param>
    ''' <param name="replaceWith">The replacement text.</param>
    Public Sub Replace(relativeFile As String, find As String, replaceWith As String)
        Dim file As String = Path.Combine(Directory, relativeFile.Replace("/"c, Path.DirectorySeparatorChar))
        Dim text As String = IO.File.ReadAllText(file)
        Dim first As Integer = text.IndexOf(find, StringComparison.Ordinal)
        If first < 0 Then
            Throw New InvalidOperationException("anchor not found in " & relativeFile & ": " & find)
        End If
        If text.IndexOf(find, first + 1, StringComparison.Ordinal) >= 0 Then
            Throw New InvalidOperationException("anchor occurs more than once in " & relativeFile & ": " & find)
        End If
        IO.File.WriteAllText(file, text.Substring(0, first) & replaceWith & text.Substring(first + find.Length))
    End Sub

    ''' <summary>
    ''' Deletes the copy, best effort.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Try
            If IO.Directory.Exists(Directory) Then IO.Directory.Delete(Directory, True)
        Catch ex As IOException
            ' best effort
        Catch ex As UnauthorizedAccessException
            ' best effort
        End Try
    End Sub

    Private Shared Sub CopyTree(source As String, target As String)
        IO.Directory.CreateDirectory(target)
        For Each file As String In IO.Directory.GetFiles(source)
            IO.File.Copy(file, Path.Combine(target, Path.GetFileName(file)))
        Next
        For Each dir As String In IO.Directory.GetDirectories(source)
            Dim name As String = Path.GetFileName(dir)
            If String.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) Then
                Continue For
            End If
            CopyTree(dir, Path.Combine(target, name))
        Next
    End Sub

End Class

' File: RepoPaths.vb
' Project: CodeMem.Tests
' Description: Locates the repository root, the test project directory and the fixture directory from the test output directory.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.IO

''' <summary>
''' Resolves well-known repository paths by walking up from <see cref="AppContext.BaseDirectory"/>.
''' </summary>
Public Module RepoPaths

    ''' <summary>
    ''' The directory containing <c>CodeMem.Tests.vbproj</c>.
    ''' </summary>
    ''' <returns>The absolute test project directory.</returns>
    Public Function TestProjectDirectory() As String
        Return FindUp(AppContext.BaseDirectory, "CodeMem.Tests.vbproj")
    End Function

    ''' <summary>
    ''' The directory containing <c>CodeMem.sln</c>.
    ''' </summary>
    ''' <returns>The absolute repository root.</returns>
    Public Function RepositoryRoot() As String
        Return FindUp(AppContext.BaseDirectory, "CodeMem.sln")
    End Function

    ''' <summary>
    ''' The committed fixture solution directory <c>Fixtures/Sample</c>.
    ''' </summary>
    ''' <returns>The absolute fixture directory.</returns>
    Public Function FixtureDirectory() As String
        Return Path.Combine(TestProjectDirectory(), "Fixtures", "Sample")
    End Function

    ''' <summary>
    ''' The build configuration this test assembly was built under (the segment after <c>bin</c>).
    ''' </summary>
    ''' <returns><c>Debug</c> or <c>Release</c>.</returns>
    Public Function Configuration() As String
        Dim parts As String() = AppContext.BaseDirectory.Split(New Char() {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar})
        For i As Integer = 0 To parts.Length - 2
            If String.Equals(parts(i), "bin", StringComparison.OrdinalIgnoreCase) Then
                Return parts(i + 1)
            End If
        Next
        Return "Debug"
    End Function

    Private Function FindUp(start As String, marker As String) As String
        Dim dir As DirectoryInfo = New DirectoryInfo(start)
        While dir IsNot Nothing
            If File.Exists(Path.Combine(dir.FullName, marker)) Then
                Return dir.FullName
            End If
            dir = dir.Parent
        End While
        Throw New DirectoryNotFoundException("Could not find " & marker & " above " & start)
    End Function

End Module

' File: V1MapFixture.vb
' Project: CodeMem.Tests
' Description: Copies the committed version-1 map fixture (Fixtures/Maps/sample-v1.sqlite, research R30) to a throwaway path so a test can upgrade it.
' Author: RCH Automation LLC
' Created: 2026-09-10

Imports System.IO

''' <summary>
''' The map the Stage A extractor wrote, copied per test. The committed file is never opened in place. No SQL here.
''' </summary>
Public Module V1MapFixture

    ''' <summary>
    ''' Copies <c>Fixtures/Maps/sample-v1.sqlite</c> from the test output directory to the path of a fresh <see cref="TempMap"/>.
    ''' </summary>
    ''' <param name="map">The temp map whose path receives the copy; the caller owns and disposes it.</param>
    ''' <returns>The copied file's path (<c>map.Path</c>).</returns>
    Public Function CopyToTemp(map As TempMap) As String
        Dim source As String = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Maps", "sample-v1.sqlite")
        If Not File.Exists(source) Then
            Throw New FileNotFoundException("version-1 map fixture not copied beside the tests", source)
        End If
        File.Copy(source, map.Path, True)
        Return map.Path
    End Function

End Module

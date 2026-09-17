' File: V2MapFixture.vb
' Project: CodeMem.Tests
' Description: Copies the committed version-2 map fixture (Fixtures/Maps/version2.sqlite, feature 005) to a throwaway path so a test can upgrade it to version 3.
' Author: RCH Automation LLC
' Created: 2026-09-17

Imports System.IO

''' <summary>
''' The map the 004-era extractor (schema version 2) wrote for the Sample fixture on 2026-09-17, copied per test. The committed file is
''' never opened in place. No SQL here.
''' </summary>
Public Module V2MapFixture

    ''' <summary>
    ''' Copies <c>Fixtures/Maps/version2.sqlite</c> from the test output directory to the path of a fresh <see cref="TempMap"/>.
    ''' </summary>
    ''' <param name="map">The temp map whose path receives the copy; the caller owns and disposes it.</param>
    ''' <returns>The copied file's path (<c>map.Path</c>).</returns>
    Public Function CopyToTemp(map As TempMap) As String
        Dim source As String = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Maps", "version2.sqlite")
        If Not File.Exists(source) Then
            Throw New FileNotFoundException("version-2 map fixture not copied beside the tests", source)
        End If
        File.Copy(source, map.Path, True)
        Return map.Path
    End Function

End Module

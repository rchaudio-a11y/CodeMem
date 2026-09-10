' File: SqlLocationGateTests.vb
' Project: CodeMem.Tests
' Description: Review gate - SQL lives only in Core/Repositories and Core/Schema (and MapQueries in tests); the map is the only database opened (FR-003).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' Exclusions in tests/: Support/MapQueries.vb (the named test queries), Guards/SchemaConstraintTests.vb (direct inserts by design),
' and the two SQL-scanning guards, this file and Guards/TripwireTests.vb, whose literals name the SQL they search for.
' GREEN: 2026-09-09 first run.
' FIRE:  2026-09-09 added a "SELECT 1" literal to ExtractionRun.vb -> red (ExtractionRun.vb named); reverted -> green.

Imports System.IO
Imports System.Text.RegularExpressions
Imports Xunit

''' <summary>
''' Scans string literals for SQL keywords outside the repository folders, and counts SqliteConnection construction sites in production.
''' </summary>
Public Class SqlLocationGateTests

    Private Shared ReadOnly StringLiteral As Regex = New Regex("""(?:[^""]|"""")*""", RegexOptions.Compiled)
    Private Shared ReadOnly SqlKeyword As Regex = New Regex("\b(SELECT|INSERT|UPDATE|DELETE|CREATE TABLE|PRAGMA)\b", RegexOptions.Compiled)

    ''' <summary>
    ''' No SQL keyword in a string literal outside the allowed files.
    ''' </summary>
    <Fact>
    Public Sub SqlAppearsOnlyInRepositories()
        Dim root As String = RepoPaths.RepositoryRoot()
        Dim offenders As List(Of String) = New List(Of String)()
        For Each file As String In SourceFiles(Path.Combine(root, "src"))
            Dim normalized As String = file.Replace("\"c, "/"c)
            If normalized.Contains("/CodeMem.Core/Repositories/") OrElse normalized.Contains("/CodeMem.Core/Schema/") Then Continue For
            Scan(file, offenders)
        Next
        For Each file As String In SourceFiles(Path.Combine(root, "tests"))
            Dim normalized As String = file.Replace("\"c, "/"c)
            If normalized.EndsWith("/Support/MapQueries.vb", StringComparison.Ordinal) OrElse normalized.EndsWith("/Guards/SchemaConstraintTests.vb", StringComparison.Ordinal) OrElse
               normalized.EndsWith("/Guards/TripwireTests.vb", StringComparison.Ordinal) OrElse normalized.EndsWith("/Guards/SqlLocationGateTests.vb", StringComparison.Ordinal) Then Continue For
            Scan(file, offenders)
        Next
        Assert.True(offenders.Count = 0, String.Join(Environment.NewLine, offenders))
    End Sub

    ''' <summary>
    ''' New SqliteConnection appears in exactly one production file, MapDatabase.vb.
    ''' </summary>
    <Fact>
    Public Sub OnlyMapDatabaseOpensAConnection()
        Dim root As String = RepoPaths.RepositoryRoot()
        Dim sites As List(Of String) = New List(Of String)()
        For Each file As String In SourceFiles(Path.Combine(root, "src"))
            If IO.File.ReadAllText(file).Contains("New SqliteConnection") Then sites.Add(Path.GetFileName(file))
        Next
        Assert.Equal(New String() {"MapDatabase.vb"}, sites.ToArray())
    End Sub

    ''' <summary>
    ''' Every .vb file under a root, excluding Fixtures, bin and obj.
    ''' </summary>
    ''' <param name="root">The directory to scan.</param>
    ''' <returns>The file paths.</returns>
    Public Shared Function SourceFiles(root As String) As List(Of String)
        Dim result As List(Of String) = New List(Of String)()
        For Each file As String In Directory.GetFiles(root, "*.vb", SearchOption.AllDirectories)
            Dim normalized As String = file.Replace("\"c, "/"c)
            If normalized.Contains("/Fixtures/") OrElse normalized.Contains("/bin/") OrElse normalized.Contains("/obj/") Then Continue For
            result.Add(file)
        Next
        Return result
    End Function

    Private Shared Sub Scan(file As String, offenders As List(Of String))
        For Each match As Match In StringLiteral.Matches(IO.File.ReadAllText(file))
            If SqlKeyword.IsMatch(match.Value) Then offenders.Add(Path.GetFileName(file) & ": " & match.Value)
        Next
    End Sub

End Class

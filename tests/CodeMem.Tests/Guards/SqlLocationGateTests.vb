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
'
' 2026-09-10 (fixpack 002, F10 / FR-121): the keyword regex matches with RegexOptions.IgnoreCase over whitespace-normalised literals
' (research R29); the New SqliteConnection count is unchanged. The 2026-09-09 fire was canonical spelling; the re-fire below is lowercase.
' FIRE:  2026-09-10 (T044) added a "select 1" literal (lowercase) to ExtractionRun.vb -> red (ExtractionRun.vb: "select 1" named); reverted -> green.
'
' 2026-09-15 (004): Support/RegistryFixture.vb holds the 029 §1 DDL, transcribed - a copy of a contract, not a reference - and
' Guards/BridgeSqlGateTests.vb is the third SQL-scanning guard whose literals name the SQL they search for; both excluded.
' RED:   2026-09-15 (T012) OnlyMapDatabaseOpensAConnection red the moment StoreDatabase.vb landed: expected ["MapDatabase.vb"], actual
'        ["MapDatabase.vb", "StoreDatabase.vb"] - the named Red of constitution v1.3.0's amendment; the assertion is amended below to the two
'        files the Review Gate names (the map's door, read-write for the extractor and read-only for the bridge; the store's door, read-only).
' FIRE:  2026-09-15 (T012) added Dim probe As SqliteConnection = New SqliteConnection(...) to src/CodeMem.Bridge/Program.vb (source scan,
'        no rebuild) -> red naming Program.vb (expected the two files, actual three); reverted -> green.

Imports System.IO
Imports System.Text.RegularExpressions
Imports Xunit

''' <summary>
''' Scans string literals for SQL keywords outside the repository folders, and counts SqliteConnection construction sites in production.
''' </summary>
Public Class SqlLocationGateTests

    Private Shared ReadOnly StringLiteral As Regex = New Regex("""(?:[^""]|"""")*""", RegexOptions.Compiled)
    Private Shared ReadOnly SqlKeyword As Regex = New Regex("\b(SELECT|INSERT|UPDATE|DELETE|CREATE TABLE|PRAGMA)\b", RegexOptions.Compiled Or RegexOptions.IgnoreCase)

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
               normalized.EndsWith("/Guards/TripwireTests.vb", StringComparison.Ordinal) OrElse normalized.EndsWith("/Guards/SqlLocationGateTests.vb", StringComparison.Ordinal) OrElse
               normalized.EndsWith("/Support/RegistryFixture.vb", StringComparison.Ordinal) OrElse normalized.EndsWith("/Guards/BridgeSqlGateTests.vb", StringComparison.Ordinal) Then Continue For
            Scan(file, offenders)
        Next
        Assert.True(offenders.Count = 0, String.Join(Environment.NewLine, offenders))
    End Sub

    ''' <summary>
    ''' New SqliteConnection appears in exactly two production files, MapDatabase.vb and StoreDatabase.vb (constitution v1.3.0, 2026-09-15).
    ''' </summary>
    <Fact>
    Public Sub OnlyMapDatabaseOpensAConnection()
        Dim root As String = RepoPaths.RepositoryRoot()
        Dim sites As List(Of String) = New List(Of String)()
        For Each file As String In SourceFiles(Path.Combine(root, "src"))
            If IO.File.ReadAllText(file).Contains("New SqliteConnection") Then sites.Add(Path.GetFileName(file))
        Next
        sites.Sort(StringComparer.Ordinal)
        Assert.Equal(New String() {"MapDatabase.vb", "StoreDatabase.vb"}, sites.ToArray())
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
            Dim literal As String = TripwireTests.Normalise(match.Value)
            If SqlKeyword.IsMatch(literal) Then offenders.Add(Path.GetFileName(file) & ": " & literal)
        Next
    End Sub

End Class

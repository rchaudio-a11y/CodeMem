' File: BridgeStandaloneGateTests.vb
' Project: CodeMem.Tests
' Description: Review gate (feature 005, FR-401, FR-402, FR-431): the bridge projects name no second database; the archived store names are gone from src/ and tests/; Article IX reads as v1.2.1; the constitution is v1.4.0.
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' (1) is a guard on a path, not a word (analyze I1): the file is memos.sqlite and a path segment is lowercase, so the scan is case-sensitive
' and prose that says MemOS (the ProjectIdRemoved refusal text) passes. Both text scans assert a positive file count first (Article II:
' a scan over nothing passes vacuously). (3) compares the article's body with every run of whitespace collapsed, so a re-wrap is not a
' difference but a changed word is.
' RED:   2026-09-17 (T002) 4 of 4 for their stated reasons - (1) "BridgeConfig.vb:15: memos" (the XML doc reads memos.sqlite); (2)
'        "ExtractDoor.vb: RegistryRecord" and the other store names across Bridging, Core and the tests; (3) the article's body carries
'        "One exception is ruled"; (4) the version line reads 1.3.0. (3) and (4) go green at T003; (1) and (2) at T024, the archive.
' GREEN: 2026-09-17 (T003) (3) and (4) after the v1.4.0 amendment (the exception paragraph removed, the gate line at one file, the entry
'        and the version line); (1) and (2) still red as named.
' FIRE:  2026-09-17 (T003) (3): "the sole writer of fact tables" changed to "the only writer" in the constitution -> red (Actual: "...the
'        only writer of fact t..."); reverted -> green.

Imports System.IO
Imports System.Text.RegularExpressions
Imports Xunit

''' <summary>
''' Four facts over the source tree and the constitution: no SqliteConnection and no store path under the two bridge projects; the eight
''' archived names absent under src/ and tests/; Article IX equal to its v1.2.1 body; the version line at 1.4.0.
''' </summary>
Public Class BridgeStandaloneGateTests

    Private Shared ReadOnly ArchivedNames As String() = New String() {
        "StoreDatabase", "CodeMapSolutionsRepository", "RegistryTableMissingException", "RegistryRecord",
        "StoreAccess", "UnboundEntryEnvelope", "InactiveEntryEnvelope", "RegistryFixture"}

    Private Shared ReadOnly Whitespace As Regex = New Regex("\s+", RegexOptions.Compiled)

    ''' <summary>The body of Article IX at constitution v1.2.1 (2026-09-10), whitespace collapsed: the text v1.4.0 returns to.</summary>
    Private Const ArticleNineV121 As String =
        "### IX. One File, Many Solutions, One Writer " &
        "codemem.sqlite holds every solution it maps. Every fact row carries solution_id; the extractor's " &
        "write scope for a run is exactly WHERE solution_id = the solution being extracted. The extractor is " &
        "the sole writer of fact tables. A one-row map_identity table holds a GUID minted when the file is " &
        "first created and never rewritten. CodeMem opens no database in any role other than its own map. " &
        "The constraint is on role, not path: tests create throwaway map files at temporary paths and those " &
        "are still codemem.sqlite in role. " &
        "Rationale: A single writer against a single file is the cheapest guarantee that no run can " &
        "half-overwrite another solution's map."

    ''' <summary>
    ''' (1) Every .vb file under src/CodeMem.Bridge and src/CodeMem.Bridging (at least one found) is free of SqliteConnection and of the
    ''' case-sensitive text memos (FR-401).
    ''' </summary>
    <Fact>
    Public Sub TheBridgeProjectsOpenOnlyTheMap()
        Dim root As String = RepoPaths.RepositoryRoot()
        Dim files As List(Of String) = New List(Of String)()
        For Each folder As String In New String() {Path.Combine(root, "src", "CodeMem.Bridge"), Path.Combine(root, "src", "CodeMem.Bridging")}
            If Directory.Exists(folder) Then files.AddRange(SqlLocationGateTests.SourceFiles(folder))
        Next
        Assert.True(files.Count > 0, "no .vb file under the bridge projects: the scan is vacuous")
        Dim offenders As List(Of String) = New List(Of String)()
        For Each file As String In files
            Dim lines As String() = IO.File.ReadAllLines(file)
            For i As Integer = 0 To lines.Length - 1
                If lines(i).Contains("SqliteConnection", StringComparison.Ordinal) Then offenders.Add(Path.GetFileName(file) & ":" & (i + 1) & ": SqliteConnection")
                If lines(i).Contains("memos", StringComparison.Ordinal) Then offenders.Add(Path.GetFileName(file) & ":" & (i + 1) & ": memos")
            Next
        Next
        Assert.True(offenders.Count = 0, String.Join(Environment.NewLine, offenders))
    End Sub

    ''' <summary>
    ''' (2) None of the eight archived identifiers appears in any .vb file under src/ or tests/ (this file excepted by name) (FR-402).
    ''' </summary>
    <Fact>
    Public Sub TheArchivedNamesAreAbsent()
        Dim root As String = RepoPaths.RepositoryRoot()
        Dim files As List(Of String) = SqlLocationGateTests.SourceFiles(Path.Combine(root, "src"))
        files.AddRange(SqlLocationGateTests.SourceFiles(Path.Combine(root, "tests")))
        Assert.True(files.Count > 0, "no .vb file under src/ or tests/: the scan is vacuous")
        Dim offenders As List(Of String) = New List(Of String)()
        For Each file As String In files
            If String.Equals(Path.GetFileName(file), "BridgeStandaloneGateTests.vb", StringComparison.Ordinal) Then Continue For
            Dim text As String = IO.File.ReadAllText(file)
            For Each name As String In ArchivedNames
                If Regex.IsMatch(text, "\b" & name & "\b") Then offenders.Add(Path.GetFileName(file) & ": " & name)
            Next
        Next
        Assert.True(offenders.Count = 0, String.Join(Environment.NewLine, offenders))
    End Sub

    ''' <summary>
    ''' (3) The text of the constitution from the Article IX heading to the Article X heading equals the v1.2.1 body, whitespace collapsed (FR-431).
    ''' </summary>
    <Fact>
    Public Sub ArticleNineReadsAsVersionOneTwoOne()
        Dim lines As String() = IO.File.ReadAllLines(ConstitutionPath())
        Dim collecting As Boolean = False
        Dim collected As List(Of String) = New List(Of String)()
        For Each line As String In lines
            If line.StartsWith("### IX.", StringComparison.Ordinal) Then collecting = True
            If line.StartsWith("### X.", StringComparison.Ordinal) Then Exit For
            If collecting Then collected.Add(line)
        Next
        Assert.True(collected.Count > 0, "Article IX heading not found in the constitution")
        Dim actual As String = Whitespace.Replace(String.Join(" ", collected), " ").Trim()
        Assert.Equal(ArticleNineV121, actual)
    End Sub

    ''' <summary>
    ''' (4) The constitution's version line begins **Version**: 1.4.0 (FR-431).
    ''' </summary>
    <Fact>
    Public Sub TheConstitutionIsVersionOneFour()
        Dim versionLine As String = Nothing
        For Each line As String In IO.File.ReadAllLines(ConstitutionPath())
            If line.StartsWith("**Version**:", StringComparison.Ordinal) Then versionLine = line
        Next
        Assert.NotNull(versionLine)
        Assert.StartsWith("**Version**: 1.4.0", versionLine, StringComparison.Ordinal)
    End Sub

    Private Shared Function ConstitutionPath() As String
        Return Path.Combine(RepoPaths.RepositoryRoot(), ".specify", "memory", "constitution.md")
    End Function

End Class

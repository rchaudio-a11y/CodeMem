' File: I09_AtomicPublishTests.vb
' Project: CodeMem.Tests
' Description: I9 - a process aborted after staging or during publication leaves every published table equal to the previous run (SC-007).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 AfterStaging green; DuringPublish red because a second run over an existing registry exits 4 before publication (no (A) matching until T096) - reported to the Architect.
' GREEN: 2026-09-09 after (A) matching (T096): AfterStaging and DuringPublish both leave every table equal; the unsynced journal case is asserted as documented above.
' FIRE:  2026-09-09 moved the DuringPublish abort to after Commit -> red (extract_runs changed after abort); reverted -> green.
'
' Order matters: RowsForSolution opens the database first, which performs SQLite's hot-journal rollback; the journal
' check comes after that read. A journal check before the read would be a false Red.
'
' Observed 2026-09-09: after the DuringPublish abort the tables were identical but a -journal file remained. SQLite
' zeroes a rollback journal's header until the journal is synced (which happens only before the database file is
' written); a killed process leaves a journal whose first byte is 0, which readers treat as "not hot" and ignore
' without deleting (pager.c hasHotJournal). The database is untouched; the next write transaction reuses and
' removes the file. The assertion therefore is: any journal left after the read is unsynced (first byte 0), and no
' journal remains after the next successful run.

Imports System.IO
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Atomic publication proven by killing the real executable at the two seam points.
''' </summary>
<Collection("Fixture")>
Public Class I09_AtomicPublishTests

    ''' <summary>
    ''' Abort at AfterStaging and at DuringPublish: non-zero exit, tables identical to the previous run, no journal left after the read.
    ''' </summary>
    <Fact>
    Public Sub AbortedRunLeavesPublishedTablesUntouched()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}, Nothing))
                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                Dim before As Dictionary(Of String, List(Of String)) = MapSnapshot.RowsForSolution(map.Path, solutionId)
                copy.Replace("Sample.Lib/Widgets.vb", "Sub Describe(", "Sub Explain(")

                For Each phase As String In New String() {"AfterStaging", "DuringPublish"}
                    Dim environment As Dictionary(Of String, String) = New Dictionary(Of String, String) From {{"CODEMEM_TEST_ABORT_AT", phase}}
                    Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(copy.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path), environment)
                    Assert.NotEqual(0, run.ExitCode)

                    Dim after As Dictionary(Of String, List(Of String)) = MapSnapshot.RowsForSolution(map.Path, solutionId)
                    For Each table As String In MapQueries.SnapshotTables()
                        Assert.True(before(table).SequenceEqual(after(table)), phase & ": " & table & " changed after abort")
                    Next
                    Dim journal As String = map.Path & "-journal"
                    If File.Exists(journal) Then
                        Dim bytes As Byte() = File.ReadAllBytes(journal)
                        Assert.True(bytes.Length = 0 OrElse bytes(0) = 0, phase & ": a synced (hot) journal remains after the read")
                    End If
                Next

                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}, Nothing))
                Assert.False(File.Exists(map.Path & "-journal"), "journal remains after the next successful run")
                Dim published As Dictionary(Of String, List(Of String)) = MapSnapshot.RowsForSolution(map.Path, solutionId)
                Assert.NotEqual(before("extract_runs").Count, published("extract_runs").Count)
            End Using
        End Using
    End Sub

End Class

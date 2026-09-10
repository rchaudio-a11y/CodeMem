' File: I05_IdentityMatchTests.vb
' Project: CodeMem.Tests
' Description: I5 - an unchanged re-extraction keeps every id, advances last_seen_run_id, and counts everything as matched (Article VI (A)).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 ResidualMismatch: a second run reported every symbol new and unaccounted_registry = 38 (no (A)) - reported to the Architect.
' GREEN: 2026-09-09 after (A) matching + retirement in Reconciler and RefreshMatched/Retire (T096).

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Identity match on (solution_id, doc_comment_id) against active rows.
''' </summary>
<Collection("Fixture")>
Public Class I05_IdentityMatchTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' Run twice unchanged: same ids, first_seen unchanged, last_seen = run 2, all matched, nothing new, retired or reactivated, residuals zero.
    ''' </summary>
    <Fact>
    Public Sub UnchangedRerunKeepsEveryId()
        Using map As TempMap = New TempMap()
            Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim first As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)
            Assert.True(first.Count > 0, "no symbols after run 1")

            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
            Dim runs As List(Of RunRow) = MapQueries.ReadRuns(map.Path)
            Assert.Equal(2, runs.Count)
            Dim second As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)
            Assert.Equal(first.Count, second.Count)

            For i As Integer = 0 To first.Count - 1
                Assert.Equal(first(i).Id, second(i).Id)
                Assert.Equal(first(i).DocCommentId, second(i).DocCommentId)
                Assert.Equal(first(i).FirstSeenRunId, second(i).FirstSeenRunId)
                Assert.Equal(runs(1).Id, second(i).LastSeenRunId)
                Assert.True(second(i).IsActive, "row retired on an unchanged rerun: " & second(i).DocCommentId)
            Next

            Dim run2 As RunRow = runs(1)
            Assert.Equal(run2.SymbolsObserved, run2.SymbolsMatched)
            Assert.Equal(0, run2.SymbolsNew)
            Assert.Equal(0, run2.SymbolsRetired)
            Assert.Equal(0, run2.SymbolsReactivated)
            Assert.Equal(run2.SymbolsObserved, run2.RegistryActiveBefore)
            Assert.Equal(0, run2.UnaccountedObserved)
            Assert.Equal(0, run2.UnaccountedRegistry)
        End Using
    End Sub

End Class

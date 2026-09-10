' File: I06_RenameCandidateTests.vb
' Project: CodeMem.Tests
' Description: I6 - renaming one method retires the old row, mints a new one, writes exactly one candidate, and a third run writes none (SC-005).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 no rename_candidates row after the rename (no (B)) - reported to the Architect.
' GREEN: 2026-09-09 after (B) in Reconciler, ProximityRanker and RenameCandidatesRepository (T100-T102).
' FIRE:  2026-09-09 compared body_hash with Not String.Equals -> red (no candidate); reverted -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Article VI (B): proposal only, on the original id of the retired row.
''' </summary>
<Collection("Fixture")>
Public Class I06_RenameCandidateTests

    ''' <summary>
    ''' Describe to Explain: one retirement, one new id, one candidate with rank 1, same path, distance 0; nothing else changes; run 3 adds no candidate.
    ''' </summary>
    <Fact>
    Public Sub RenameWritesExactlyOneCandidate()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                Dim before As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)
                Dim describe1 As SymbolRow = before.Find(Function(s As SymbolRow) s.DocCommentId = "M:Sample.Widgets.BaseWidget.Describe")
                Assert.NotNull(describe1)

                copy.Replace("Sample.Lib/Widgets.vb", "Sub Describe(", "Sub Explain(")
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim run2 As RunRow = MapQueries.ReadRuns(map.Path)(1)
                Dim after As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)

                Dim describe2 As SymbolRow = after.Find(Function(s As SymbolRow) s.Id = describe1.Id)
                Assert.False(describe2.IsActive, "retired row must be inactive")
                Assert.Equal("M:Sample.Widgets.BaseWidget.Describe", describe2.DocCommentId)
                Dim explain As SymbolRow = after.Find(Function(s As SymbolRow) s.DocCommentId = "M:Sample.Widgets.BaseWidget.Explain")
                Assert.NotNull(explain)
                Assert.True(explain.IsActive)
                Assert.Equal(run2.Id, explain.FirstSeenRunId)

                Dim candidates As List(Of CandidateRow) = MapQueries.ReadCandidates(map.Path, run2.Id)
                Assert.Single(candidates)
                Assert.Equal(describe1.Id, candidates(0).RetiredSymbolId)
                Assert.Equal(explain.Id, candidates(0).NewSymbolId)
                Assert.Equal(1, candidates(0).Rank)
                Assert.True(candidates(0).SamePath)
                Assert.Equal(0, candidates(0).OffsetDistance)
                Assert.Equal(1, run2.RenameCandidates)
                Assert.Equal(1, run2.SymbolsRetired)
                Assert.Equal(1, run2.SymbolsNew)
                Assert.Equal(1L, MapQueries.CountRows(map.Path, "rename_candidates", solutionId))

                For Each row As SymbolRow In before
                    If row.Id = describe1.Id Then Continue For
                    Dim same As SymbolRow = after.Find(Function(s As SymbolRow) s.Id = row.Id)
                    Assert.NotNull(same)
                    Assert.Equal(row.DocCommentId, same.DocCommentId)
                    Assert.True(same.IsActive, "unrelated row changed active state: " & row.DocCommentId)
                Next
                Assert.Equal(before.Count + 1, after.Count)

                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim run3 As RunRow = MapQueries.ReadRuns(map.Path)(2)
                Assert.Empty(MapQueries.ReadCandidates(map.Path, run3.Id))
                Assert.Equal(0, run3.RenameCandidates)
                Assert.Equal(1L, MapQueries.CountRows(map.Path, "rename_candidates", solutionId))
            End Using
        End Using
    End Sub

End Class

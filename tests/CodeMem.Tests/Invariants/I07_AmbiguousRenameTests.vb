' File: I07_AmbiguousRenameTests.vb
' Project: CodeMem.Tests
' Description: I7 - two identical twins renamed in one run: each new row gets two candidates with proximity evidence and ranks; nothing is applied (Article VI (C)).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 zero candidates for the twins (no (B)/(C)) - reported to the Architect.
' GREEN: 2026-09-09 after (B)/(C): four candidates, ranks by offset distance.
' FIRE:  2026-09-09 reversed the distance order in ProximityRanker -> red (rank 1 was the farther row); reverted -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Rank 1 is the nearer retired row; proximity is evidence, never a threshold.
''' </summary>
<Collection("Fixture")>
Public Class I07_AmbiguousRenameTests

    ''' <summary>
    ''' First to Uno and Second to Dos in one edit: both old rows retired with ids intact, two new rows, two candidates each, four in total, ranks by offset distance.
    ''' </summary>
    <Fact>
    Public Sub TwinRenameRanksCandidatesByProximity()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                Dim before As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)
                Dim first1 As SymbolRow = before.Find(Function(s As SymbolRow) s.DocCommentId = "M:Sample.Widgets.Twins.First")
                Dim second1 As SymbolRow = before.Find(Function(s As SymbolRow) s.DocCommentId = "M:Sample.Widgets.Twins.Second")
                Assert.Equal(first1.BodyHash, second1.BodyHash)

                copy.Replace("Sample.Lib/Twins.vb", "Sub First()", "Sub Uno()")
                copy.Replace("Sample.Lib/Twins.vb", "Sub Second()", "Sub Dos()")
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim run2 As RunRow = MapQueries.ReadRuns(map.Path)(1)
                Dim after As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)

                Assert.False(after.Find(Function(s As SymbolRow) s.Id = first1.Id).IsActive)
                Assert.False(after.Find(Function(s As SymbolRow) s.Id = second1.Id).IsActive)
                Dim uno As SymbolRow = after.Find(Function(s As SymbolRow) s.DocCommentId = "M:Sample.Widgets.Twins.Uno")
                Dim dos As SymbolRow = after.Find(Function(s As SymbolRow) s.DocCommentId = "M:Sample.Widgets.Twins.Dos")
                Assert.NotNull(uno)
                Assert.NotNull(dos)

                Dim candidates As List(Of CandidateRow) = MapQueries.ReadCandidates(map.Path, run2.Id)
                Assert.Equal(4, candidates.Count)
                Assert.Equal(4, run2.RenameCandidates)
                For Each newRow As SymbolRow In New SymbolRow() {uno, dos}
                    Dim own As List(Of CandidateRow) = candidates.FindAll(Function(c As CandidateRow) c.NewSymbolId = newRow.Id)
                    Assert.Equal(2, own.Count)
                    Assert.Equal(1, own(0).Rank)
                    Assert.Equal(2, own(1).Rank)
                    Assert.True(own(0).SamePath AndAlso own(1).SamePath)
                    Assert.True(own(0).OffsetDistance.Value <= own(1).OffsetDistance.Value, "rank 1 must be the nearer retired row")
                Next
                Dim unoRank1 As CandidateRow = candidates.Find(Function(c As CandidateRow) c.NewSymbolId = uno.Id AndAlso c.Rank = 1)
                Assert.Equal(first1.Id, unoRank1.RetiredSymbolId)

                For Each row As SymbolRow In before
                    If row.Id = first1.Id OrElse row.Id = second1.Id Then Continue For
                    Dim same As SymbolRow = after.Find(Function(s As SymbolRow) s.Id = row.Id)
                    Assert.True(same IsNot Nothing AndAlso same.IsActive AndAlso same.DocCommentId = row.DocCommentId, "unrelated row changed: " & row.DocCommentId)
                Next
                Assert.Equal(before.Count + 2, after.Count)
            End Using
        End Using
    End Sub

End Class

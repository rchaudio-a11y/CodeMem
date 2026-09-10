' File: F02_KindRefreshTests.vb
' Project: CodeMem.Tests
' Description: F2 - an (A) identity match refreshes kind: one fixture type goes Class -> Structure -> Module across three runs and keeps its id (FR-101..FR-103, SC-101).
' Author: RCH Automation LLC
' Created: 2026-09-10
'
' RED:   2026-09-10 run 2 red: T:Sample.Fields kept kind "class" after the Class -> Structure edit (expected "structure"); the id was kept and the
'        counts were all-matched already, so the stale kind was the only defect (review F2).
' GREEN: 2026-09-10 after RefreshMatched/Reactivate gained kind = @kind and the call sites pass symbol.Kind (T029): class -> structure -> module on one id.
' FIRE:  2026-09-10 removed "kind = @kind" from the RefreshMatched UPDATE (old kind kept) -> red (expected "structure", actual "class"); reverted -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Sample.Lib/Fields.vb declares one class, two fields and one function (research R27); its kind text changes twice and nothing else does.
''' </summary>
<Collection("Fixture")>
Public Class F02_KindRefreshTests

    ''' <summary>
    ''' Run 1 records T:Sample.Fields (class) and F:Sample.Fields.a; run 2 (Structure) and run 3 (Module) keep both ids, refresh the kind,
    ''' and count everything as matched: no new row, no retirement, no candidate.
    ''' </summary>
    <Fact>
    Public Sub KindChangeKeepsTheIdAndRefreshesTheKind()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                Dim typeRow As SymbolRow = FindActive(map.Path, solutionId, "T:Sample.Fields")
                Dim fieldRow As SymbolRow = FindActive(map.Path, solutionId, "F:Sample.Fields.a")
                Assert.Equal("class", typeRow.Kind)
                Assert.Equal("field", fieldRow.Kind)
                Dim observed As Integer = MapQueries.ReadRuns(map.Path)(0).SymbolsObserved

                copy.Replace("Sample.Lib/Fields.vb", "Public Class Fields", "Public Structure Fields")
                copy.Replace("Sample.Lib/Fields.vb", "End Class", "End Structure")
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                AssertRun(map.Path, 1, observed)
                AssertSameIdWithKind(map.Path, solutionId, "T:Sample.Fields", typeRow.Id, "structure")
                AssertSameIdWithKind(map.Path, solutionId, "F:Sample.Fields.a", fieldRow.Id, "field")

                copy.Replace("Sample.Lib/Fields.vb", "Public Structure Fields", "Public Module Fields")
                copy.Replace("Sample.Lib/Fields.vb", "End Structure", "End Module")
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                AssertRun(map.Path, 2, observed)
                AssertSameIdWithKind(map.Path, solutionId, "T:Sample.Fields", typeRow.Id, "module")
                AssertSameIdWithKind(map.Path, solutionId, "F:Sample.Fields.a", fieldRow.Id, "field")
            End Using
        End Using
    End Sub

    Private Shared Function FindActive(db As String, solutionId As Long, docId As String) As SymbolRow
        Dim row As SymbolRow = MapQueries.ReadSymbols(db, solutionId).Find(Function(s As SymbolRow) s.IsActive AndAlso s.DocCommentId = docId)
        Assert.NotNull(row)
        Return row
    End Function

    Private Shared Sub AssertSameIdWithKind(db As String, solutionId As Long, docId As String, expectedId As Long, expectedKind As String)
        Dim rows As List(Of SymbolRow) = MapQueries.ReadSymbols(db, solutionId).FindAll(Function(s As SymbolRow) s.DocCommentId = docId)
        Assert.Single(rows)
        Assert.Equal(expectedId, rows(0).Id)
        Assert.True(rows(0).IsActive, docId & " retired")
        Assert.Equal(expectedKind, rows(0).Kind)
    End Sub

    Private Shared Sub AssertRun(db As String, index As Integer, observed As Integer)
        Dim run As RunRow = MapQueries.ReadRuns(db)(index)
        Assert.Equal("completed", run.Outcome)
        Assert.Equal(observed, run.SymbolsObserved)
        Assert.Equal(run.SymbolsObserved, run.SymbolsMatched)
        Assert.Equal(0, run.SymbolsNew)
        Assert.Equal(0, run.SymbolsRetired)
        Assert.Equal(0, run.SymbolsReactivated)
        Assert.Equal(0, run.RenameCandidates)
        Assert.Empty(MapQueries.ReadCandidates(db, run.Id))
    End Sub

End Class

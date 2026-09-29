' File: RenameScenario.vb
' Project: CodeMem.Tests
' Description: Class fixture for B12 (feature 006, T006; research R79): one map holding Sample run 1, Other run 1, Sample run 2 with three rename candidates (Describe->Explain; Name->Label once per linked-file twin) and Sample run 3, a real failed run that records one candidate and publishes none.
' Author: RCH Automation LLC
' Created: 2026-09-29
'
' Both copies are prepared by TwinScenario.Prepare (its second caller; no third, so no abstraction): Shared/Twin.vb compiles as
' Sample.App.Shared.Twin in Sample.App (TWIN_APP) and Sample.Lib.Shared.Twin in Sample.Lib, so a renamed member of Twin retires one id per
' project, each within its own project's container - one candidate per twin. Run 3 renames Explain->Narrate under corrupted counts
' (RunSeams.CorruptStagedCounts, as I08, B02 and X02 make failed runs): the run row records rename_candidates = 1 and nothing is published.
' The constructor asserts every figure the facts rely on from the map itself (analyze U2) and throws naming any that differs.

Imports System.IO
Imports CodeMem.Core
Imports CodeMem.Extraction

''' <summary>
''' Sample runs 1-3 and Other run 1 in one temp map, with an in-process host over it and the config path for the executable.
''' </summary>
Public Class RenameScenario
    Implements IDisposable

    ''' <summary>The fixture copy extracted as Sample (renamed between runs).</summary>
    Public ReadOnly Property Copy As FixtureCopy

    ''' <summary>A second prepared copy, extracted once as Other and never changed.</summary>
    Public ReadOnly Property Other As FixtureCopy

    ''' <summary>The temp map holding every run.</summary>
    Public ReadOnly Property Map As TempMap

    ''' <summary>The Sample solution id.</summary>
    Public ReadOnly Property SampleSolutionId As Long

    ''' <summary>The Other solution id.</summary>
    Public ReadOnly Property OtherSolutionId As Long

    ''' <summary>Sample's first run: the baseline, no candidates.</summary>
    Public ReadOnly Property SampleRun1 As Long

    ''' <summary>Sample's second run: three candidates.</summary>
    Public ReadOnly Property SampleRun2 As Long

    ''' <summary>Sample's third run: failed, one candidate recorded, none published.</summary>
    Public ReadOnly Property SampleRun3Failed As Long

    ''' <summary>Other's only run.</summary>
    Public ReadOnly Property OtherRun1 As Long

    ''' <summary>The configuration file the host reads, for BridgeProcess.Serve (analyze U1).</summary>
    Public ReadOnly Property ConfigPath As String

    ''' <summary>An in-process host, both gates off.</summary>
    Public ReadOnly Property Host As BridgeHost

    ''' <summary>
    ''' Prepares both copies, runs the four extractions and checks the figures the facts rely on.
    ''' </summary>
    Public Sub New()
        Copy = New FixtureCopy()
        TwinScenario.Prepare(Copy)
        Other = New FixtureCopy()
        TwinScenario.Prepare(Other)
        Map = New TempMap()

        SampleRun1 = Extract(Copy, "Sample", Nothing, ExitCode.Success)
        OtherRun1 = Extract(Other, "Other", Nothing, ExitCode.Success)
        Dim solutions As List(Of SolutionRow) = MapQueries.ReadSolutions(Map.Path)
        SampleSolutionId = solutions.Find(Function(s As SolutionRow) s.Key = "Sample").Id
        OtherSolutionId = solutions.Find(Function(s As SolutionRow) s.Key = "Other").Id

        Copy.Replace("Sample.Lib/Widgets.vb", "Sub Describe(", "Sub Explain(")
        Copy.Replace("Shared/Twin.vb", "Public Function Name()", "Public Function Label()")
        SampleRun2 = Extract(Copy, "Sample", Nothing, ExitCode.Success)
        Dim run2Candidates As Integer = MapQueries.ReadCandidates(Map.Path, SampleRun2).Count
        If run2Candidates <> 3 Then Throw New InvalidOperationException("run 2 wrote " & run2Candidates & " rename candidates, expected 3")

        Copy.Replace("Sample.Lib/Widgets.vb", "Sub Explain(", "Sub Narrate(")
        Dim corrupt As RunSeams = New RunSeams With {.CorruptStagedCounts = Sub(c As RunCounts) c.SymbolsMatched += 1}
        SampleRun3Failed = Extract(Copy, "Sample", corrupt, ExitCode.ResidualMismatch)
        Dim run3 As RunRow = MapQueries.ReadRuns(Map.Path).Find(Function(r As RunRow) r.Id = SampleRun3Failed)
        Dim run3Rows As Integer = MapQueries.ReadCandidates(Map.Path, SampleRun3Failed).Count
        If run3 Is Nothing OrElse run3.Outcome <> "failed" OrElse run3.RenameCandidates <> 1 OrElse run3Rows <> 0 Then
            Throw New InvalidOperationException("run 3 is not a failed run recording 1 candidate with none published: outcome " &
                If(run3 Is Nothing, "(missing)", run3.Outcome) & ", recorded " & If(run3 Is Nothing, "-", run3.RenameCandidates.ToString(Globalization.CultureInfo.InvariantCulture)) &
                ", rows " & run3Rows)
        End If

        Host = New BridgeHost(BridgeHost.WriteConfig(Map.Path, Nothing, False, False))
        ConfigPath = Host.ConfigPath
    End Sub

    ''' <summary>
    ''' The id of the row of a solution with a doc-comment id and an active state.
    ''' </summary>
    ''' <param name="solutionId">The solution.</param>
    ''' <param name="docCommentId">The doc-comment id.</param>
    ''' <param name="active">True for the active row, False for a retired one.</param>
    ''' <returns>The id; fails when there is no such row.</returns>
    Public Function SymbolIdByDocId(solutionId As Long, docCommentId As String, active As Boolean) As Long
        Dim row As SymbolRow = MapQueries.ReadSymbols(Map.Path, solutionId).Find(Function(s As SymbolRow) s.DocCommentId = docCommentId AndAlso s.IsActive = active)
        If row Is Nothing Then Throw New InvalidOperationException(docCommentId & " has no " & If(active, "active", "retired") & " row in solution " & solutionId)
        Return row.Id
    End Function

    ''' <summary>
    ''' A byte copy of the scenario's map at a new temp path, for a fact that must change a map (B12 (4)).
    ''' </summary>
    ''' <returns>The copy; the caller disposes it.</returns>
    Public Function CopyMap() As TempMap
        Dim copyOfMap As TempMap = New TempMap()
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools()
        File.Copy(Map.Path, copyOfMap.Path, True)
        Return copyOfMap
    End Function

    ''' <summary>
    ''' Deletes the map and both copies.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Map.Dispose()
        Other.Dispose()
        Copy.Dispose()
    End Sub

    Private Function Extract(source As FixtureCopy, key As String, seams As RunSeams, expected As ExitCode) As Long
        Dim code As ExitCode = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = source.SolutionPath, .DbPath = Map.Path, .SolutionKey = key}, seams)
        If code <> expected Then Throw New InvalidOperationException("extraction as " & key & " exited " & code.ToString() & ", expected " & expected.ToString())
        Dim runs As List(Of RunRow) = MapQueries.ReadRuns(Map.Path)
        Return runs(runs.Count - 1).Id
    End Function

End Class

' File: R02_BareNameTests.vb
' Project: CodeMem.Tests
' Description: Rule 2 - a field, property, event or method named bare is a calls occurrence targeting its row, one per identifier, at the identifier; bare external members write nothing (the stated limit); no candidate is written and the counts balance (FR-208..FR-215, SC-204..SC-206).
' Author: RCH Automation LLC
' Created: 2026-09-13
'
' RED:   2026-09-13 (1) red on the untouched tree: all seven expected occurrences absent - "M:Sample.Fields.Total -> F:Sample.Fields.a at
'        Sample.Lib/Fields.vb (Return a): expected one edge at 5,16, found 0" and likewise at MainForm.vb 5,20 (Timer1) / 5,43 (OnTick) /
'        13,9 / 17,9 and MainForm.Designer.vb 9,34 / 10,17 (components). (2) not red: no bare-name edge of any kind exists before rule 2, so the
'        absence of the vbCrLf edge holds vacuously; its guard is the second FIRE below (T017). (3) not red: candidates were already 0 and the
'        counts already balanced (I6/I7/I8); it pins that rule 2 changes none of that.
' GREEN: 2026-09-13 after CallsRule.CollectBareName (T015): 3 of 3 on first run; I3 still 5 handles edges; I12 green over every edge including
'        the seven new spans; the Stage A v1 fixture map re-extracts with 144 edges where the Stage A extractor wrote 137 (S02, amended).
' FIRE:  2026-09-13 (T017 i) CollectBareName admitted fields only -> (1) red on the four property and method facts (Timer1 x3, OnTick: "expected
'        one edge at 5,20, found 0" ...), the three field facts still found; (2), (3) green; reverted -> green.
' FIRE:  2026-09-13 (T017 ii) the rows-only check replaced by "any bound target" -> (2) red (Assert.DoesNotContain: an edge to
'        F:Microsoft.VisualBasic.Constants.vbCrLf appeared); (1), (3) green; reverted -> green.

Imports System.IO
Imports System.Text.RegularExpressions
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' The committed fixture already holds every shape rule 2 needs: a bare field read (Fields.Total), a WithEvents member used bare (Timer1), a
''' bare AddressOf (OnTick) and a field read twice in one method (Dispose). The external case runs on a copy.
''' </summary>
<Collection("Fixture")>
Public Class R02_BareNameTests

    Private ReadOnly _fixture As FixtureSolution

    Private Const LoadHandler As String = "M:Sample.MainForm.MainForm_Load(System.Object,System.EventArgs)"
    Private Const ShownHandler As String = "M:Sample.MainForm.MainForm_Shown(System.Object,System.EventArgs)"
    Private Const TickHandler As String = "M:Sample.MainForm.OnTick(System.Object,System.EventArgs)"
    Private Const DisposeMethod As String = "M:Sample.MainForm.Dispose(System.Boolean)"

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' (1) The seven expected occurrences exist exactly once each, with a row target, at the line and column of the identifier, with span text
    ''' equal to the name; the handles edges are still Stage A's five; no target is an accessor.
    ''' </summary>
    <Fact>
    Public Sub BareNamesOnTheCommittedFixtureAreOccurrences()
        Using map As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim calls As List(Of EdgeRow) = MapQueries.ReadEdges(map.Path, solutionId, "calls")
            Dim failures As List(Of String) = New List(Of String)()

            Expect(failures, calls, "M:Sample.Fields.Total", "F:Sample.Fields.a", "Sample.Lib/Fields.vb", "Return a", "a")
            Expect(failures, calls, LoadHandler, "P:Sample.MainForm.Timer1", "Sample.App/MainForm.vb", "AddHandler Timer1.Tick", "Timer1")
            Expect(failures, calls, LoadHandler, TickHandler, "Sample.App/MainForm.vb", "AddHandler Timer1.Tick", "OnTick")
            Expect(failures, calls, ShownHandler, "P:Sample.MainForm.Timer1", "Sample.App/MainForm.vb", "Timer1.Start()", "Timer1")
            Expect(failures, calls, TickHandler, "P:Sample.MainForm.Timer1", "Sample.App/MainForm.vb", "Timer1.Stop()", "Timer1")
            Expect(failures, calls, DisposeMethod, "F:Sample.MainForm.components", "Sample.App/MainForm.Designer.vb", "AndAlso components IsNot Nothing", "components")
            Expect(failures, calls, DisposeMethod, "F:Sample.MainForm.components", "Sample.App/MainForm.Designer.vb", "components.Dispose()", "components")
            Assert.True(failures.Count = 0, String.Join(Environment.NewLine, failures))

            Assert.Equal(5, MapQueries.ReadEdges(map.Path, solutionId, "handles").Count)
            Assert.DoesNotContain(calls, Function(e As EdgeRow) e.TargetDocCommentId.Contains(".get_") OrElse e.TargetDocCommentId.Contains(".set_"))
        End Using
    End Sub

    ''' <summary>
    ''' (2) A bare external member (vbCrLf, a field of Microsoft.VisualBasic.Constants) writes no edge - the stated limit - while the same method's
    ''' member-access call into the framework still writes its external-target edge.
    ''' </summary>
    <Fact>
    Public Sub BareExternalMembersWriteNothing()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                copy.Replace("Sample.Lib/Consumer.vb", "sb.Append(w.ToString())", "sb.Append(w.ToString())" & vbCrLf & "        sb.Append(vbCrLf)")
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}, Nothing))
                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                Dim edges As List(Of EdgeRow) = MapQueries.ReadEdges(map.Path, solutionId, Nothing)
                Assert.DoesNotContain(edges, Function(e As EdgeRow) e.TargetDocCommentId = "F:Microsoft.VisualBasic.Constants.vbCrLf")
                Assert.Contains(edges, Function(e As EdgeRow) e.Verb = "calls" AndAlso e.SourceDocCommentId = "M:Sample.Consumer.Build" AndAlso e.TargetDocCommentId.StartsWith("M:System.Text.StringBuilder.Append(", StringComparison.Ordinal) AndAlso Not e.TargetSymbolId.HasValue)
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (3) Two runs of the fixture into one map: run 2 matches everything, retires nothing, writes no candidate, and both residuals are 0; the
    ''' rename_candidates table is unchanged.
    ''' </summary>
    <Fact>
    Public Sub TwoRunsWriteNoCandidateAndBalance()
        Using map As TempMap = New TempMap()
            Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim candidatesBefore As Long = MapQueries.CountRows(map.Path, "rename_candidates", solutionId)
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
            Dim run2 As RunRow = MapQueries.ReadRuns(map.Path)(1)
            Assert.Equal(run2.SymbolsObserved, run2.SymbolsMatched)
            Assert.Equal(0, run2.SymbolsRetired)
            Assert.Equal(0, run2.SymbolsNew)
            Assert.Equal(0, run2.RenameCandidates)
            Assert.Equal(0, run2.UnaccountedObserved)
            Assert.Equal(0, run2.UnaccountedRegistry)
            Assert.Equal(candidatesBefore, MapQueries.CountRows(map.Path, "rename_candidates", solutionId))
            Assert.Equal("0.2.0", run2.ExtractorVersion)
        End Using
    End Sub

    ''' <summary>
    ''' Locates the identifier independently of the extractor (first line containing the anchor, whole-word column of the token) and demands
    ''' exactly one calls edge there with the given source and target, a row target, the token's length and the token as span text.
    ''' </summary>
    Private Sub Expect(failures As List(Of String), calls As List(Of EdgeRow), source As String, target As String, relativePath As String, lineAnchor As String, token As String)
        Dim text As String = File.ReadAllText(Path.Combine(_fixture.FixtureDirectory, relativePath.Replace("/"c, Path.DirectorySeparatorChar)))
        Dim lines As String() = text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.None)
        Dim lineNumber As Integer = 0
        Dim column As Integer = 0
        For i As Integer = 0 To lines.Length - 1
            If lines(i).Contains(lineAnchor) Then
                Dim match As Match = Regex.Match(lines(i), "\b" & Regex.Escape(token) & "\b")
                If Not match.Success Then Exit For
                lineNumber = i + 1
                column = match.Index + 1
                Exit For
            End If
        Next
        Dim what As String = source & " -> " & target & " at " & relativePath & " (" & lineAnchor & ")"
        If lineNumber = 0 Then
            failures.Add(what & ": anchor or token not found in the fixture")
            Return
        End If
        Dim found As List(Of EdgeRow) = calls.FindAll(Function(e As EdgeRow) e.SourceDocCommentId = source AndAlso e.TargetDocCommentId = target AndAlso e.Path = relativePath AndAlso e.StartLine = lineNumber AndAlso e.StartColumn = column)
        If found.Count <> 1 Then
            failures.Add(what & ": expected one edge at " & lineNumber & "," & column & ", found " & found.Count)
            Return
        End If
        Dim edge As EdgeRow = found(0)
        If Not edge.TargetSymbolId.HasValue Then failures.Add(what & ": target_symbol_id is null")
        If edge.Length <> token.Length Then failures.Add(what & ": length " & edge.Length & " <> " & token.Length)
        If edge.StartOffset < 0 OrElse edge.StartOffset + edge.Length > text.Length OrElse text.Substring(edge.StartOffset, edge.Length) <> token Then failures.Add(what & ": span text is not the identifier")
    End Sub

End Class

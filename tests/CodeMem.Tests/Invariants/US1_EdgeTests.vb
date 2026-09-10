' File: US1_EdgeTests.vb
' Project: CodeMem.Tests
' Description: US1 edge facts - each of the eight verbs appears where the fixture demands it, and nowhere it must not (FR-017).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 no edges exist yet - reported to the Architect.
' GREEN: 2026-09-09 after the symbol walker (T055-T062) and the eight edge rules (T065-T080); whole US1 filter green in 8 s.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Edge rows after one run of the fixture.
''' </summary>
<Collection("Fixture")>
Public Class US1_EdgeTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' The eight verbs by their rules.
    ''' </summary>
    <Fact>
    Public Sub EdgesFollowTheVerbRules()
        Using map As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim symbols As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)
            Dim edges As List(Of EdgeRow) = MapQueries.ReadEdges(map.Path, solutionId, Nothing)
            Assert.True(edges.Count > 0, "no edges exist")

            ' part_of from every non-project row.
            For Each s As SymbolRow In symbols
                If s.Kind = "project" Then Continue For
                Assert.True(edges.Exists(Function(e As EdgeRow) e.Verb = "part_of" AndAlso e.SourceSymbolId = s.Id), "no part_of edge from " & s.DocCommentId)
            Next
            Assert.False(edges.Exists(Function(e As EdgeRow) e.Verb = "part_of" AndAlso e.SourceDocCommentId.StartsWith("Project:", StringComparison.Ordinal)), "project row has a part_of edge")

            Assert.Contains(edges, Function(e As EdgeRow) e.Verb = "extends" AndAlso e.SourceDocCommentId = "T:Sample.Widgets.LeafWidget" AndAlso e.TargetDocCommentId = "T:Sample.Widgets.MidWidget" AndAlso e.TargetSymbolId.HasValue)
            Assert.Contains(edges, Function(e As EdgeRow) e.Verb = "implements" AndAlso e.SourceDocCommentId = "T:Sample.Widgets.AlphaService" AndAlso e.TargetDocCommentId = "T:Sample.Widgets.ISampleService")
            Assert.Contains(edges, Function(e As EdgeRow) e.Verb = "implements" AndAlso e.SourceDocCommentId = "T:Sample.Widgets.BetaService" AndAlso e.TargetDocCommentId = "T:Sample.Widgets.ISampleService")
            Assert.Contains(edges, Function(e As EdgeRow) e.Verb = "implements" AndAlso e.SourceDocCommentId = "M:Sample.Widgets.AlphaService.Serve(System.String)" AndAlso e.TargetDocCommentId = "M:Sample.Widgets.ISampleService.Serve(System.String)")

            ' uses: a parameter type and a local As clause.
            Assert.Contains(edges, Function(e As EdgeRow) e.Verb = "uses" AndAlso e.SourceDocCommentId = "M:Sample.OverloadSet.Run(System.Int32)" AndAlso e.TargetDocCommentId = "T:System.Int32")
            Assert.Contains(edges, Function(e As EdgeRow) e.Verb = "uses" AndAlso e.SourceDocCommentId = "M:Sample.Consumer.Build" AndAlso e.TargetDocCommentId = "T:Sample.Widgets.LeafWidget" AndAlso e.TargetSymbolId.HasValue)

            ' imports: top-level type to namespace, external target.
            Assert.Contains(edges, Function(e As EdgeRow) e.Verb = "imports" AndAlso e.SourceDocCommentId = "T:Sample.Consumer" AndAlso e.TargetDocCommentId = "N:System.Text" AndAlso Not e.TargetSymbolId.HasValue)

            ' depends_on: project to project with a resolved target.
            Assert.Contains(edges, Function(e As EdgeRow) e.Verb = "depends_on" AndAlso e.SourceDocCommentId = "Project:Sample.App" AndAlso e.TargetDocCommentId = "Project:Sample.Lib" AndAlso e.TargetSymbolId.HasValue)

            ' calls: external target, and the implicit constructor redirected to the type.
            Assert.Contains(edges, Function(e As EdgeRow) e.Verb = "calls" AndAlso e.TargetDocCommentId.StartsWith("M:System.Windows.Forms.MessageBox.Show(", StringComparison.Ordinal) AndAlso Not e.TargetSymbolId.HasValue)
            Assert.Contains(edges, Function(e As EdgeRow) e.Verb = "calls" AndAlso e.SourceDocCommentId = "M:Sample.Consumer.Build" AndAlso e.TargetDocCommentId = "T:Sample.Widgets.LeafWidget" AndAlso e.TargetSymbolId.HasValue)
            Assert.False(edges.Exists(Function(e As EdgeRow) e.TargetDocCommentId.EndsWith("#ctor", StringComparison.Ordinal) AndAlso e.TargetDocCommentId.Contains("Sample.Widgets.")), "implicit constructor target not redirected")

            ' no accessor or lambda sources; only the eight verbs.
            Dim verbs As String() = New String() {"part_of", "calls", "uses", "implements", "extends", "imports", "depends_on", "handles"}
            For Each e As EdgeRow In edges
                Assert.Contains(e.Verb, verbs)
                Assert.False(e.SourceDocCommentId.Contains(".get_") OrElse e.SourceDocCommentId.Contains(".set_") OrElse e.SourceDocCommentId.Contains(".add_") OrElse e.SourceDocCommentId.Contains(".remove_"), "edge sourced from an accessor: " & e.SourceDocCommentId)
                Assert.True(symbols.Exists(Function(s As SymbolRow) s.Id = e.SourceSymbolId), "edge source is not a row")
                If e.Verb <> "handles" Then Assert.False(e.ViaSymbolId.HasValue, "via set on a non-handles edge")
            Next
        End Using
    End Sub

End Class

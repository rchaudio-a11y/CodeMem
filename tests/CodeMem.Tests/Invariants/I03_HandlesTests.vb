' File: I03_HandlesTests.vb
' Project: CodeMem.Tests
' Description: I3 - every Handles item and AddHandler statement in the fixture yields exactly one handles edge; no handler lacks one (SC-003).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 zero handles edges written (no edge rule runs yet) - reported to the Architect.
' GREEN: 2026-09-09 after the symbol walker (T055-T062) and the eight edge rules (T065-T080); whole US1 filter green in 8 s.

Imports System.IO
Imports CodeMem.Extraction
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Xunit

''' <summary>
''' Counts Handles items and AddHandler statements by an independent syntax walk and compares with the handles edges written.
''' </summary>
<Collection("Fixture")>
Public Class I03_HandlesTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' handles edge count equals Handles items plus AddHandler statements; via ids point at the WithEvents members; AddHandler via is null.
    ''' </summary>
    <Fact>
    Public Sub HandlesEdgesMatchTheSource()
        Dim handlesItems As Integer = 0
        Dim addHandlers As Integer = 0
        Dim handlerNames As HashSet(Of String) = New HashSet(Of String)(StringComparer.Ordinal)
        For Each sourceFile As String In Directory.GetFiles(_fixture.FixtureDirectory, "*.vb", SearchOption.AllDirectories)
            If sourceFile.Contains(Path.DirectorySeparatorChar & "obj" & Path.DirectorySeparatorChar) OrElse sourceFile.Contains(Path.DirectorySeparatorChar & "bin" & Path.DirectorySeparatorChar) Then Continue For
            Dim root As SyntaxNode = VisualBasicSyntaxTree.ParseText(File.ReadAllText(sourceFile)).GetRoot()
            For Each node As SyntaxNode In root.DescendantNodes()
                If TypeOf node Is HandlesClauseItemSyntax Then
                    handlesItems += 1
                    handlerNames.Add(DirectCast(node.Parent.Parent, MethodStatementSyntax).Identifier.ValueText)
                ElseIf node.IsKind(SyntaxKind.AddHandlerStatement) Then
                    addHandlers += 1
                    Dim operand As ExpressionSyntax = DirectCast(DirectCast(node, AddRemoveHandlerStatementSyntax).DelegateExpression, UnaryExpressionSyntax).Operand
                    handlerNames.Add(DirectCast(operand, IdentifierNameSyntax).Identifier.ValueText)
                End If
            Next
        Next
        Assert.Equal(4, handlesItems)
        Assert.Equal(1, addHandlers)
        Assert.Equal(4, handlerNames.Count)

        Using map As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim edges As List(Of EdgeRow) = MapQueries.ReadEdges(map.Path, solutionId, "handles")
            Assert.Equal(handlesItems + addHandlers, edges.Count)

            For Each handler As String In handlerNames
                Dim docId As String = "M:Sample.MainForm." & handler & "(System.Object,System.EventArgs)"
                Assert.True(edges.Exists(Function(e As EdgeRow) e.SourceDocCommentId = docId), "handler without an incoming handles edge: " & docId)
            Next

            Dim clicks As List(Of EdgeRow) = edges.FindAll(Function(e As EdgeRow) e.TargetDocCommentId.EndsWith(".Click", StringComparison.Ordinal))
            Assert.Equal(2, clicks.Count)
            Assert.Contains(clicks, Function(e As EdgeRow) e.ViaDocCommentId = "P:Sample.MainForm.Button1")
            Assert.Contains(clicks, Function(e As EdgeRow) e.ViaDocCommentId = "P:Sample.MainForm.Button2")
            For Each click As EdgeRow In clicks
                Assert.True(click.ViaSymbolId.HasValue, "Handles item via_symbol_id is null")
            Next

            Dim tick As EdgeRow = edges.Find(Function(e As EdgeRow) e.TargetDocCommentId.EndsWith(".Tick", StringComparison.Ordinal))
            Assert.NotNull(tick)
            Assert.False(tick.ViaSymbolId.HasValue, "AddHandler edge must leave via_symbol_id null")
            Assert.Equal("M:Sample.MainForm.OnTick(System.Object,System.EventArgs)", tick.SourceDocCommentId)

            For Each e As EdgeRow In edges
                Assert.StartsWith("E:", e.TargetDocCommentId)
            Next
        End Using
    End Sub

End Class

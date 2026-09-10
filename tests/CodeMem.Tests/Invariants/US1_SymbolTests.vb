' File: US1_SymbolTests.vb
' Project: CodeMem.Tests
' Description: US1 symbol facts - rows, kinds, the per-solution namespace merge, no implicit symbols, hashes, primary declarations.
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 no code_symbols rows written - reported to the Architect.
' GREEN: 2026-09-09 after the symbol walker (T055-T062) and the eight edge rules (T065-T080); whole US1 filter green in 8 s.
'
' Primary declaration (FR-010 vs FR-018): the row's path is its first part's path and its span is the identifier token
' inside that part, so the span text equals the name (I12). The offset therefore lies within the first part's span
' rather than equalling the part's own start offset.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Symbol rows after one run of the fixture.
''' </summary>
<Collection("Fixture")>
Public Class US1_SymbolTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' The expected rows exist, the namespace is merged, implicit symbols are absent, project ids are null exactly for namespace and project rows.
    ''' </summary>
    <Fact>
    Public Sub SymbolRowsMatchTheFixture()
        Using map As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim symbols As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)
            Dim parts As List(Of PartRow) = MapQueries.ReadParts(map.Path, solutionId)
            Assert.True(symbols.Count > 0, "no code_symbols rows written")

            For Each expected As String In New String() {"T:Sample.Widgets.ISampleService", "T:Sample.Widgets.LeafWidget", "M:Sample.Widgets.Twins.First", "F:Sample.Fields.a", "Project:Sample.App", "Project:Sample.Lib", "T:Sample.MainForm", "M:Sample.Consumer.#ctor"}
                Assert.True(symbols.Exists(Function(s As SymbolRow) s.DocCommentId = expected), "missing row " & expected)
            Next

            Dim widgets As List(Of SymbolRow) = symbols.FindAll(Function(s As SymbolRow) s.DocCommentId = "N:Sample.Widgets")
            Assert.Single(widgets)
            Dim widgetParts As List(Of PartRow) = parts.FindAll(Function(p As PartRow) p.SymbolId = widgets(0).Id)
            Assert.True(widgetParts.Exists(Function(p As PartRow) p.Path.StartsWith("Sample.Lib/", StringComparison.Ordinal)), "namespace lacks a Sample.Lib part")
            Assert.True(widgetParts.Exists(Function(p As PartRow) p.Path.StartsWith("Sample.App/", StringComparison.Ordinal)), "namespace lacks a Sample.App part")

            For Each typeWithoutCtor As String In New String() {"BaseWidget", "MidWidget", "LeafWidget", "AlphaService", "BetaService", "Twins", "AppWidget"}
                Dim implicitCtor As String = "M:Sample.Widgets." & typeWithoutCtor & ".#ctor"
                Assert.False(symbols.Exists(Function(s As SymbolRow) s.DocCommentId = implicitCtor), "implicit constructor written: " & implicitCtor)
            Next
            Dim explicitCtor As SymbolRow = symbols.Find(Function(s As SymbolRow) s.DocCommentId = "M:Sample.Consumer.#ctor")
            Assert.Equal("New", explicitCtor.Name)
            Assert.Equal("constructor", explicitCtor.Kind)

            Assert.False(symbols.Exists(Function(s As SymbolRow) s.Name = "_Button1"), "synthesized WithEvents backing field written")
            Assert.Single(symbols.FindAll(Function(s As SymbolRow) s.Name = "Button1"))

            For Each s As SymbolRow In symbols
                Dim shouldBeNull As Boolean = s.Kind = "namespace" OrElse s.Kind = "project"
                Assert.True(shouldBeNull = Not s.ProjectSymbolId.HasValue, "project_symbol_id null-ness wrong for " & s.DocCommentId)
                Assert.Matches("^[0-9a-f]{64}$", s.BodyHash)
                If s.Kind <> "project" Then
                    Dim own As List(Of PartRow) = parts.FindAll(Function(p As PartRow) p.SymbolId = s.Id)
                    Assert.True(own.Count > 0, "no parts for " & s.DocCommentId)
                    Dim firstPart As PartRow = own(0)
                    Assert.Equal(firstPart.Path, s.Path)
                    Assert.True(s.StartOffset >= firstPart.StartOffset AndAlso s.StartOffset < firstPart.StartOffset + firstPart.Length, "primary declaration of " & s.DocCommentId & " is not inside its first part")
                Else
                    Assert.Empty(parts.FindAll(Function(p As PartRow) p.SymbolId = s.Id))
                End If
            Next
        End Using
    End Sub

End Class

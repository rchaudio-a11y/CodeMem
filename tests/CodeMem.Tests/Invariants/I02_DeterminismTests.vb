' File: I02_DeterminismTests.vb
' Project: CodeMem.Tests
' Description: I2 - the same fixture extracted into two fresh maps yields identical logical fact sets (Article IV, SC-002).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 fact sets contain no symbol lines (nothing staged yet) - reported to the Architect.
' GREEN: 2026-09-09 after the symbol walker (T055-T062) and the eight edge rules (T065-T080); whole US1 filter green in 8 s.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Determinism over the canonical fact set: symbols by doc id, parts, edges, ten counts; surrogate ids, run ids, timestamps and map identity excluded.
''' </summary>
<Collection("Fixture")>
Public Class I02_DeterminismTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' Two fresh maps, one fixture, equal fact sets that actually contain symbols, parts and edges.
    ''' </summary>
    <Fact>
    Public Sub TwoFreshMapsYieldIdenticalFactSets()
        Using first As TempMap = New TempMap()
            Using second As TempMap = New TempMap()
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = first.Path}, Nothing))
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = second.Path}, Nothing))
                Dim firstSolutionId As Long = MapQueries.ReadSolutions(first.Path)(0).Id
                Dim secondSolutionId As Long = MapQueries.ReadSolutions(second.Path)(0).Id
                Dim factsA As List(Of String) = MapQueries.FactSet(first.Path, firstSolutionId)
                Dim factsB As List(Of String) = MapQueries.FactSet(second.Path, secondSolutionId)
                Assert.True(factsA.Exists(Function(line As String) line.StartsWith("S|", StringComparison.Ordinal)), "fact set has no symbol lines")
                Assert.True(factsA.Exists(Function(line As String) line.StartsWith("P|", StringComparison.Ordinal)), "fact set has no part lines")
                Assert.True(factsA.Exists(Function(line As String) line.StartsWith("E|", StringComparison.Ordinal)), "fact set has no edge lines")
                Assert.Equal(factsA, factsB)
            End Using
        End Using
    End Sub

End Class

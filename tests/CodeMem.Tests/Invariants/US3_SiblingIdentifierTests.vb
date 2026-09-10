' File: US3_SiblingIdentifierTests.vb
' Project: CodeMem.Tests
' Description: FR-013 - renaming a sibling declarator (Dim a, b) leaves the other field's body_hash and id untouched.
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   n/a - expected green from the sibling exclusion in TokenTextHasher (T056); the guard is trusted through its FIRE.
' GREEN: 2026-09-09 first run.
' FIRE:  2026-09-09 excluded only the first declarator identifier from the field hash -> red (a's hash changed); reverted -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Every declarator identifier of a shared declaration is excluded from every sibling's hash.
''' </summary>
<Collection("Fixture")>
Public Class US3_SiblingIdentifierTests

    ''' <summary>
    ''' a, b As Integer to a, c As Integer: a keeps hash and id, b is retired, c is new.
    ''' </summary>
    <Fact>
    Public Sub SiblingRenameLeavesTheOtherFieldAlone()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                Dim a1 As SymbolRow = MapQueries.ReadSymbols(map.Path, solutionId).Find(Function(s As SymbolRow) s.DocCommentId = "F:Sample.Fields.a")

                copy.Replace("Sample.Lib/Fields.vb", "a, b As Integer", "a, c As Integer")
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim symbols As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)

                Dim a2 As SymbolRow = symbols.Find(Function(s As SymbolRow) s.DocCommentId = "F:Sample.Fields.a" AndAlso s.IsActive)
                Assert.Equal(a1.Id, a2.Id)
                Assert.Equal(a1.BodyHash, a2.BodyHash)
                Assert.False(symbols.Find(Function(s As SymbolRow) s.DocCommentId = "F:Sample.Fields.b").IsActive)
                Assert.True(symbols.Find(Function(s As SymbolRow) s.DocCommentId = "F:Sample.Fields.c").IsActive)
            End Using
        End Using
    End Sub

End Class

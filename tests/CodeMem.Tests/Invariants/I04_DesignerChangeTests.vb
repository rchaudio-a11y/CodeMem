' File: I04_DesignerChangeTests.vb
' Project: CodeMem.Tests
' Description: I4 - a token change in the designer partial changes the Form's body_hash but not the part_hash of its other part; the id survives (FR-014).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   n/a - expected green from the part/body hashing of Slice B; the guard is trusted through its FIRE.
' GREEN: 2026-09-09 first run after (A) matching kept the id.
' FIRE:  2026-09-09 the task's injection (part node = compilation unit) does not defeat I4 (a whole-file part still changes the body hash), so the body hash was computed from the last part only -> red; reverted -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Part hashes are per declaring reference; the body hash spans them all.
''' </summary>
<Collection("Fixture")>
Public Class I04_DesignerChangeTests

    ''' <summary>
    ''' Edit New Size(75, 23) in the designer: body_hash changes, the MainForm.vb part_hash does not, the row keeps its id.
    ''' </summary>
    <Fact>
    Public Sub DesignerEditChangesBodyHashOnly()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Dim options As ExtractionOptions = New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))
                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                Dim form1 As SymbolRow = MapQueries.ReadSymbols(map.Path, solutionId).Find(Function(s As SymbolRow) s.DocCommentId = "T:Sample.MainForm")
                Dim part1 As PartRow = MapQueries.ReadParts(map.Path, solutionId).Find(Function(p As PartRow) p.SymbolId = form1.Id AndAlso p.Path = "Sample.App/MainForm.vb")
                Assert.NotNull(part1)

                copy.Replace("Sample.App/MainForm.Designer.vb", "New Size(75, 23)", "New Size(80, 23)")
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(options, Nothing))

                Dim form2 As SymbolRow = MapQueries.ReadSymbols(map.Path, solutionId).Find(Function(s As SymbolRow) s.DocCommentId = "T:Sample.MainForm" AndAlso s.IsActive)
                Dim part2 As PartRow = MapQueries.ReadParts(map.Path, solutionId).Find(Function(p As PartRow) p.SymbolId = form2.Id AndAlso p.Path = "Sample.App/MainForm.vb")
                Assert.Equal(form1.Id, form2.Id)
                Assert.NotEqual(form1.BodyHash, form2.BodyHash)
                Assert.Equal(part1.PartHash, part2.PartHash)
            End Using
        End Using
    End Sub

End Class

' File: I01_GreenGateTests.vb
' Project: CodeMem.Tests
' Description: I1 - a compile error is refused with exit 2, diagnostics on stderr, and the map file byte-identical (SC-001).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 not red: the exit-2 path landed in Slice A ahead of this test; the guard is trusted through its FIRE below.
' GREEN: 2026-09-09 first run.
' FIRE:  2026-09-09 changed the exit-2 test to 'errorCount > 1000000' (check skipped) -> red (exit 0, map changed); reverted -> green.

Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' The green gate, proven through the real executable against a mutated copy of the fixture.
''' </summary>
<Collection("Fixture")>
Public Class I01_GreenGateTests

    ''' <summary>
    ''' Break one line, run the executable: exit 2, error BC on stderr, a final errors=n line, empty stdout, identical file bytes.
    ''' </summary>
    <Fact>
    Public Sub CompileErrorLeavesTheMapByteIdentical()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = map.Path}, Nothing))
                copy.Replace("Sample.Lib/Twins.vb", "Sub First()", "Sub First() As Integer")
                Dim before As String = MapSnapshot.FileBytesHash(map.Path)

                Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(copy.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path))

                Assert.Equal(2, run.ExitCode)
                Assert.Contains("error BC", run.StandardError)
                Assert.Matches("errors=\d+\s*$", run.StandardError)
                Assert.Equal("", run.StandardOutput)
                Assert.Equal(before, MapSnapshot.FileBytesHash(map.Path))
            End Using
        End Using
    End Sub

End Class

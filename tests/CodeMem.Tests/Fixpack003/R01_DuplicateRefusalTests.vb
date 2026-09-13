' File: R01_DuplicateRefusalTests.vb
' Project: CodeMem.Tests
' Description: Rule 1 leaves the in-scope duplicate refusal as it is: two in-scope declarations of one doc-comment id are refused through the executable with exit 1, one line naming both locations, nothing written (FR-205, SC-202, Article XIII).
' Author: RCH Automation LLC
' Created: 2026-09-13
'
' RED:   2026-09-13 not red: the guard is Stage A's (FR-019 step 1, Reconciler) and had no production-route test until now; recorded as that proof
'        and trusted through its FIRE (T014).
' GREEN: 2026-09-13 first run, on the untouched tree and again after rule 1 (T010): exit 1, one line naming both locations, 0 user tables.
' FIRE:  2026-09-13 (T014) Reconciler step 1 skipped the Throw (Continue For) -> red: the run reached publication and the schema's unique index
'        refused instead ("database error: SQLite Error 19: 'UNIQUE ..."), so the named line was absent (Assert.Contains "duplicate doc-comment
'        id T:Sample.Dup" not found); reverted -> green. The two doors are distinct: the code names the pair, the schema only says UNIQUE.

Imports System.IO
Imports Xunit

''' <summary>
''' Dup.vb declaring Public Class Dup in both projects of a fixture copy (root namespace Sample in both), run through the real executable.
''' </summary>
<Collection("Fixture")>
Public Class R01_DuplicateRefusalTests

    ''' <summary>
    ''' Exit 1, empty stdout, exactly one stderr line naming T:Sample.Dup at both files (line 1, column 14), no stack frame, and the fresh map
    ''' path holds no user table.
    ''' </summary>
    <Fact>
    Public Sub TwoInScopeDeclarationsOfOneIdAreRefusedByName()
        Using copy As FixtureCopy = New FixtureCopy()
            Using map As TempMap = New TempMap()
                Dim source As String = "Public Class Dup" & vbCrLf & "End Class" & vbCrLf
                File.WriteAllText(Path.Combine(copy.Directory, "Sample.Lib", "Dup.vb"), source)
                File.WriteAllText(Path.Combine(copy.Directory, "Sample.App", "Dup.vb"), source)

                Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(copy.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path))
                Assert.Equal(1, run.ExitCode)
                Assert.Equal("", run.StandardOutput)
                Dim lines As String() = run.StandardError.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                Assert.True(lines.Length = 1, "expected exactly one stderr line, got " & lines.Length & ":" & Environment.NewLine & run.StandardError)
                Assert.Contains("duplicate doc-comment id T:Sample.Dup", lines(0))
                Assert.Contains("Sample.App/Dup.vb:1,14", lines(0))
                Assert.Contains("Sample.Lib/Dup.vb:1,14", lines(0))
                Assert.DoesNotContain("   at ", lines(0))
                Assert.Equal(0L, MapQueries.CountUserTables(map.Path))
            End Using
        End Using
    End Sub

End Class

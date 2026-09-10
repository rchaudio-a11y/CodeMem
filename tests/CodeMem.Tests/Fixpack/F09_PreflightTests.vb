' File: F09_PreflightTests.vb
' Project: CodeMem.Tests
' Description: F9 - every preflight failure of the executable maps to exit 1 with exactly one stderr line and nothing on stdout; no stack trace (FR-115, SC-103).
' Author: RCH Automation LLC
' Created: 2026-09-10
'
' RED:   2026-09-10 not red: the error boundary from the first statement landed with Slice A (T024) before this file first ran (Stage A: (a)
'        was an unhandled ArgumentException with the CLR's exit code, (b) exited 3); green on first run, 4 of 4.
' GREEN: 2026-09-10 first run: whitespace --db, ma<p>.sqlite, a directory, whitespace --solution: each exit 1, one stderr line, empty stdout.
' FIRE:  2026-09-10 (T034) Path.GetFullPath moved back outside the Try -> (a) and (d) red (exit -532462766, the CLR's unhandled-exception
'        code, instead of 1); reverted -> green.

Imports System.IO
Imports Xunit

''' <summary>
''' Whitespace-only --db, illegal characters in --db, a directory as --db, whitespace-only --solution.
''' </summary>
<Collection("Fixture")>
Public Class F09_PreflightTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' (a) --db " " (whitespace only): exit 1, one stderr line, empty stdout.
    ''' </summary>
    <Fact>
    Public Sub WhitespaceDbPathIsExitOne()
        AssertOneLineRefusal(ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db "" """))
    End Sub

    ''' <summary>
    ''' (b) --db with illegal characters (ma&lt;p&gt;.sqlite): exit 1 (not 3), one stderr line.
    ''' </summary>
    <Fact>
    Public Sub IllegalCharactersInDbPathAreExitOneNotThree()
        Dim illegal As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "ma<p>.sqlite")
        AssertOneLineRefusal(ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db " & ExtractorProcess.Quote(illegal)))
    End Sub

    ''' <summary>
    ''' (c) --db naming an existing directory: exit 1, one line.
    ''' </summary>
    <Fact>
    Public Sub DirectoryAsDbPathIsExitOne()
        Dim dir As String = Path.Combine(Path.GetTempPath(), "codemem-tests")
        Directory.CreateDirectory(dir)
        AssertOneLineRefusal(ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db " & ExtractorProcess.Quote(dir)))
    End Sub

    ''' <summary>
    ''' (d) --solution " " (whitespace only): exit 1, one line.
    ''' </summary>
    <Fact>
    Public Sub WhitespaceSolutionPathIsExitOne()
        Using map As TempMap = New TempMap()
            AssertOneLineRefusal(ExtractorProcess.Run("--solution "" "" --db " & ExtractorProcess.Quote(map.Path)))
        End Using
    End Sub

    Private Shared Sub AssertOneLineRefusal(run As ExtractorProcess)
        Assert.Equal(1, run.ExitCode)
        Assert.Equal("", run.StandardOutput)
        Dim lines As String() = run.StandardError.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        Assert.True(lines.Length = 1, "expected exactly one stderr line, got " & lines.Length & ":" & Environment.NewLine & run.StandardError)
        Assert.DoesNotContain("   at ", lines(0))
    End Sub

End Class

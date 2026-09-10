' File: F08_NonceTests.vb
' Project: CodeMem.Tests
' Description: F8 - the test-only abort seam is armed only by CODEMEM_TEST_ABORT_AT=<phase>:<nonce> plus an equal CODEMEM_TEST_NONCE (FR-116, FR-117, SC-107).
' Author: RCH Automation LLC
' Created: 2026-09-10
'
' RED:   2026-09-10 (d) red: "DuringPublish:<guid>" is not a phase Stage A knows, so the run completed (exit 0, one run row) instead of
'        aborting; (a), (b) and (c) were green for the wrong reason (the same unknown-value inertness), so (d) carried the Red.
' GREEN: 2026-09-10 after ReadAbortPhase parses <phase>:<nonce> and requires an equal CODEMEM_TEST_NONCE (T014): 4 of 4 assertions, help fact green.
' FIRE:  2026-09-10 accepted the phase without checking CODEMEM_TEST_NONCE (T014) -> (a) and (c) red: exit -2146232797 (FailFast) instead of 0; reverted -> green 5 of 5.
' FIRE:  2026-09-10 removed the CODEMEM_TEST_NONCE line from CommandLine.Usage (T040) -> help fact red (sub-string not found); reverted -> green.

Imports Xunit

''' <summary>
''' Four executable runs against the committed fixture: variable alone, nonce alone, unequal nonces, equal nonces; plus the --help fact.
''' </summary>
<Collection("Fixture")>
Public Class F08_NonceTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' (a) the abort variable with a phase and nonce but no CODEMEM_TEST_NONCE: exit 0, one completed run.
    ''' </summary>
    <Fact>
    Public Sub AbortVariableAloneIsInert()
        AssertCompletes(New Dictionary(Of String, String) From {{"CODEMEM_TEST_ABORT_AT", "DuringPublish:abc"}})
    End Sub

    ''' <summary>
    ''' (b) the nonce alone: exit 0, one completed run.
    ''' </summary>
    <Fact>
    Public Sub NonceAloneIsInert()
        AssertCompletes(New Dictionary(Of String, String) From {{"CODEMEM_TEST_NONCE", "abc"}})
    End Sub

    ''' <summary>
    ''' (c) both set but unequal: exit 0, one completed run.
    ''' </summary>
    <Fact>
    Public Sub UnequalNoncesAreInert()
        AssertCompletes(New Dictionary(Of String, String) From {{"CODEMEM_TEST_ABORT_AT", "DuringPublish:abc"}, {"CODEMEM_TEST_NONCE", "xyz"}})
    End Sub

    ''' <summary>
    ''' (d) both set and equal (a fresh GUID): the process aborts at DuringPublish, non-zero exit, FailFast text on stderr, no run row.
    ''' </summary>
    <Fact>
    Public Sub EqualNoncesArmTheAbort()
        Using map As TempMap = New TempMap()
            Dim nonce As String = Guid.NewGuid().ToString("N")
            Dim environment As Dictionary(Of String, String) = New Dictionary(Of String, String) From {{"CODEMEM_TEST_ABORT_AT", "DuringPublish:" & nonce}, {"CODEMEM_TEST_NONCE", nonce}}
            Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path), environment)
            Assert.NotEqual(0, run.ExitCode)
            Assert.Contains("Process terminated. CODEMEM_TEST_ABORT_AT=DuringPublish", run.StandardError)
            Assert.Equal(0L, MapQueries.CountUserTables(map.Path))   ' no run row: the fresh map rolled back with the aborted run (research R23)
        End Using
    End Sub

    ''' <summary>
    ''' --help documents both variables and the both-required rule under the test-only heading (FR-117).
    ''' </summary>
    <Fact>
    Public Sub HelpDocumentsBothVariablesUnderTheTestOnlyHeading()
        Dim run As ExtractorProcess = ExtractorProcess.Run("--help")
        Assert.Equal(0, run.ExitCode)
        Dim lines As String() = run.StandardOutput.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.None)
        Dim heading As Integer = Array.FindIndex(lines, Function(l As String) l.Contains("test-only"))
        Assert.True(heading >= 0, "no test-only heading in --help")
        Dim below As String = String.Join(vbLf, lines, heading + 1, lines.Length - heading - 1)
        Assert.Contains("CODEMEM_TEST_ABORT_AT", below)
        Assert.Contains("CODEMEM_TEST_NONCE", below)
        Assert.Contains("both", below)
    End Sub

    Private Sub AssertCompletes(environment As Dictionary(Of String, String))
        Using map As TempMap = New TempMap()
            Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(_fixture.SolutionPath) & " --db " & ExtractorProcess.Quote(map.Path), environment)
            Assert.Equal(0, run.ExitCode)
            Dim runs As List(Of RunRow) = MapQueries.ReadRuns(map.Path)
            Assert.Single(runs)
            Assert.Equal("completed", runs(0).Outcome)
        End Using
    End Sub

End Class

' File: BridgeStandaloneGateTests_RetiredFacts.vb
' Project: _Archive/006-map-teaches-its-use/tests (not compiled)
' Description: BridgeStandaloneGateTests (4): the constitution's version line begins **Version**: 1.4.0.
' Author: RCH Automation LLC
' Created: 2026-09-29
'
' Origin: tests/CodeMem.Tests/Guards/BridgeStandaloneGateTests.vb, fact (4), written by feature 005 (T002, FR-431).
' RED:   2026-09-17 (005, T002) the version line read 1.3.0.
' GREEN: 2026-09-17 (005, T003) after the v1.4.0 amendment.
' RED:   2026-09-29 on main at 806b524 - "Assert.StartsWith() Failure: String start does not match / Expected start:
'        "**Version**: 1.4.0"": the v1.5.0 amendment moved the version line. Observed at 006's plan (two runs).
' Retired 2026-09-29 by feature 006 (T002; STOP 1 decision 1; Article XIV). The Architect's ruling: "fact (3) pins Article IX's
' text, which is what it protected; (4) now only breaks main on every amendment." Facts (1)-(3) remain in the live file.
' ConstitutionPath() is the class's own helper and stays there for fact (3).

' ---- BridgeStandaloneGateTests (4) TheConstitutionIsVersionOneFour ----
    ''' <summary>
    ''' (4) The constitution's version line begins **Version**: 1.4.0 (FR-431).
    ''' </summary>
    <Fact>
    Public Sub TheConstitutionIsVersionOneFour()
        Dim versionLine As String = Nothing
        For Each line As String In IO.File.ReadAllLines(ConstitutionPath())
            If line.StartsWith("**Version**:", StringComparison.Ordinal) Then versionLine = line
        Next
        Assert.NotNull(versionLine)
        Assert.StartsWith("**Version**: 1.4.0", versionLine, StringComparison.Ordinal)
    End Sub

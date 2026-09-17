' File: ChildEnvironment.vb
' Project: CodeMem.Tests
' Description: The environment a warning fact hands the extractor child: every NoWarn* variable of the test process removed, asserted before the launch (feature 005, X02; FR-427: the extractor reads no such variable and the facts prove the child ran without one).
' Author: RCH Automation LLC
' Created: 2026-09-17

Imports Xunit

''' <summary>
''' A removal is a key mapped to Nothing; ExtractorProcess.Run removes such keys from the child's environment (feature 005).
''' </summary>
Public Module ChildEnvironment

    ''' <summary>
    ''' The removals: one entry per NoWarn* variable the test process holds, mapped to Nothing.
    ''' </summary>
    ''' <returns>The dictionary to hand ExtractorProcess.Run.</returns>
    Public Function WithoutNoWarn() As Dictionary(Of String, String)
        Dim result As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        For Each key As Object In Environment.GetEnvironmentVariables().Keys
            If CStr(key).StartsWith("NoWarn", StringComparison.OrdinalIgnoreCase) Then result(CStr(key)) = Nothing
        Next
        Return result
    End Function

    ''' <summary>
    ''' Asserts that the child will receive no NoWarn* variable: every one the test process holds is in the removals.
    ''' </summary>
    ''' <param name="handed">The dictionary about to be handed to the launch.</param>
    Public Sub AssertNoNoWarn(handed As IDictionary(Of String, String))
        For Each key As Object In Environment.GetEnvironmentVariables().Keys
            Dim name As String = CStr(key)
            If Not name.StartsWith("NoWarn", StringComparison.OrdinalIgnoreCase) Then Continue For
            Dim value As String = Nothing
            Assert.True(handed.TryGetValue(name, value) AndAlso value Is Nothing, "the child would inherit " & name)
        Next
        For Each pair As KeyValuePair(Of String, String) In handed
            If pair.Key.StartsWith("NoWarn", StringComparison.OrdinalIgnoreCase) Then Assert.Null(pair.Value)
        Next
    End Sub

End Module

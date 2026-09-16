' File: ProcessReply.vb
' Project: CodeMem.Tests
' Description: What a one-shot run of the bridge executable returned: exit code, both streams and the elapsed time (feature 004).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' The captured result of <c>BridgeProcess.RunOnce</c> for the extract and hook entries.
''' </summary>
Public Class ProcessReply

    ''' <summary>The process exit code.</summary>
    Public ReadOnly Property ExitCode As Integer

    ''' <summary>Captured standard output.</summary>
    Public ReadOnly Property StandardOutput As String

    ''' <summary>Captured standard error.</summary>
    Public ReadOnly Property StandardError As String

    ''' <summary>Wall-clock time from start to exit, process start included.</summary>
    Public ReadOnly Property Elapsed As TimeSpan

    ''' <summary>
    ''' Wraps one run.
    ''' </summary>
    ''' <param name="exitCode">The exit code.</param>
    ''' <param name="standardOutput">Standard output.</param>
    ''' <param name="standardError">Standard error.</param>
    ''' <param name="elapsed">The elapsed time.</param>
    Public Sub New(exitCode As Integer, standardOutput As String, standardError As String, elapsed As TimeSpan)
        Me.ExitCode = exitCode
        Me.StandardOutput = standardOutput
        Me.StandardError = standardError
        Me.Elapsed = elapsed
    End Sub

End Class

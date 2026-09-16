' File: LaunchResult.vb
' Project: CodeMem.Bridging
' Description: What a launch returned: the exit code, both streams, the elapsed time, or a timeout (research R47, TIM1).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' ExitCode is Nothing exactly when the child was killed.
''' </summary>
Public Class LaunchResult

    ''' <summary>The exit code, or Nothing on a timeout.</summary>
    Public Property ExitCode As Integer?

    ''' <summary>Captured standard output.</summary>
    Public Property StandardOutput As String

    ''' <summary>Captured standard error.</summary>
    Public Property StandardError As String

    ''' <summary>Wall-clock time.</summary>
    Public Property Elapsed As TimeSpan

    ''' <summary>True when the budget was exceeded and the child killed.</summary>
    Public Property TimedOut As Boolean

End Class

' File: ExtractResult.vb
' Project: CodeMem.Bridging
' Description: The result of one extract: the origin, the target, the gate, a refusal or the launch's outcome, the run read back, the log outcome (contracts/tools.md §3.8; data-model §8; FR-328, FR-351).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.Text.Json.Serialization

''' <summary>
''' Serialised whole on the wire; a refused or failed result is still IsError there (<see cref="IsFailure"/>), so a caller cannot mistake it
''' for a run.
''' </summary>
Public Class ExtractResult

    ''' <summary>Read time.</summary>
    Public Property ReadAtUtc As String

    ''' <summary>tool, green_build or manual.</summary>
    Public Property Origin As String

    ''' <summary>The target as given and as resolved.</summary>
    Public Property Target As ExtractTargetEnvelope

    ''' <summary>passed, extract.enabled or extract.onGreenBuild; a dash when no gate was evaluated.</summary>
    Public Property Gate As String

    ''' <summary>The refusal, or null when the request was accepted.</summary>
    Public Property Refusal As ExtractRefusalEnvelope

    ''' <summary>True when the extractor was started.</summary>
    Public Property Launched As Boolean

    ''' <summary>The child's exit code, or null.</summary>
    Public Property ExitCode As Integer?

    ''' <summary>True when the child exceeded its budget and was killed (TIM1).</summary>
    Public Property TimedOut As Boolean

    ''' <summary>The summary, refusal or timeout line verbatim, or null.</summary>
    Public Property Line As String

    ''' <summary>The published run and its ten counts, or null.</summary>
    Public Property Run As ExtractRunEnvelope

    ''' <summary>Milliseconds from request to result.</summary>
    Public Property ElapsedMs As Long

    ''' <summary>True when the log line was written.</summary>
    Public Property Logged As Boolean

    ''' <summary>Why the log line could not be written, or null.</summary>
    Public Property LogError As String

    ''' <summary>Not serialised: true when the result is a refusal, a failed child or a timeout - IsError on the wire.</summary>
    <JsonIgnore>
    Public ReadOnly Property IsFailure As Boolean
        Get
            Return Refusal IsNot Nothing OrElse TimedOut OrElse (Launched AndAlso (Not ExitCode.HasValue OrElse ExitCode.Value <> 0))
        End Get
    End Property

End Class

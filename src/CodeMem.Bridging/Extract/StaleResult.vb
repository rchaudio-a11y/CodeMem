' File: StaleResult.vb
' Project: CodeMem.Bridging
' Description: The result of extract(stale): every bound solution considered with its verdict, the ones extracted with their results, or the refusal that stopped the verb (contracts/tools.md §3.8; FR-329).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.Text.Json.Serialization

''' <summary>
''' One line per solution launched is logged, no header (INC3).
''' </summary>
Public Class StaleResult

    ''' <summary>Read time.</summary>
    Public Property ReadAtUtc As String

    ''' <summary>tool or manual (never green_build: the trigger refreshes the built solution only, FR-338).</summary>
    Public Property Origin As String

    ''' <summary>passed, extract.enabled or extract.onGreenBuild; a dash when no gate was evaluated.</summary>
    Public Property Gate As String

    ''' <summary>The refusal that stopped the verb, or null.</summary>
    Public Property Refusal As ExtractRefusalEnvelope

    ''' <summary>Every bound solution with its verdict and outcome.</summary>
    Public Property Considered As List(Of StaleEntryEnvelope)

    ''' <summary>How many were extracted.</summary>
    Public Property ExtractedCount As Integer

    ''' <summary>The observed directories not in the map, as map_status lists them; never extracted (005 FR-420).</summary>
    Public Property NotInMap As List(Of NotInMapEntryEnvelope)

    ''' <summary>Not serialised: true when the verb was refused.</summary>
    <JsonIgnore>
    Public ReadOnly Property IsFailure As Boolean
        Get
            Return Refusal IsNot Nothing
        End Get
    End Property

End Class

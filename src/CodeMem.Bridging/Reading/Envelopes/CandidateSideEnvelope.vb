' File: CandidateSideEnvelope.vb
' Project: CodeMem.Bridging
' Description: A symbol as rename_candidates presents it - a candidate's side, or the symbol block (feature 006, T011; data-model §2-§3).
' Author: RCH Automation LLC
' Created: 2026-09-29

''' <summary>
''' id, docCommentId, kind, name, container, project, path, line and the current active state. lastSeenRunId and retiredInRunId are set on
''' the symbol block only and are null on a candidate's sides.
''' </summary>
Public Class CandidateSideEnvelope

    ''' <summary>The symbol id.</summary>
    Public Property Id As Long

    ''' <summary>The doc-comment id.</summary>
    Public Property DocCommentId As String

    ''' <summary>The kind text.</summary>
    Public Property Kind As String

    ''' <summary>The name.</summary>
    Public Property Name As String

    ''' <summary>The container, or null.</summary>
    Public Property Container As NamedRefEnvelope

    ''' <summary>The declaring project, or null (spec Q1: it tells twins apart).</summary>
    Public Property Project As NamedRefEnvelope

    ''' <summary>The primary declaration's path, as last observed.</summary>
    Public Property Path As String

    ''' <summary>The primary declaration's line, as last observed.</summary>
    Public Property Line As Integer

    ''' <summary>Whether the symbol is active now (a retired side can be reactivated, a new side retired since).</summary>
    Public Property IsActive As Boolean

    ''' <summary>The run that last observed the symbol; symbol block only, otherwise null.</summary>
    Public Property LastSeenRunId As Long?

    ''' <summary>The run that retired the symbol when it is retired now (spec Q2); symbol block only, otherwise null.</summary>
    Public Property RetiredInRunId As Long?

End Class

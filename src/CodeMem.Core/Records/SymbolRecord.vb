' File: SymbolRecord.vb
' Project: CodeMem.Core
' Description: One code_symbols row as the bridge reads it: every column plus the joined container name, project name and solution key (feature 004).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' A symbol row for presentation. Kind is the schema text; the three joined labels are Nothing where the row has no container or project.
''' </summary>
Public Class SymbolRecord

    ''' <summary>The row id.</summary>
    Public Property Id As Long

    ''' <summary>Owning solution.</summary>
    Public Property SolutionId As Long

    ''' <summary>The owning solution's key (joined).</summary>
    Public Property SolutionKey As String

    ''' <summary>The doc-comment id.</summary>
    Public Property DocCommentId As String

    ''' <summary>The kind text.</summary>
    Public Property Kind As String

    ''' <summary>Surface name.</summary>
    Public Property Name As String

    ''' <summary>Container row id, or Nothing.</summary>
    Public Property ContainerId As Long?

    ''' <summary>Container name (joined, active rows only), or Nothing.</summary>
    Public Property ContainerName As String

    ''' <summary>Declaring project row id, or Nothing.</summary>
    Public Property ProjectSymbolId As Long?

    ''' <summary>Declaring project name (joined, active rows only), or Nothing.</summary>
    Public Property ProjectName As String

    ''' <summary>Primary declaration path, solution-relative.</summary>
    Public Property Path As String

    ''' <summary>Primary declaration start offset.</summary>
    Public Property StartOffset As Integer

    ''' <summary>Primary declaration length.</summary>
    Public Property Length As Integer

    ''' <summary>Primary declaration start line.</summary>
    Public Property StartLine As Integer

    ''' <summary>Primary declaration start column.</summary>
    Public Property StartColumn As Integer

    ''' <summary>Body hash.</summary>
    Public Property BodyHash As String

    ''' <summary>True when active.</summary>
    Public Property IsActive As Boolean

    ''' <summary>The run that minted the row.</summary>
    Public Property FirstSeenRunId As Long

    ''' <summary>The last run that observed the row.</summary>
    Public Property LastSeenRunId As Long

End Class

' File: RegistryRow.vb
' Project: CodeMem.Core
' Description: A code_symbols row as read from the map.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' One <c>code_symbols</c> row, one property per column.
''' </summary>
Public Class RegistryRow

    ''' <summary>Surrogate id: the durable handle.</summary>
    Public Property Id As Long

    ''' <summary>Owning solution.</summary>
    Public Property SolutionId As Long

    ''' <summary>Identity within the solution.</summary>
    Public Property DocCommentId As String

    ''' <summary>The kind.</summary>
    Public Property Kind As SymbolKind

    ''' <summary>Surface name.</summary>
    Public Property Name As String

    ''' <summary>Container row id, or Nothing.</summary>
    Public Property ContainerId As Long?

    ''' <summary>Declaring project's row id, or Nothing for namespace and project rows.</summary>
    Public Property ProjectSymbolId As Long?

    ''' <summary>Primary declaration path.</summary>
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

    ''' <summary>True when active, False when retired.</summary>
    Public Property IsActive As Boolean

    ''' <summary>Run that minted the row.</summary>
    Public Property FirstSeenRunId As Long

    ''' <summary>Last run that observed the row.</summary>
    Public Property LastSeenRunId As Long

    ''' <summary>
    ''' The primary declaration as a location.
    ''' </summary>
    ''' <returns>The location built from the five location columns.</returns>
    Public Function Location() As SourceLocation
        Return New SourceLocation(Path, StartOffset, Length, StartLine, StartColumn)
    End Function

End Class

' File: DeclarationEnvelope.vb
' Project: CodeMem.Bridging
' Description: One presented declaration of symbol_search (contracts/tools.md §3.2): the first twin's labels plus compiledInto.
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' A row of symbols[]; id, docCommentId, container and project are the first twin's.
''' </summary>
Public Class DeclarationEnvelope

    ''' <summary>The solution id.</summary>
    Public Property SolutionId As Long

    ''' <summary>The solution key.</summary>
    Public Property SolutionKey As String

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

    ''' <summary>The declaring project, or null.</summary>
    Public Property Project As NamedRefEnvelope

    ''' <summary>The declaration path.</summary>
    Public Property Path As String

    ''' <summary>The declaration line.</summary>
    Public Property Line As Integer

    ''' <summary>Every project the declaration is compiled into.</summary>
    Public Property CompiledInto As List(Of CompiledIntoEnvelope)

End Class

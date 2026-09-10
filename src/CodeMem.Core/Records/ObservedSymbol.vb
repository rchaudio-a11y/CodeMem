' File: ObservedSymbol.vb
' Project: CodeMem.Core
' Description: A source-declared symbol as observed by one run, staged in memory before reconciliation.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' A staged symbol. Container and project are doc-comment ids here and resolve to row ids at publication.
''' </summary>
Public Class ObservedSymbol

    ''' <summary>The compiler's documentation-comment id, or <c>Project:name</c> for a project row.</summary>
    Public Property DocCommentId As String

    ''' <summary>The kind.</summary>
    Public Property Kind As SymbolKind

    ''' <summary>The VB surface name; <c>New</c> for constructors.</summary>
    Public Property Name As String

    ''' <summary>Doc-comment id of the containing symbol, or Nothing when the container is the global namespace or there is none.</summary>
    Public Property ContainerDocCommentId As String

    ''' <summary>Doc-comment id of the declaring project's row; Nothing for namespace and project symbols.</summary>
    Public Property ProjectDocCommentId As String

    ''' <summary>Location of the primary declaration's identifier (first part in (path, offset) order: FR-010, FR-018).</summary>
    Public Property Primary As SourceLocation

    ''' <summary>SHA-256 hex over every part's hash input in (path, offset) order (FR-014).</summary>
    Public Property BodyHash As String

    ''' <summary>The declaring parts, sorted by (path ordinal, start offset).</summary>
    Public Property Parts As List(Of ObservedPart) = New List(Of ObservedPart)()

End Class

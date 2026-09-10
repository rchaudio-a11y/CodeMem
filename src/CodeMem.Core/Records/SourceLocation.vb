' File: SourceLocation.vb
' Project: CodeMem.Core
' Description: A path and span in a compiled input: the one shape every location in the map takes.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' A solution-relative path plus a span, in both the compiler's native form (0-based offset and length)
''' and the human form (1-based line and column). Article VII: evidence on every row.
''' </summary>
Public Structure SourceLocation

    ''' <summary>Solution-relative path with forward slashes.</summary>
    Public Property Path As String

    ''' <summary>0-based character offset of the span start.</summary>
    Public Property StartOffset As Integer

    ''' <summary>Length of the span in characters.</summary>
    Public Property Length As Integer

    ''' <summary>1-based line of the span start.</summary>
    Public Property StartLine As Integer

    ''' <summary>1-based column of the span start.</summary>
    Public Property StartColumn As Integer

    ''' <summary>
    ''' Creates a location.
    ''' </summary>
    ''' <param name="path">Solution-relative path with forward slashes.</param>
    ''' <param name="startOffset">0-based start offset.</param>
    ''' <param name="length">Span length.</param>
    ''' <param name="startLine">1-based start line.</param>
    ''' <param name="startColumn">1-based start column.</param>
    Public Sub New(path As String, startOffset As Integer, length As Integer, startLine As Integer, startColumn As Integer)
        Me.Path = path
        Me.StartOffset = startOffset
        Me.Length = length
        Me.StartLine = startLine
        Me.StartColumn = startColumn
    End Sub

End Structure

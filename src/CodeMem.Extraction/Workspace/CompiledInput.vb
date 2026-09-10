' File: CompiledInput.vb
' Project: CodeMem.Extraction
' Description: One compiled input: a source document, project file or solution file with its normalized text.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' An entry of the compiled-inputs enumeration (FR-005). Text has CRLF normalized to LF and no leading BOM.
''' </summary>
Public Class CompiledInput

    ''' <summary>Solution-relative path, forward slashes.</summary>
    Public Property RelativePath As String

    ''' <summary>Absolute path.</summary>
    Public Property FullPath As String

    ''' <summary>Normalized text.</summary>
    Public Property Text As String

End Class

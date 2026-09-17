' File: WorkspaceLoadException.vb
' Project: CodeMem.Extraction
' Description: Raised when MSBuildWorkspace reports a Failure diagnostic or a project cannot be compiled (exit 1).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-17 (feature 005, T031): Refusal(line) - the message is the line itself; the failures constructor keeps its prefix.

''' <summary>
''' The workspace could not load the solution. Not a compile error (that is exit 2).
''' </summary>
Public Class WorkspaceLoadException
    Inherits Exception

    ''' <summary>
    ''' Creates the exception from the workspace's failure messages.
    ''' </summary>
    ''' <param name="failures">The Failure diagnostics.</param>
    Public Sub New(failures As IEnumerable(Of String))
        MyBase.New("workspace load failed:" & Environment.NewLine & String.Join(Environment.NewLine, failures))
    End Sub

    Private Sub New(line As String)
        MyBase.New(line)
    End Sub

    ''' <summary>
    ''' A refusal whose one stderr line is exactly the text given, without the "workspace load failed:" prefix (feature 005: the .slnx parse's
    ''' four refusals and the unsupported extension, contracts/extractor.md §1-§2).
    ''' </summary>
    ''' <param name="line">The line.</param>
    ''' <returns>The exception.</returns>
    Public Shared Function Refusal(line As String) As WorkspaceLoadException
        Return New WorkspaceLoadException(line)
    End Function

End Class

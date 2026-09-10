' File: WorkspaceLoadException.vb
' Project: CodeMem.Extraction
' Description: Raised when MSBuildWorkspace reports a Failure diagnostic or a project cannot be compiled (exit 1).
' Author: RCH Automation LLC
' Created: 2026-09-09

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

End Class

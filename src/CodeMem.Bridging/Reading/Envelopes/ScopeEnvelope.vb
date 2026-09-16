' File: ScopeEnvelope.vb
' Project: CodeMem.Bridging
' Description: The scope every successful result carries (058 §3.1; data-model §3; spec Q7 adds reason).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' By projectId or solutionKey; the four registry fields are null by key.
''' </summary>
Public Class ScopeEnvelope

    ''' <summary>projectId or solutionKey.</summary>
    Public Property By As String

    ''' <summary>The project id, or null.</summary>
    Public Property ProjectId As Long?

    ''' <summary>The solution key, or null.</summary>
    Public Property SolutionKey As String

    ''' <summary>The solutions in scope, ordered by key.</summary>
    Public Property Solutions As List(Of ScopeSolutionEnvelope)

    ''' <summary>Active registry rows of the project, or null by key.</summary>
    Public Property ActiveRegistryRows As Integer?

    ''' <summary>Active rows with no binding, or null by key.</summary>
    Public Property UnboundRegistryRows As Integer?

    ''' <summary>Rows not active, or null by key.</summary>
    Public Property InactiveRegistryRows As Integer?

    ''' <summary>Bound ids the map lacks, or null by key.</summary>
    Public Property DanglingSolutionIds As List(Of Long)

    ''' <summary>True when the scope holds no solution.</summary>
    Public Property HadNothingToSearch As Boolean

    ''' <summary>Why nothing was searched, or null (spec Q7).</summary>
    Public Property Reason As String

End Class

' File: LaunchRequest.vb
' Project: CodeMem.Bridging
' Description: What the launcher is asked to run: the extractor, the solution, the map, the key and the budget (research R47, TIM1).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Built by the door from the resolved target; recorded by the scripted launcher.
''' </summary>
Public Class LaunchRequest

    ''' <summary>The extractor dll or exe.</summary>
    Public Property ExtractorPath As String

    ''' <summary>The solution path passed as --solution.</summary>
    Public Property SolutionPath As String

    ''' <summary>The map passed as --db.</summary>
    Public Property MapPath As String

    ''' <summary>The key passed as --solution-key.</summary>
    Public Property SolutionKey As String

    ''' <summary>The time the child may take before it is killed.</summary>
    Public Property Budget As TimeSpan

End Class

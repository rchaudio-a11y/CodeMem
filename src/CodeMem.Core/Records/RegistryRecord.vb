' File: RegistryRecord.vb
' Project: CodeMem.Core
' Description: One code_map_solutions row of the MemOS store as the bridge reads it (feature 004, data-model §2).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' A registry row: which MemOS project a solution key belongs to and which map solution it is bound to. Read-only here.
''' </summary>
Public Class RegistryRecord

    ''' <summary>The row id.</summary>
    Public Property Id As Long

    ''' <summary>The MemOS project id.</summary>
    Public Property ProjectId As Long

    ''' <summary>The value passed as --solution-key; never defaulted.</summary>
    Public Property SolutionKey As String

    ''' <summary>The map's solutions.id, or Nothing until the first run publishes.</summary>
    Public Property CodememSolutionId As Long?

    ''' <summary>The anchored scope text; read, not used by the bridge (spec Q1).</summary>
    Public Property ExtractionScope As String

    ''' <summary>active or inactive.</summary>
    Public Property State As String

    ''' <summary>Free text, or Nothing.</summary>
    Public Property Notes As String

End Class

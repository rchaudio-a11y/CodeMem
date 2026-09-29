' File: RenameCandidateRecord.vb
' Project: CodeMem.Core
' Description: One stored rename_candidates row as the bridge reads it (feature 006, T008; research R76).
' Author: RCH Automation LLC
' Created: 2026-09-29
'
' RenameCandidate is the reconciler's proposal before the new id is minted (it carries the new side's doc-comment id); this is the row
' after publication, with both ids - the pre- and post-publication views of one concept, as RegistryRow and SymbolRecord are for
' code_symbols.

''' <summary>
''' A published candidate: the run that wrote it, the retired and new symbol ids, and the proximity evidence. The body hash is not read.
''' </summary>
Public Class RenameCandidateRecord

    ''' <summary>The row id.</summary>
    Public Property Id As Long

    ''' <summary>The solution.</summary>
    Public Property SolutionId As Long

    ''' <summary>The run that wrote the candidate (the run that retired the retired side).</summary>
    Public Property RunId As Long

    ''' <summary>The retired symbol's id.</summary>
    Public Property RetiredSymbolId As Long

    ''' <summary>The new symbol's id.</summary>
    Public Property NewSymbolId As Long

    ''' <summary>Whether both sides' primary declarations are in the same file.</summary>
    Public Property SamePath As Boolean

    ''' <summary>The distance between the two start offsets; null when same_path = 0.</summary>
    Public Property OffsetDistance As Integer?

    ''' <summary>The proximity rank among the candidates of one new symbol; 1 is nearest.</summary>
    Public Property Rank As Integer

End Class

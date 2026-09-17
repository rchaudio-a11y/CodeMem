' File: MapStatusEnvelope.vb
' Project: CodeMem.Bridging
' Description: map_status: readAtUtc, the map path, one entry per map solution, the directories observed but not in the map (005 data-model §5).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T015): storePath, bound, unbound and inactive are gone with the registry; notInMap and notInMapError arrive
' (filled by NotInMapReader at T028; an empty list and null until then).

''' <summary>
''' readAtUtc, the map path, the entries, the observed directories.
''' </summary>
Public Class MapStatusEnvelope

    ''' <summary>Read time.</summary>
    Public Property ReadAtUtc As String

    ''' <summary>The map path.</summary>
    Public Property MapPath As String

    ''' <summary>One entry per map solution, ordered by key.</summary>
    Public Property Entries As List(Of MapStatusEntryEnvelope)

    ''' <summary>Directories an extract refused as not in the map that no mapped root contains yet, ordered by path.</summary>
    Public Property NotInMap As List(Of NotInMapEntryEnvelope)

    ''' <summary>Why the extract log could not be read, or null.</summary>
    Public Property NotInMapError As String

End Class

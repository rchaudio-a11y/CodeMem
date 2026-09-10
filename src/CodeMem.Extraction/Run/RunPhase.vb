' File: RunPhase.vb
' Project: CodeMem.Extraction
' Description: The points at which the test-only abort seam can stop the process (I9).
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' Values of <c>CODEMEM_TEST_ABORT_AT</c>. Test-only.
''' </summary>
Public Enum RunPhase
    ''' <summary>No abort.</summary>
    None
    ''' <summary>Abort after staging and validation, before the first fact-table write.</summary>
    AfterStaging
    ''' <summary>Abort between the parts and edges replacement, inside the publication transaction.</summary>
    DuringPublish
End Enum

' File: RunPhase.vb
' Project: CodeMem.Extraction
' Description: The points at which the test-only abort seam can stop the process (I9; fixpack 002 F6 and the schema upgrade).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-10 (fixpack 002): DuringInitialize and DuringUpgrade added (data-model.md "Test-only seam values"); arming now needs the nonce (research R26).

''' <summary>
''' Values of the phase half of <c>CODEMEM_TEST_ABORT_AT</c> (<c>&lt;phase&gt;:&lt;nonce&gt;</c>). Test-only; inert without a matching <c>CODEMEM_TEST_NONCE</c>.
''' </summary>
Public Enum RunPhase
    ''' <summary>No abort.</summary>
    None
    ''' <summary>Fresh map only: abort after the version-1 schema and the identity row are written, before the migration to version 2 (FR-106).</summary>
    DuringInitialize
    ''' <summary>Abort after the migration statements have run, before map_identity.schema_version is set to 2 (FR-113).</summary>
    DuringUpgrade
    ''' <summary>Abort after staging and validation, before the first fact-table write.</summary>
    AfterStaging
    ''' <summary>Abort between the parts and edges replacement, inside the publication transaction.</summary>
    DuringPublish
End Enum

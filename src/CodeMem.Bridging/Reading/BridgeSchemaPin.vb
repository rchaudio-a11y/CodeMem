' File: BridgeSchemaPin.vb
' Project: CodeMem.Bridging
' Description: The schema version the bridge reads, pinned as its own constant (FR-305; 137077, the condition that makes duplication safe).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T010): Required = 3 (extract_run_warnings, FR-430). The 004 sentence held: Core moved to 3 at T009 and every bridge
' fact went red with "schema version 3; this bridge requires 2" before this constant moved.

''' <summary>
''' Exactly 3. Not <c>SchemaVersion.Current</c> by reference: a Core bump that moves the extractor to 4 turns the bridge's refusal on rather
''' than silently widening it.
''' </summary>
Public Module BridgeSchemaPin

    ''' <summary>The one version the bridge reads.</summary>
    Public Const Required As Integer = 3

End Module

' File: BridgeSchemaPin.vb
' Project: CodeMem.Bridging
' Description: The schema version the bridge reads, pinned as its own constant (FR-305; 137077, the condition that makes duplication safe).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Exactly 2. Not <c>SchemaVersion.Current</c> by reference: a Core bump that moves the extractor to 3 turns the bridge's refusal on rather
''' than silently widening it.
''' </summary>
Public Module BridgeSchemaPin

    ''' <summary>The one version the bridge reads.</summary>
    Public Const Required As Integer = 2

End Module

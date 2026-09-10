' File: RunSeams.vb
' Project: CodeMem.Extraction
' Description: Test-only in-process seams reachable through the production entry point (Articles XII, XIII).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports CodeMem.Core

''' <summary>
''' Hooks a test may pass to <c>ExtractionRun.Execute</c>. Both are test-only; <c>Main</c> passes Nothing for the whole object
''' and neither hook has any effect in production. Justified in plan.md Complexity Tracking.
''' </summary>
Public Class RunSeams

    ''' <summary>Invoked on the staged counts before the auditor runs (I8). Test-only.</summary>
    Public Property CorruptStagedCounts As Action(Of RunCounts)

    ''' <summary>Invoked on the staged symbols after the namespace merge, before reconciliation (schema-constraint propagation proof). Test-only.</summary>
    Public Property MutateStaged As Action(Of List(Of ObservedSymbol))

End Class

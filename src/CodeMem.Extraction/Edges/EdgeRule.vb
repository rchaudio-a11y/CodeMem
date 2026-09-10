' File: EdgeRule.vb
' Project: CodeMem.Extraction
' Description: The contract each of the eight verb rules implements (Article IV: one deterministic rule per verb).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports CodeMem.Core

''' <summary>
''' A rule that collects the edges of one verb from a compilation (or, for part_of, from the merged staged symbols).
''' </summary>
Public Interface EdgeRule

    ''' <summary>
    ''' Appends every edge the rule derives from the context.
    ''' </summary>
    ''' <param name="context">The compilation, its source trees, the staged symbols and the row lookup.</param>
    ''' <param name="sink">Where edges accumulate.</param>
    Sub Collect(context As EdgeContext, sink As List(Of ObservedEdge))

End Interface

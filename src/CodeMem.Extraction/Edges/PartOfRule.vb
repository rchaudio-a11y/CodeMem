' File: PartOfRule.vb
' Project: CodeMem.Extraction
' Description: The part_of verb: every row except project to its containing symbol; none when the container is the global namespace.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports CodeMem.Core

''' <summary>
''' Runs once over the merged staged symbols so a namespace declared in several projects yields one edge.
''' </summary>
Public Class PartOfRule
    Implements EdgeRule

    ''' <inheritdoc/>
    Public Sub Collect(context As EdgeContext, sink As List(Of ObservedEdge)) Implements EdgeRule.Collect
        For Each symbol As ObservedSymbol In context.Symbols
            If symbol.Kind = CodeMem.Core.SymbolKind.Project Then Continue For
            If symbol.ContainerDocCommentId Is Nothing Then Continue For
            sink.Add(New ObservedEdge With {
                .SourceDocCommentId = symbol.DocCommentId,
                .Verb = EdgeVerb.PartOf,
                .TargetDocCommentId = symbol.ContainerDocCommentId,
                .ViaDocCommentId = Nothing,
                .Location = symbol.Primary})
        Next
    End Sub

End Class

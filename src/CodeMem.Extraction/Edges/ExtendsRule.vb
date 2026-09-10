' File: ExtendsRule.vb
' Project: CodeMem.Extraction
' Description: The extends verb: a type with an Inherits statement to its bound base type.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports CodeMem.Core
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

''' <summary>
''' Bound base types only; the occurrence is the rightmost name token of the base type syntax.
''' </summary>
Public Class ExtendsRule
    Implements EdgeRule

    ''' <inheritdoc/>
    Public Sub Collect(context As EdgeContext, sink As List(Of ObservedEdge)) Implements EdgeRule.Collect
        For Each tree As SyntaxTree In context.Trees
            Dim model As SemanticModel = context.ModelFor(tree)
            For Each node As SyntaxNode In tree.GetRoot().DescendantNodes()
                Dim block As TypeBlockSyntax = TryCast(node, TypeBlockSyntax)
                If block Is Nothing OrElse block.Inherits.Count = 0 Then Continue For
                Dim source As String = context.DocIdOf(model.GetDeclaredSymbol(block))
                If Not context.IsRow(source) Then Continue For
                For Each statement As InheritsStatementSyntax In block.Inherits
                    For Each typeSyntax As TypeSyntax In statement.Types
                        Dim target As ISymbol = model.GetSymbolInfo(typeSyntax).Symbol
                        Dim targetDocId As String = context.DocIdOf(target)
                        If targetDocId Is Nothing Then Continue For
                        sink.Add(New ObservedEdge With {
                            .SourceDocCommentId = source,
                            .Verb = EdgeVerb.Extends,
                            .TargetDocCommentId = targetDocId,
                            .ViaDocCommentId = Nothing,
                            .Location = context.LocationOf(PartResolver.NameTokenOf(typeSyntax))})
                    Next
                Next
            Next
        Next
    End Sub

End Class

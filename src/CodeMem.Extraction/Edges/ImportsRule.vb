' File: ImportsRule.vb
' Project: CodeMem.Extraction
' Description: The imports verb: every top-level type declared in a file to each namespace a file-level Imports statement names.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports CodeMem.Core
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

''' <summary>
''' Alias and XML imports are excluded; project-level imports have no syntax and are never written.
''' </summary>
Public Class ImportsRule
    Implements EdgeRule

    ''' <inheritdoc/>
    Public Sub Collect(context As EdgeContext, sink As List(Of ObservedEdge)) Implements EdgeRule.Collect
        For Each tree As SyntaxTree In context.Trees
            Dim root As CompilationUnitSyntax = TryCast(tree.GetRoot(), CompilationUnitSyntax)
            If root Is Nothing OrElse root.Imports.Count = 0 Then Continue For
            Dim model As SemanticModel = context.ModelFor(tree)
            Dim sources As List(Of String) = TopLevelTypeRows(context, model, root)
            If sources.Count = 0 Then Continue For
            For Each statement As ImportsStatementSyntax In root.Imports
                For Each clause As ImportsClauseSyntax In statement.ImportsClauses
                    Dim simple As SimpleImportsClauseSyntax = TryCast(clause, SimpleImportsClauseSyntax)
                    If simple Is Nothing OrElse simple.Alias IsNot Nothing Then Continue For
                    Dim target As INamespaceSymbol = TryCast(model.GetSymbolInfo(simple.Name).Symbol, INamespaceSymbol)
                    Dim targetDocId As String = context.DocIdOf(target)
                    If targetDocId Is Nothing Then Continue For
                    Dim location As SourceLocation = context.LocationOf(PartResolver.NameTokenOf(simple.Name))
                    For Each source As String In sources
                        sink.Add(New ObservedEdge With {
                            .SourceDocCommentId = source,
                            .Verb = EdgeVerb.Imports_,
                            .TargetDocCommentId = targetDocId,
                            .ViaDocCommentId = Nothing,
                            .Location = location})
                    Next
                Next
            Next
        Next
    End Sub

    Private Shared Function TopLevelTypeRows(context As EdgeContext, model As SemanticModel, root As CompilationUnitSyntax) As List(Of String)
        Dim result As List(Of String) = New List(Of String)()
        For Each node As SyntaxNode In root.DescendantNodes(Function(n As SyntaxNode) TypeOf n Is CompilationUnitSyntax OrElse TypeOf n Is NamespaceBlockSyntax)
            If Not (TypeOf node Is TypeBlockSyntax OrElse TypeOf node Is EnumBlockSyntax OrElse TypeOf node Is DelegateStatementSyntax) Then Continue For
            If Not (TypeOf node.Parent Is CompilationUnitSyntax OrElse TypeOf node.Parent Is NamespaceBlockSyntax) Then Continue For
            Dim docId As String = context.DocIdOf(model.GetDeclaredSymbol(node))
            If context.IsRow(docId) Then result.Add(docId)
        Next
        Return result
    End Function

End Class

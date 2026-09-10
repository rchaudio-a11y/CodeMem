' File: ImplementsRule.vb
' Project: CodeMem.Extraction
' Description: The implements verb: type to each listed interface; member to each interface member in its Implements clause.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports CodeMem.Core
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

''' <summary>
''' Bound targets only; the occurrence is the rightmost name token.
''' </summary>
Public Class ImplementsRule
    Implements EdgeRule

    ''' <inheritdoc/>
    Public Sub Collect(context As EdgeContext, sink As List(Of ObservedEdge)) Implements EdgeRule.Collect
        For Each tree As SyntaxTree In context.Trees
            Dim model As SemanticModel = context.ModelFor(tree)
            For Each node As SyntaxNode In tree.GetRoot().DescendantNodes()
                Dim block As TypeBlockSyntax = TryCast(node, TypeBlockSyntax)
                If block IsNot Nothing Then
                    Dim source As String = context.DocIdOf(model.GetDeclaredSymbol(block))
                    If Not context.IsRow(source) Then Continue For
                    For Each statement As ImplementsStatementSyntax In block.Implements
                        For Each typeSyntax As TypeSyntax In statement.Types
                            Add(context, model, sink, source, typeSyntax)
                        Next
                    Next
                    Continue For
                End If
                Dim clause As ImplementsClauseSyntax = Nothing
                Dim declaration As SyntaxNode = Nothing
                Dim method As MethodStatementSyntax = TryCast(node, MethodStatementSyntax)
                If method IsNot Nothing Then
                    clause = method.ImplementsClause
                    declaration = method
                End If
                Dim [property] As PropertyStatementSyntax = TryCast(node, PropertyStatementSyntax)
                If [property] IsNot Nothing Then
                    clause = [property].ImplementsClause
                    declaration = [property]
                End If
                Dim [event] As EventStatementSyntax = TryCast(node, EventStatementSyntax)
                If [event] IsNot Nothing Then
                    clause = [event].ImplementsClause
                    declaration = [event]
                End If
                If clause Is Nothing Then Continue For
                Dim memberSource As String = context.DocIdOf(model.GetDeclaredSymbol(declaration))
                If Not context.IsRow(memberSource) Then Continue For
                For Each name As QualifiedNameSyntax In clause.InterfaceMembers
                    Add(context, model, sink, memberSource, name)
                Next
            Next
        Next
    End Sub

    Private Shared Sub Add(context As EdgeContext, model As SemanticModel, sink As List(Of ObservedEdge), source As String, name As TypeSyntax)
        Dim target As ISymbol = model.GetSymbolInfo(name).Symbol
        Dim targetDocId As String = context.DocIdOf(target)
        If targetDocId Is Nothing Then Return
        sink.Add(New ObservedEdge With {
            .SourceDocCommentId = source,
            .Verb = EdgeVerb.Implements_,
            .TargetDocCommentId = targetDocId,
            .ViaDocCommentId = Nothing,
            .Location = context.LocationOf(PartResolver.NameTokenOf(name))})
    End Sub

End Class

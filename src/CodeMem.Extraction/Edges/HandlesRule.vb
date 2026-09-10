' File: HandlesRule.vb
' Project: CodeMem.Extraction
' Description: The handles verb: one edge per Handles item (via the WithEvents member) and per AddHandler with an AddressOf method (spec Q5, I3).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports CodeMem.Core
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

''' <summary>
''' Handler method to event. Unbound items are not written.
''' </summary>
Public Class HandlesRule
    Implements EdgeRule

    ''' <inheritdoc/>
    Public Sub Collect(context As EdgeContext, sink As List(Of ObservedEdge)) Implements EdgeRule.Collect
        For Each tree As SyntaxTree In context.Trees
            Dim model As SemanticModel = context.ModelFor(tree)
            Dim root As SyntaxNode = tree.GetRoot()
            For Each node As SyntaxNode In root.DescendantNodes()
                Dim statement As MethodStatementSyntax = TryCast(node, MethodStatementSyntax)
                If statement IsNot Nothing AndAlso statement.HandlesClause IsNot Nothing Then
                    CollectHandlesClause(context, model, statement, sink)
                    Continue For
                End If
                If node.IsKind(SyntaxKind.AddHandlerStatement) Then
                    CollectAddHandler(context, model, DirectCast(node, AddRemoveHandlerStatementSyntax), sink)
                End If
            Next
        Next
    End Sub

    Private Shared Sub CollectHandlesClause(context As EdgeContext, model As SemanticModel, statement As MethodStatementSyntax, sink As List(Of ObservedEdge))
        Dim source As String = context.DocIdOf(model.GetDeclaredSymbol(statement))
        If Not context.IsRow(source) Then Return
        For Each item As HandlesClauseItemSyntax In statement.HandlesClause.Events
            Dim target As IEventSymbol = TryCast(model.GetSymbolInfo(item.EventMember).Symbol, IEventSymbol)
            If target Is Nothing Then Continue For
            Dim via As String = Nothing
            Dim container As EventContainerSyntax = item.EventContainer
            Dim member As ISymbol = Nothing
            If TypeOf container Is WithEventsEventContainerSyntax Then
                member = model.GetSymbolInfo(container).Symbol
            ElseIf TypeOf container Is WithEventsPropertyEventContainerSyntax Then
                member = model.GetSymbolInfo(DirectCast(container, WithEventsPropertyEventContainerSyntax).Property).Symbol
            End If
            If member IsNot Nothing Then
                Dim memberDocId As String = context.DocIdOf(member)
                If context.IsRow(memberDocId) Then via = memberDocId
            End If
            sink.Add(New ObservedEdge With {
                .SourceDocCommentId = source,
                .Verb = EdgeVerb.Handles_,
                .TargetDocCommentId = context.DocIdOf(target),
                .ViaDocCommentId = via,
                .Location = context.LocationOf(item.EventMember.Identifier)})
        Next
    End Sub

    Private Shared Sub CollectAddHandler(context As EdgeContext, model As SemanticModel, statement As AddRemoveHandlerStatementSyntax, sink As List(Of ObservedEdge))
        Dim unary As UnaryExpressionSyntax = TryCast(statement.DelegateExpression, UnaryExpressionSyntax)
        If unary Is Nothing OrElse Not unary.IsKind(SyntaxKind.AddressOfExpression) Then Return
        Dim method As IMethodSymbol = TryCast(model.GetSymbolInfo(unary.Operand).Symbol, IMethodSymbol)
        Dim target As IEventSymbol = TryCast(model.GetSymbolInfo(statement.EventExpression).Symbol, IEventSymbol)
        If method Is Nothing OrElse target Is Nothing Then Return
        Dim source As String = context.DocIdOf(method)
        If Not context.IsRow(source) Then Return
        Dim nameToken As SyntaxToken = PartResolver.NameTokenOf(statement.EventExpression)
        sink.Add(New ObservedEdge With {
            .SourceDocCommentId = source,
            .Verb = EdgeVerb.Handles_,
            .TargetDocCommentId = context.DocIdOf(target),
            .ViaDocCommentId = Nothing,
            .Location = context.LocationOf(nameToken)})
    End Sub

End Class

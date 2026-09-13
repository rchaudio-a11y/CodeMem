' File: CallsRule.vb
' Project: CodeMem.Extraction
' Description: The calls verb: invocation, object creation, member access and (fixpack 003) a bare name to the bound member; implicit constructors redirect to the type (FR-017, spec Q4; 003 FR-208..FR-211).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-13 (fixpack 003, rule 2): the fourth occurrence shape - a simple name the compiler binds to a field, property, event or method - is
' collected by CollectBareName, one occurrence per identifier at the identifier, and only when the target is a row of this run (FR-209, a stated
' limit: a bare external member writes nothing). A bare name that is also the expression of an invocation folds into the invocation's edge in
' ExtractionRun.Canonical (FR-213).

Imports CodeMem.Core
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

''' <summary>
''' Unresolved or candidate-ambiguous bindings, locals, parameters, range variables, type parameters, types and namespaces are never targets.
''' The bare-name shape additionally skips the member name of a member access, any part of a qualified name, an attribute name, the other
''' verbs' clauses, constructors, and any target without a row.
''' </summary>
Public Class CallsRule
    Implements EdgeRule

    ''' <inheritdoc/>
    Public Sub Collect(context As EdgeContext, sink As List(Of ObservedEdge)) Implements EdgeRule.Collect
        For Each tree As SyntaxTree In context.Trees
            Dim model As SemanticModel = context.ModelFor(tree)
            For Each node As SyntaxNode In tree.GetRoot().DescendantNodes()
                Dim simple As SimpleNameSyntax = TryCast(node, SimpleNameSyntax)
                If simple IsNot Nothing Then
                    CollectBareName(context, model, simple, sink)
                    Continue For
                End If
                If Not (TypeOf node Is InvocationExpressionSyntax OrElse TypeOf node Is ObjectCreationExpressionSyntax OrElse TypeOf node Is MemberAccessExpressionSyntax) Then Continue For
                If InsideOtherVerbClause(node) Then Continue For
                Dim nameToken As SyntaxToken? = NameTokenOf(node)
                If Not nameToken.HasValue Then Continue For
                Dim info As SymbolInfo = model.GetSymbolInfo(node)
                If info.Symbol Is Nothing OrElse info.CandidateSymbols.Length > 0 Then Continue For
                Dim target As ISymbol = info.Symbol
                If Not (TypeOf target Is IMethodSymbol OrElse TypeOf target Is IPropertySymbol OrElse TypeOf target Is IEventSymbol OrElse TypeOf target Is IFieldSymbol) Then Continue For
                Dim method As IMethodSymbol = TryCast(target, IMethodSymbol)
                If method IsNot Nothing AndAlso method.MethodKind = MethodKind.Constructor AndAlso method.IsImplicitlyDeclared Then
                    target = method.ContainingType
                End If
                Dim source As String = EnclosingSymbolResolver.RowSymbolAt(context, model, node)
                If source Is Nothing Then Continue For
                Dim targetDocId As String = context.DocIdOf(target)
                If targetDocId Is Nothing Then Continue For
                sink.Add(New ObservedEdge With {
                    .SourceDocCommentId = source,
                    .Verb = EdgeVerb.Calls,
                    .TargetDocCommentId = targetDocId,
                    .ViaDocCommentId = Nothing,
                    .Location = context.LocationOf(nameToken.Value)})
            Next
        Next
    End Sub

    ''' <summary>
    ''' The bare-name shape (003 FR-208): a simple name that is not the member name of a member access, not part of a qualified name, not an
    ''' attribute name and not inside another verb's clause, bound without candidates to a field, property, event or method (ordinary, Declare
    ''' or reduced extension) that is a row of this run (FR-209 - the stated limit: a bare external member writes nothing). One occurrence per
    ''' identifier, verb calls, at the identifier token, sourced from the enclosing row symbol (FR-016).
    ''' </summary>
    ''' <param name="context">The edge context.</param>
    ''' <param name="model">The tree's semantic model.</param>
    ''' <param name="simple">The simple name.</param>
    ''' <param name="sink">Where edges accumulate.</param>
    Private Shared Sub CollectBareName(context As EdgeContext, model As SemanticModel, simple As SimpleNameSyntax, sink As List(Of ObservedEdge))
        Dim parent As SyntaxNode = simple.Parent
        Dim access As MemberAccessExpressionSyntax = TryCast(parent, MemberAccessExpressionSyntax)
        If access IsNot Nothing AndAlso access.Name Is simple Then Return
        If TypeOf parent Is QualifiedNameSyntax OrElse TypeOf parent Is AttributeSyntax Then Return
        If InsideOtherVerbClause(simple) Then Return
        Dim info As SymbolInfo = model.GetSymbolInfo(simple)
        If info.Symbol Is Nothing OrElse info.CandidateSymbols.Length > 0 Then Return
        Dim target As ISymbol = info.Symbol
        Dim method As IMethodSymbol = TryCast(target, IMethodSymbol)
        Dim admitted As Boolean = TypeOf target Is IFieldSymbol OrElse TypeOf target Is IPropertySymbol OrElse TypeOf target Is IEventSymbol OrElse
            (method IsNot Nothing AndAlso (method.MethodKind = MethodKind.Ordinary OrElse method.MethodKind = MethodKind.DeclareMethod OrElse method.MethodKind = MethodKind.ReducedExtension))
        If Not admitted Then Return
        Dim targetDocId As String = context.DocIdOf(target)
        If Not context.IsRow(targetDocId) Then Return
        Dim source As String = EnclosingSymbolResolver.RowSymbolAt(context, model, simple)
        If source Is Nothing Then Return
        sink.Add(New ObservedEdge With {
            .SourceDocCommentId = source,
            .Verb = EdgeVerb.Calls,
            .TargetDocCommentId = targetDocId,
            .ViaDocCommentId = Nothing,
            .Location = context.LocationOf(simple.Identifier)})
    End Sub

    Private Shared Function NameTokenOf(node As SyntaxNode) As SyntaxToken?
        Dim invocation As InvocationExpressionSyntax = TryCast(node, InvocationExpressionSyntax)
        If invocation IsNot Nothing Then
            If invocation.Expression Is Nothing Then Return Nothing
            If TypeOf invocation.Expression Is MemberAccessExpressionSyntax OrElse TypeOf invocation.Expression Is SimpleNameSyntax Then
                Return PartResolver.NameTokenOf(invocation.Expression)
            End If
            Return Nothing
        End If
        Dim creation As ObjectCreationExpressionSyntax = TryCast(node, ObjectCreationExpressionSyntax)
        If creation IsNot Nothing Then Return PartResolver.NameTokenOf(creation.Type)
        Dim access As MemberAccessExpressionSyntax = TryCast(node, MemberAccessExpressionSyntax)
        If access IsNot Nothing AndAlso access.Name IsNot Nothing Then Return access.Name.Identifier
        Return Nothing
    End Function

    Private Shared Function InsideOtherVerbClause(node As SyntaxNode) As Boolean
        Dim ancestor As SyntaxNode = node.Parent
        While ancestor IsNot Nothing
            If TypeOf ancestor Is HandlesClauseSyntax OrElse TypeOf ancestor Is ImplementsClauseSyntax OrElse TypeOf ancestor Is ImplementsStatementSyntax OrElse TypeOf ancestor Is InheritsStatementSyntax OrElse TypeOf ancestor Is ImportsStatementSyntax Then Return True
            If TypeOf ancestor Is StatementSyntax Then Exit While
            ancestor = ancestor.Parent
        End While
        Return False
    End Function

End Class

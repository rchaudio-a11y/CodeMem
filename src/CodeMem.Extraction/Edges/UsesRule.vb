' File: UsesRule.vb
' Project: CodeMem.Extraction
' Description: The uses verb: each named type in the As clause of parameters, returns, fields, properties, events and local declarations (spec Q3).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports CodeMem.Core
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

''' <summary>
''' One edge per named type occurrence, generic arguments included; arrays and nullables name their element type. Error types and type parameters are skipped.
''' </summary>
Public Class UsesRule
    Implements EdgeRule

    ''' <inheritdoc/>
    Public Sub Collect(context As EdgeContext, sink As List(Of ObservedEdge)) Implements EdgeRule.Collect
        For Each tree As SyntaxTree In context.Trees
            Dim model As SemanticModel = context.ModelFor(tree)
            For Each node As SyntaxNode In tree.GetRoot().DescendantNodes()
                Dim parameter As ParameterSyntax = TryCast(node, ParameterSyntax)
                If parameter IsNot Nothing Then
                    Dim owner As ISymbol = model.GetDeclaredSymbol(parameter)
                    Emit(context, model, sink, If(owner Is Nothing, Nothing, EnclosingSymbolResolver.RowSymbolOf(context, owner.ContainingSymbol)), parameter.AsClause)
                    Continue For
                End If
                Dim declaringAsClause As AsClauseSyntax = DeclarationAsClause(node)
                If declaringAsClause IsNot Nothing Then
                    Emit(context, model, sink, EnclosingSymbolResolver.RowSymbolOf(context, model.GetDeclaredSymbol(node)), declaringAsClause)
                    Continue For
                End If
                Dim declarator As VariableDeclaratorSyntax = TryCast(node, VariableDeclaratorSyntax)
                If declarator IsNot Nothing AndAlso declarator.AsClause IsNot Nothing Then
                    If TypeOf declarator.Parent Is FieldDeclarationSyntax Then
                        For Each name As ModifiedIdentifierSyntax In declarator.Names
                            Emit(context, model, sink, EnclosingSymbolResolver.RowSymbolOf(context, model.GetDeclaredSymbol(name)), declarator.AsClause)
                        Next
                    ElseIf TypeOf declarator.Parent Is LocalDeclarationStatementSyntax Then
                        Emit(context, model, sink, EnclosingSymbolResolver.RowSymbolAt(context, model, declarator), declarator.AsClause)
                    End If
                End If
            Next
        Next
    End Sub

    Private Shared Function DeclarationAsClause(node As SyntaxNode) As AsClauseSyntax
        Dim method As MethodStatementSyntax = TryCast(node, MethodStatementSyntax)
        If method IsNot Nothing Then Return method.AsClause
        Dim [delegate] As DelegateStatementSyntax = TryCast(node, DelegateStatementSyntax)
        If [delegate] IsNot Nothing Then Return [delegate].AsClause
        Dim [property] As PropertyStatementSyntax = TryCast(node, PropertyStatementSyntax)
        If [property] IsNot Nothing Then Return [property].AsClause
        Dim [event] As EventStatementSyntax = TryCast(node, EventStatementSyntax)
        If [event] IsNot Nothing Then Return [event].AsClause
        Dim [declare] As DeclareStatementSyntax = TryCast(node, DeclareStatementSyntax)
        If [declare] IsNot Nothing Then Return [declare].AsClause
        Dim [operator] As OperatorStatementSyntax = TryCast(node, OperatorStatementSyntax)
        If [operator] IsNot Nothing Then Return [operator].AsClause
        Return Nothing
    End Function

    Private Shared Sub Emit(context As EdgeContext, model As SemanticModel, sink As List(Of ObservedEdge), source As String, asClause As AsClauseSyntax)
        If source Is Nothing OrElse asClause Is Nothing Then Return
        Dim typeSyntax As TypeSyntax = Nothing
        Dim simple As SimpleAsClauseSyntax = TryCast(asClause, SimpleAsClauseSyntax)
        If simple IsNot Nothing Then
            typeSyntax = simple.Type
        Else
            Dim asNew As AsNewClauseSyntax = TryCast(asClause, AsNewClauseSyntax)
            If asNew IsNot Nothing Then
                Dim creation As ObjectCreationExpressionSyntax = TryCast(asNew.NewExpression, ObjectCreationExpressionSyntax)
                If creation IsNot Nothing Then typeSyntax = creation.Type
                Dim arrayCreation As ArrayCreationExpressionSyntax = TryCast(asNew.NewExpression, ArrayCreationExpressionSyntax)
                If arrayCreation IsNot Nothing Then typeSyntax = arrayCreation.Type
            End If
        End If
        If typeSyntax Is Nothing Then Return
        Dim named As List(Of TypeSyntax) = New List(Of TypeSyntax)()
        CollectNamedTypes(typeSyntax, named)
        For Each occurrence As TypeSyntax In named
            Dim type As ITypeSymbol = model.GetTypeInfo(occurrence).Type
            If type Is Nothing Then type = TryCast(model.GetSymbolInfo(occurrence).Symbol, ITypeSymbol)
            If type Is Nothing OrElse type.TypeKind = TypeKind.Error OrElse type.TypeKind = TypeKind.TypeParameter Then Continue For
            Dim targetDocId As String = context.DocIdOf(type)
            If targetDocId Is Nothing Then Continue For
            sink.Add(New ObservedEdge With {
                .SourceDocCommentId = source,
                .Verb = EdgeVerb.Uses,
                .TargetDocCommentId = targetDocId,
                .ViaDocCommentId = Nothing,
                .Location = context.LocationOf(PartResolver.NameTokenOf(occurrence))})
        Next
    End Sub

    ''' <summary>
    ''' Flattens a type syntax into its named type occurrences (research R13).
    ''' </summary>
    ''' <param name="typeSyntax">The type syntax.</param>
    ''' <param name="named">Receives each named occurrence in source order.</param>
    Public Shared Sub CollectNamedTypes(typeSyntax As TypeSyntax, named As List(Of TypeSyntax))
        If typeSyntax Is Nothing Then Return
        Dim arrayType As ArrayTypeSyntax = TryCast(typeSyntax, ArrayTypeSyntax)
        If arrayType IsNot Nothing Then
            CollectNamedTypes(arrayType.ElementType, named)
            Return
        End If
        Dim nullableType As NullableTypeSyntax = TryCast(typeSyntax, NullableTypeSyntax)
        If nullableType IsNot Nothing Then
            CollectNamedTypes(nullableType.ElementType, named)
            Return
        End If
        Dim tuple As TupleTypeSyntax = TryCast(typeSyntax, TupleTypeSyntax)
        If tuple IsNot Nothing Then
            For Each element As TupleElementSyntax In tuple.Elements
                Dim typed As TypedTupleElementSyntax = TryCast(element, TypedTupleElementSyntax)
                If typed IsNot Nothing Then CollectNamedTypes(typed.Type, named)
                Dim namedElement As NamedTupleElementSyntax = TryCast(element, NamedTupleElementSyntax)
                If namedElement IsNot Nothing AndAlso namedElement.AsClause IsNot Nothing Then CollectNamedTypes(namedElement.AsClause.Type, named)
            Next
            Return
        End If
        Dim generic As GenericNameSyntax = TryCast(typeSyntax, GenericNameSyntax)
        If generic IsNot Nothing Then
            named.Add(generic)
            For Each argument As TypeSyntax In generic.TypeArgumentList.Arguments
                CollectNamedTypes(argument, named)
            Next
            Return
        End If
        Dim qualified As QualifiedNameSyntax = TryCast(typeSyntax, QualifiedNameSyntax)
        If qualified IsNot Nothing Then
            named.Add(qualified)
            Dim rightGeneric As GenericNameSyntax = TryCast(qualified.Right, GenericNameSyntax)
            If rightGeneric IsNot Nothing Then
                For Each argument As TypeSyntax In rightGeneric.TypeArgumentList.Arguments
                    CollectNamedTypes(argument, named)
                Next
            End If
            Return
        End If
        If TypeOf typeSyntax Is IdentifierNameSyntax OrElse TypeOf typeSyntax Is PredefinedTypeSyntax Then named.Add(typeSyntax)
    End Sub

End Class

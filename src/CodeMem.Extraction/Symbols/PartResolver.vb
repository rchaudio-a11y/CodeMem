' File: PartResolver.vb
' Project: CodeMem.Extraction
' Description: Block promotion of declaring references (research R2), namespace-part filtering, and the one door for every location in the map.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports CodeMem.Core
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

''' <summary>
''' Resolves what a declaring reference's part node is, which token names the symbol in it, and where anything is.
''' </summary>
Public Module PartResolver

    ''' <summary>
    ''' The part node of a declaring reference: the enclosing block when the reference is a block's begin statement, the
    ''' variable declarator for a field or WithEvents identifier, otherwise the reference node itself.
    ''' </summary>
    ''' <param name="reference">The declaring reference.</param>
    ''' <returns>The part node.</returns>
    Public Function PartNodeOf(reference As SyntaxReference) As SyntaxNode
        Return PartNodeOf(reference.GetSyntax())
    End Function

    ''' <summary>
    ''' The part node of a declaration node (see the reference overload).
    ''' </summary>
    ''' <param name="node">The declaration node.</param>
    ''' <returns>The part node.</returns>
    Public Function PartNodeOf(node As SyntaxNode) As SyntaxNode
        Dim parent As SyntaxNode = node.Parent
        If parent Is Nothing Then Return node
        If TypeOf node Is TypeStatementSyntax AndAlso TypeOf parent Is TypeBlockSyntax AndAlso DirectCast(parent, TypeBlockSyntax).BlockStatement.Span = node.Span Then Return parent
        If TypeOf node Is MethodBaseSyntax AndAlso TypeOf parent Is MethodBlockBaseSyntax AndAlso DirectCast(parent, MethodBlockBaseSyntax).BlockStatement.Span = node.Span Then Return parent
        If TypeOf node Is PropertyStatementSyntax AndAlso TypeOf parent Is PropertyBlockSyntax AndAlso DirectCast(parent, PropertyBlockSyntax).PropertyStatement.Span = node.Span Then Return parent
        If TypeOf node Is EventStatementSyntax AndAlso TypeOf parent Is EventBlockSyntax AndAlso DirectCast(parent, EventBlockSyntax).EventStatement.Span = node.Span Then Return parent
        If TypeOf node Is EnumStatementSyntax AndAlso TypeOf parent Is EnumBlockSyntax AndAlso DirectCast(parent, EnumBlockSyntax).EnumStatement.Span = node.Span Then Return parent
        If TypeOf node Is NamespaceStatementSyntax AndAlso TypeOf parent Is NamespaceBlockSyntax AndAlso DirectCast(parent, NamespaceBlockSyntax).NamespaceStatement.Span = node.Span Then Return parent
        If TypeOf node Is ModifiedIdentifierSyntax AndAlso TypeOf parent Is VariableDeclaratorSyntax Then Return parent
        Return node
    End Function

    ''' <summary>
    ''' True only when the reference is a Namespace block (or its begin statement); compilation-unit references of a root namespace are not parts.
    ''' </summary>
    ''' <param name="reference">The declaring reference.</param>
    ''' <returns>Whether the reference is a namespace part.</returns>
    Public Function IsNamespacePart(reference As SyntaxReference) As Boolean
        Dim node As SyntaxNode = reference.GetSyntax()
        If TypeOf node Is NamespaceBlockSyntax Then Return True
        Return TypeOf node Is NamespaceStatementSyntax AndAlso TypeOf node.Parent Is NamespaceBlockSyntax
    End Function

    ''' <summary>
    ''' The token that names the symbol within its part node: the identifier, the New keyword for a constructor, the operator token for an operator.
    ''' </summary>
    ''' <param name="partNode">The part node.</param>
    ''' <param name="symbolName">The symbol's name, to pick the right declarator or namespace segment.</param>
    ''' <returns>The naming token.</returns>
    Public Function IdentifierTokenOf(partNode As SyntaxNode, symbolName As String) As SyntaxToken
        If TypeOf partNode Is TypeBlockSyntax Then Return DirectCast(partNode, TypeBlockSyntax).BlockStatement.Identifier
        If TypeOf partNode Is EnumBlockSyntax Then Return DirectCast(partNode, EnumBlockSyntax).EnumStatement.Identifier
        If TypeOf partNode Is DelegateStatementSyntax Then Return DirectCast(partNode, DelegateStatementSyntax).Identifier
        If TypeOf partNode Is MethodBlockSyntax Then Return DirectCast(partNode, MethodBlockSyntax).SubOrFunctionStatement.Identifier
        If TypeOf partNode Is MethodStatementSyntax Then Return DirectCast(partNode, MethodStatementSyntax).Identifier
        If TypeOf partNode Is ConstructorBlockSyntax Then Return DirectCast(partNode, ConstructorBlockSyntax).SubNewStatement.NewKeyword
        If TypeOf partNode Is SubNewStatementSyntax Then Return DirectCast(partNode, SubNewStatementSyntax).NewKeyword
        If TypeOf partNode Is OperatorBlockSyntax Then Return DirectCast(partNode, OperatorBlockSyntax).OperatorStatement.OperatorToken
        If TypeOf partNode Is OperatorStatementSyntax Then Return DirectCast(partNode, OperatorStatementSyntax).OperatorToken
        If TypeOf partNode Is DeclareStatementSyntax Then Return DirectCast(partNode, DeclareStatementSyntax).Identifier
        If TypeOf partNode Is PropertyBlockSyntax Then Return DirectCast(partNode, PropertyBlockSyntax).PropertyStatement.Identifier
        If TypeOf partNode Is PropertyStatementSyntax Then Return DirectCast(partNode, PropertyStatementSyntax).Identifier
        If TypeOf partNode Is EventBlockSyntax Then Return DirectCast(partNode, EventBlockSyntax).EventStatement.Identifier
        If TypeOf partNode Is EventStatementSyntax Then Return DirectCast(partNode, EventStatementSyntax).Identifier
        If TypeOf partNode Is EnumMemberDeclarationSyntax Then Return DirectCast(partNode, EnumMemberDeclarationSyntax).Identifier
        If TypeOf partNode Is NamespaceBlockSyntax Then
            Dim last As SyntaxToken = Nothing
            For Each token As SyntaxToken In DirectCast(partNode, NamespaceBlockSyntax).NamespaceStatement.Name.DescendantTokens()
                If token.IsKind(Microsoft.CodeAnalysis.VisualBasic.SyntaxKind.IdentifierToken) Then
                    last = token
                    If String.Equals(token.ValueText, symbolName, StringComparison.OrdinalIgnoreCase) Then Return token
                End If
            Next
            Return last
        End If
        If TypeOf partNode Is VariableDeclaratorSyntax Then
            Dim declarator As VariableDeclaratorSyntax = DirectCast(partNode, VariableDeclaratorSyntax)
            For Each name As ModifiedIdentifierSyntax In declarator.Names
                If String.Equals(name.Identifier.ValueText, symbolName, StringComparison.OrdinalIgnoreCase) Then Return name.Identifier
            Next
            Return declarator.Names(0).Identifier
        End If
        Return partNode.GetFirstToken()
    End Function

    ''' <summary>
    ''' The token that names the target in a name, type or member-access expression: the rightmost identifier, or the keyword of a predefined type.
    ''' </summary>
    ''' <param name="node">A name, type or member-access node.</param>
    ''' <returns>The naming token; the node's last token when the shape is unknown.</returns>
    Public Function NameTokenOf(node As SyntaxNode) As SyntaxToken
        Dim qualified As QualifiedNameSyntax = TryCast(node, QualifiedNameSyntax)
        If qualified IsNot Nothing Then Return qualified.Right.Identifier
        Dim simple As SimpleNameSyntax = TryCast(node, SimpleNameSyntax)
        If simple IsNot Nothing Then Return simple.Identifier
        Dim predefined As PredefinedTypeSyntax = TryCast(node, PredefinedTypeSyntax)
        If predefined IsNot Nothing Then Return predefined.Keyword
        Dim access As MemberAccessExpressionSyntax = TryCast(node, MemberAccessExpressionSyntax)
        If access IsNot Nothing AndAlso access.Name IsNot Nothing Then Return access.Name.Identifier
        Dim nullable As NullableTypeSyntax = TryCast(node, NullableTypeSyntax)
        If nullable IsNot Nothing Then Return NameTokenOf(nullable.ElementType)
        Dim arrayType As ArrayTypeSyntax = TryCast(node, ArrayTypeSyntax)
        If arrayType IsNot Nothing Then Return NameTokenOf(arrayType.ElementType)
        Return node.GetLastToken()
    End Function

    ''' <summary>
    ''' Builds the map's location shape for a span in a tree: solution-relative path, 0-based offset and length, 1-based line and column.
    ''' </summary>
    ''' <param name="tree">The syntax tree.</param>
    ''' <param name="span">The span.</param>
    ''' <param name="basePath">The base directory for relative paths.</param>
    ''' <returns>The location.</returns>
    Public Function LocationOf(tree As SyntaxTree, span As TextSpan, basePath As String) As SourceLocation
        Dim lineSpan As FileLinePositionSpan = tree.GetLineSpan(span)
        Return New SourceLocation(SolutionPaths.Relative(basePath, tree.FilePath), span.Start, span.Length, lineSpan.StartLinePosition.Line + 1, lineSpan.StartLinePosition.Character + 1)
    End Function

    ''' <summary>
    ''' The location of a token (see the span overload).
    ''' </summary>
    ''' <param name="token">The token.</param>
    ''' <param name="basePath">The base directory for relative paths.</param>
    ''' <returns>The location.</returns>
    Public Function LocationOf(token As SyntaxToken, basePath As String) As SourceLocation
        Return LocationOf(token.SyntaxTree, token.Span, basePath)
    End Function

    ''' <summary>
    ''' The location of a node's span (see the span overload).
    ''' </summary>
    ''' <param name="node">The node.</param>
    ''' <param name="basePath">The base directory for relative paths.</param>
    ''' <returns>The location.</returns>
    Public Function LocationOf(node As SyntaxNode, basePath As String) As SourceLocation
        Return LocationOf(node.SyntaxTree, node.Span, basePath)
    End Function

End Module

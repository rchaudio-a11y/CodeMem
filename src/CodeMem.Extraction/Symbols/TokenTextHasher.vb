' File: TokenTextHasher.vb
' Project: CodeMem.Extraction
' Description: Token-text serialization and hashing of declaring parts (research R4, FR-013, FR-014).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.Text
Imports CodeMem.Core
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

''' <summary>
''' Trivia never enters a hash; the symbol's own identifier tokens (and every sibling declarator's) are excluded so a rename keeps the hash.
''' </summary>
Public Module TokenTextHasher

    ''' <summary>
    ''' The hash input of a part: each descendant token's text followed by LF, skipping excluded tokens.
    ''' </summary>
    ''' <param name="partNode">The part node.</param>
    ''' <param name="excludedTokens">Spans of tokens to omit.</param>
    ''' <returns>UTF-8 bytes.</returns>
    Public Function HashInput(partNode As SyntaxNode, excludedTokens As HashSet(Of TextSpan)) As Byte()
        Dim builder As StringBuilder = New StringBuilder()
        For Each token As SyntaxToken In partNode.DescendantTokens()
            If excludedTokens.Contains(token.Span) Then Continue For
            builder.Append(token.Text).Append(vbLf)
        Next
        Return Encoding.UTF8.GetBytes(builder.ToString())
    End Function

    ''' <summary>
    ''' The identifier tokens a part's hash must not see: the declared name (all segments of a namespace name; every declarator identifier of a field declaration).
    ''' </summary>
    ''' <param name="partNode">The part node.</param>
    ''' <returns>The spans to exclude.</returns>
    Public Function ExcludedIdentifiers(partNode As SyntaxNode) As HashSet(Of TextSpan)
        Dim excluded As HashSet(Of TextSpan) = New HashSet(Of TextSpan)()
        If TypeOf partNode Is TypeBlockSyntax Then
            excluded.Add(DirectCast(partNode, TypeBlockSyntax).BlockStatement.Identifier.Span)
        ElseIf TypeOf partNode Is EnumBlockSyntax Then
            excluded.Add(DirectCast(partNode, EnumBlockSyntax).EnumStatement.Identifier.Span)
        ElseIf TypeOf partNode Is DelegateStatementSyntax Then
            excluded.Add(DirectCast(partNode, DelegateStatementSyntax).Identifier.Span)
        ElseIf TypeOf partNode Is MethodBlockSyntax Then
            excluded.Add(DirectCast(partNode, MethodBlockSyntax).SubOrFunctionStatement.Identifier.Span)
        ElseIf TypeOf partNode Is MethodStatementSyntax Then
            excluded.Add(DirectCast(partNode, MethodStatementSyntax).Identifier.Span)
        ElseIf TypeOf partNode Is DeclareStatementSyntax Then
            excluded.Add(DirectCast(partNode, DeclareStatementSyntax).Identifier.Span)
        ElseIf TypeOf partNode Is PropertyBlockSyntax Then
            excluded.Add(DirectCast(partNode, PropertyBlockSyntax).PropertyStatement.Identifier.Span)
        ElseIf TypeOf partNode Is PropertyStatementSyntax Then
            excluded.Add(DirectCast(partNode, PropertyStatementSyntax).Identifier.Span)
        ElseIf TypeOf partNode Is EventBlockSyntax Then
            excluded.Add(DirectCast(partNode, EventBlockSyntax).EventStatement.Identifier.Span)
        ElseIf TypeOf partNode Is EventStatementSyntax Then
            excluded.Add(DirectCast(partNode, EventStatementSyntax).Identifier.Span)
        ElseIf TypeOf partNode Is EnumMemberDeclarationSyntax Then
            excluded.Add(DirectCast(partNode, EnumMemberDeclarationSyntax).Identifier.Span)
        ElseIf TypeOf partNode Is NamespaceBlockSyntax Then
            For Each token As SyntaxToken In DirectCast(partNode, NamespaceBlockSyntax).NamespaceStatement.Name.DescendantTokens()
                excluded.Add(token.Span)
            Next
        ElseIf TypeOf partNode Is VariableDeclaratorSyntax Then
            For Each name As ModifiedIdentifierSyntax In DirectCast(partNode, VariableDeclaratorSyntax).Names
                excluded.Add(name.Identifier.Span)
            Next
        End If
        Return excluded
    End Function

    ''' <summary>
    ''' The part hash: SHA-256 hex of the hash input.
    ''' </summary>
    ''' <param name="input">The hash input.</param>
    ''' <returns>64 lowercase hex characters.</returns>
    Public Function PartHash(input As Byte()) As String
        Return Sha256Hex.Compute(New Byte()() {input})
    End Function

    ''' <summary>
    ''' The body hash: SHA-256 hex over the parts' hash inputs in (path, offset) order, separated by NUL.
    ''' </summary>
    ''' <param name="inputs">The hash inputs in part order.</param>
    ''' <returns>64 lowercase hex characters.</returns>
    Public Function BodyHash(inputs As IEnumerable(Of Byte())) As String
        Dim chunks As List(Of Byte()) = New List(Of Byte())()
        Dim first As Boolean = True
        For Each input As Byte() In inputs
            If Not first Then chunks.Add(New Byte() {0})
            chunks.Add(input)
            first = False
        Next
        Return Sha256Hex.Compute(chunks)
    End Function

End Module

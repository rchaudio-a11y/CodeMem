' File: EnclosingSymbolResolver.vb
' Project: CodeMem.Extraction
' Description: Attributes an occurrence to the nearest enclosing symbol that has a row: accessor to property, lambda to method, initializer to field (FR-016).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

''' <summary>
''' The one attribution walk every occurrence-based rule uses.
''' </summary>
Public Module EnclosingSymbolResolver

    ''' <summary>
    ''' The doc-comment id of the row symbol enclosing a node, or Nothing when none encloses it.
    ''' </summary>
    ''' <param name="context">The edge context.</param>
    ''' <param name="model">The node's semantic model.</param>
    ''' <param name="node">The occurrence node.</param>
    ''' <returns>The enclosing row's doc-comment id or Nothing.</returns>
    Public Function RowSymbolAt(context As EdgeContext, model As SemanticModel, node As SyntaxNode) As String
        Dim ancestor As SyntaxNode = node.Parent
        While ancestor IsNot Nothing
            Dim declarator As VariableDeclaratorSyntax = TryCast(ancestor, VariableDeclaratorSyntax)
            If declarator IsNot Nothing AndAlso TypeOf declarator.Parent Is FieldDeclarationSyntax AndAlso declarator.Names.Count > 0 Then
                Dim field As ISymbol = model.GetDeclaredSymbol(declarator.Names(0))
                Dim docId As String = RowSymbolOf(context, field)
                If docId IsNot Nothing Then Return docId
            End If
            If TypeOf ancestor Is MethodBlockBaseSyntax OrElse TypeOf ancestor Is TypeBlockSyntax Then Exit While
            ancestor = ancestor.Parent
        End While
        Return RowSymbolOf(context, model.GetEnclosingSymbol(node.SpanStart))
    End Function

    ''' <summary>
    ''' Walks from a symbol through accessor-to-property and containing-symbol links until a symbol with a row is found.
    ''' </summary>
    ''' <param name="context">The edge context.</param>
    ''' <param name="symbol">The starting symbol.</param>
    ''' <returns>The row's doc-comment id or Nothing.</returns>
    Public Function RowSymbolOf(context As EdgeContext, symbol As ISymbol) As String
        Dim current As ISymbol = symbol
        While current IsNot Nothing
            Dim method As IMethodSymbol = TryCast(current, IMethodSymbol)
            If method IsNot Nothing AndAlso method.AssociatedSymbol IsNot Nothing Then
                current = method.AssociatedSymbol
                Continue While
            End If
            Dim docId As String = context.DocIdOf(current)
            If context.IsRow(docId) Then Return docId
            current = current.ContainingSymbol
        End While
        Return Nothing
    End Function

End Module

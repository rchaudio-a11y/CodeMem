' File: SymbolWalker.vb
' Project: CodeMem.Extraction
' Description: Walks a compilation's declared symbols, applying the kind and implicit-declaration filters, and merges namespaces across compilations (FR-007, FR-008, FR-010).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports CodeMem.Core
Imports Microsoft.CodeAnalysis

''' <summary>
''' Produces the staged symbols of one compilation. Accessors, lambdas, locals, parameters and type parameters are never rows.
''' </summary>
Public Class SymbolWalker

    Private ReadOnly _projectDocId As String
    Private ReadOnly _basePath As String
    Private ReadOnly _allowedTrees As HashSet(Of SyntaxTree)
    Private ReadOnly _result As List(Of ObservedSymbol) = New List(Of ObservedSymbol)()

    Private Sub New(projectDocId As String, basePath As String, allowedTrees As HashSet(Of SyntaxTree))
        _projectDocId = projectDocId
        _basePath = basePath
        _allowedTrees = allowedTrees
    End Sub

    ''' <summary>
    ''' Walks from the assembly's global namespace and returns every row symbol declared in an allowed tree.
    ''' </summary>
    ''' <param name="compilation">The compilation.</param>
    ''' <param name="projectDocId">The declaring project's doc-comment id, recorded on every non-namespace row.</param>
    ''' <param name="basePath">The base directory for relative paths.</param>
    ''' <param name="allowedTrees">The compiled source trees (documents outside obj/); generated and embedded trees are not declaring documents.</param>
    ''' <returns>The staged symbols.</returns>
    Public Shared Function Walk(compilation As Compilation, projectDocId As String, basePath As String, allowedTrees As HashSet(Of SyntaxTree)) As List(Of ObservedSymbol)
        Dim walker As SymbolWalker = New SymbolWalker(projectDocId, basePath, allowedTrees)
        walker.VisitNamespace(compilation.Assembly.GlobalNamespace)
        Return walker._result
    End Function

    ''' <summary>
    ''' Merges namespace symbols observed by several compilations into one per doc-comment id: parts concatenate, primary is the first
    ''' in (path, offset) order, body hash is recomputed. No other kind is merged.
    ''' </summary>
    ''' <param name="observed">Symbols from every compilation.</param>
    ''' <returns>The merged list, first-appearance order.</returns>
    Public Shared Function MergeNamespaces(observed As IEnumerable(Of ObservedSymbol)) As List(Of ObservedSymbol)
        Dim result As List(Of ObservedSymbol) = New List(Of ObservedSymbol)()
        Dim namespaces As Dictionary(Of String, ObservedSymbol) = New Dictionary(Of String, ObservedSymbol)(StringComparer.Ordinal)
        For Each symbol As ObservedSymbol In observed
            If symbol.Kind <> CodeMem.Core.SymbolKind.Namespace_ Then
                result.Add(symbol)
                Continue For
            End If
            Dim existing As ObservedSymbol = Nothing
            If namespaces.TryGetValue(symbol.DocCommentId, existing) Then
                existing.Parts.AddRange(symbol.Parts)
                SortParts(existing.Parts)
                existing.Primary = existing.Parts(0).IdentifierLocation
                existing.BodyHash = TokenTextHasher.BodyHash(existing.Parts.ConvertAll(Function(p As ObservedPart) p.HashInput))
            Else
                namespaces(symbol.DocCommentId) = symbol
                result.Add(symbol)
            End If
        Next
        Return result
    End Function

    ''' <summary>
    ''' Maps a Roslyn symbol to a row kind, or Nothing when the symbol is never a row.
    ''' </summary>
    ''' <param name="symbol">The symbol.</param>
    ''' <returns>The kind or Nothing.</returns>
    Public Shared Function KindOf(symbol As ISymbol) As CodeMem.Core.SymbolKind?
        Dim named As INamedTypeSymbol = TryCast(symbol, INamedTypeSymbol)
        If named IsNot Nothing Then
            Select Case named.TypeKind
                Case TypeKind.Class : Return CodeMem.Core.SymbolKind.Class_
                Case TypeKind.Module : Return CodeMem.Core.SymbolKind.Module_
                Case TypeKind.Structure : Return CodeMem.Core.SymbolKind.Structure_
                Case TypeKind.Interface : Return CodeMem.Core.SymbolKind.Interface_
                Case TypeKind.Enum : Return CodeMem.Core.SymbolKind.Enum_
                Case TypeKind.Delegate : Return CodeMem.Core.SymbolKind.Delegate_
                Case Else : Return Nothing
            End Select
        End If
        If TypeOf symbol Is INamespaceSymbol Then Return CodeMem.Core.SymbolKind.Namespace_
        Dim method As IMethodSymbol = TryCast(symbol, IMethodSymbol)
        If method IsNot Nothing Then
            If method.AssociatedSymbol IsNot Nothing Then Return Nothing
            Select Case method.MethodKind
                Case MethodKind.Constructor, MethodKind.StaticConstructor : Return CodeMem.Core.SymbolKind.Constructor
                Case MethodKind.Ordinary, MethodKind.UserDefinedOperator, MethodKind.Conversion, MethodKind.DeclareMethod : Return CodeMem.Core.SymbolKind.Method
                Case Else : Return Nothing
            End Select
        End If
        If TypeOf symbol Is IPropertySymbol Then Return CodeMem.Core.SymbolKind.Property_
        Dim field As IFieldSymbol = TryCast(symbol, IFieldSymbol)
        If field IsNot Nothing Then
            If field.ContainingType IsNot Nothing AndAlso field.ContainingType.TypeKind = TypeKind.Enum Then Return CodeMem.Core.SymbolKind.EnumMember
            Return CodeMem.Core.SymbolKind.Field
        End If
        If TypeOf symbol Is IEventSymbol Then Return CodeMem.Core.SymbolKind.Event_
        Return Nothing
    End Function

    ''' <summary>
    ''' The doc-comment id of a symbol's container, or Nothing when the container is the global namespace or absent.
    ''' </summary>
    ''' <param name="symbol">The symbol.</param>
    ''' <returns>The container's doc-comment id or Nothing.</returns>
    Public Shared Function ContainerDocIdOf(symbol As ISymbol) As String
        Dim container As ISymbol = symbol.ContainingSymbol
        If container Is Nothing Then Return Nothing
        Dim ns As INamespaceSymbol = TryCast(container, INamespaceSymbol)
        If ns IsNot Nothing AndAlso ns.IsGlobalNamespace Then Return Nothing
        Return container.OriginalDefinition.GetDocumentationCommentId()
    End Function

    ''' <summary>
    ''' Sorts parts by (path ordinal, start offset).
    ''' </summary>
    ''' <param name="parts">The parts to sort in place.</param>
    Public Shared Sub SortParts(parts As List(Of ObservedPart))
        parts.Sort(Function(a As ObservedPart, b As ObservedPart)
                       Dim byPath As Integer = SolutionPaths.Compare(a.Location.Path, b.Location.Path)
                       If byPath <> 0 Then Return byPath
                       Return a.Location.StartOffset.CompareTo(b.Location.StartOffset)
                   End Function)
    End Sub

    Private Sub VisitNamespace(ns As INamespaceSymbol)
        If Not ns.IsGlobalNamespace Then TryAdd(ns)
        For Each member As ISymbol In ns.GetMembers()
            Dim child As INamespaceSymbol = TryCast(member, INamespaceSymbol)
            If child IsNot Nothing Then
                VisitNamespace(child)
                Continue For
            End If
            Dim type As INamedTypeSymbol = TryCast(member, INamedTypeSymbol)
            If type IsNot Nothing Then VisitType(type)
        Next
    End Sub

    Private Sub VisitType(type As INamedTypeSymbol)
        If Not TryAdd(type) Then Return
        For Each member As ISymbol In type.GetMembers()
            Dim nested As INamedTypeSymbol = TryCast(member, INamedTypeSymbol)
            If nested IsNot Nothing Then
                VisitType(nested)
            Else
                TryAdd(member)
            End If
        Next
    End Sub

    Private Function TryAdd(symbol As ISymbol) As Boolean
        If symbol.IsImplicitlyDeclared Then Return False
        Dim kind As CodeMem.Core.SymbolKind? = KindOf(symbol)
        If Not kind.HasValue Then Return False
        Dim docId As String = symbol.GetDocumentationCommentId()
        If String.IsNullOrEmpty(docId) Then Return False
        Dim name As String = If(kind.Value = CodeMem.Core.SymbolKind.Constructor, "New", symbol.Name)
        Dim parts As List(Of ObservedPart) = New List(Of ObservedPart)()
        For Each reference As SyntaxReference In symbol.DeclaringSyntaxReferences
            If Not _allowedTrees.Contains(reference.SyntaxTree) Then Continue For
            If kind.Value = CodeMem.Core.SymbolKind.Namespace_ AndAlso Not PartResolver.IsNamespacePart(reference) Then Continue For
            Dim partNode As SyntaxNode = PartResolver.PartNodeOf(reference)
            Dim input As Byte() = TokenTextHasher.HashInput(partNode, TokenTextHasher.ExcludedIdentifiers(partNode))
            Dim identifier As SyntaxToken = PartResolver.IdentifierTokenOf(partNode, name)
            parts.Add(New ObservedPart With {
                .Location = PartResolver.LocationOf(reference.SyntaxTree, partNode.Span, _basePath),
                .HashInput = input,
                .PartHash = TokenTextHasher.PartHash(input),
                .IdentifierLocation = PartResolver.LocationOf(reference.SyntaxTree, identifier.Span, _basePath)})
        Next
        If parts.Count = 0 Then Return False
        SortParts(parts)
        _result.Add(New ObservedSymbol With {
            .DocCommentId = docId,
            .Kind = kind.Value,
            .Name = name,
            .ContainerDocCommentId = ContainerDocIdOf(symbol),
            .ProjectDocCommentId = If(kind.Value = CodeMem.Core.SymbolKind.Namespace_, Nothing, _projectDocId),
            .Primary = parts(0).IdentifierLocation,
            .BodyHash = TokenTextHasher.BodyHash(parts.ConvertAll(Function(p As ObservedPart) p.HashInput)),
            .Parts = parts})
        Return True
    End Function

End Class

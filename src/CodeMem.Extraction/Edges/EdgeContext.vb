' File: EdgeContext.vb
' Project: CodeMem.Extraction
' Description: What an edge rule sees: the compilation, a semantic model per tree, the staged symbols and the row lookup by doc id.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports CodeMem.Core
Imports Microsoft.CodeAnalysis

''' <summary>
''' Per-compilation context for the tree-based rules; for the merged part_of pass the compilation is Nothing and only Symbols matters.
''' </summary>
Public Class EdgeContext

    Private ReadOnly _models As Dictionary(Of SyntaxTree, SemanticModel) = New Dictionary(Of SyntaxTree, SemanticModel)()

    ''' <summary>The compiled project, or Nothing for the merged pass.</summary>
    Public ReadOnly Property Project As CompiledProject

    ''' <summary>The loaded solution.</summary>
    Public ReadOnly Property Solution As Solution

    ''' <summary>The base directory for relative paths.</summary>
    Public ReadOnly Property BasePath As String

    ''' <summary>The source trees of the compilation (documents outside obj/).</summary>
    Public ReadOnly Property Trees As HashSet(Of SyntaxTree)

    ''' <summary>The staged symbols the rule may source from.</summary>
    Public ReadOnly Property Symbols As List(Of ObservedSymbol)

    ''' <summary>Every doc-comment id that has a row in this run, across all compilations.</summary>
    Public ReadOnly Property RowDocIds As HashSet(Of String)

    ''' <summary>
    ''' Creates the context.
    ''' </summary>
    ''' <param name="project">The compiled project or Nothing.</param>
    ''' <param name="solution">The loaded solution.</param>
    ''' <param name="basePath">The base directory.</param>
    ''' <param name="trees">The source trees.</param>
    ''' <param name="symbols">The staged symbols.</param>
    ''' <param name="rowDocIds">All row doc ids.</param>
    Public Sub New(project As CompiledProject, solution As Solution, basePath As String, trees As HashSet(Of SyntaxTree), symbols As List(Of ObservedSymbol), rowDocIds As HashSet(Of String))
        Me.Project = project
        Me.Solution = solution
        Me.BasePath = basePath
        Me.Trees = trees
        Me.Symbols = symbols
        Me.RowDocIds = rowDocIds
    End Sub

    ''' <summary>
    ''' The semantic model of a tree, cached.
    ''' </summary>
    ''' <param name="tree">A tree of this compilation.</param>
    ''' <returns>Its semantic model.</returns>
    Public Function ModelFor(tree As SyntaxTree) As SemanticModel
        Dim model As SemanticModel = Nothing
        If Not _models.TryGetValue(tree, model) Then
            model = Project.Compilation.GetSemanticModel(tree)
            _models(tree) = model
        End If
        Return model
    End Function

    ''' <summary>
    ''' Whether a doc-comment id has a row this run.
    ''' </summary>
    ''' <param name="docId">The id.</param>
    ''' <returns>True when a row exists.</returns>
    Public Function IsRow(docId As String) As Boolean
        Return docId IsNot Nothing AndAlso RowDocIds.Contains(docId)
    End Function

    ''' <summary>
    ''' The doc-comment id an edge names a symbol by: the original definition (reduced extension methods use their definition) (research R13).
    ''' </summary>
    ''' <param name="symbol">The bound symbol.</param>
    ''' <returns>The doc-comment id, or Nothing when the symbol has none.</returns>
    Public Function DocIdOf(symbol As ISymbol) As String
        If symbol Is Nothing Then Return Nothing
        Dim method As IMethodSymbol = TryCast(symbol, IMethodSymbol)
        If method IsNot Nothing AndAlso method.ReducedFrom IsNot Nothing Then symbol = method.ReducedFrom
        Dim id As String = symbol.OriginalDefinition.GetDocumentationCommentId()
        If String.IsNullOrEmpty(id) Then Return Nothing
        Return id
    End Function

    ''' <summary>
    ''' The location of a token in this run's shape.
    ''' </summary>
    ''' <param name="token">The token.</param>
    ''' <returns>The location.</returns>
    Public Function LocationOf(token As SyntaxToken) As SourceLocation
        Return PartResolver.LocationOf(token, BasePath)
    End Function

End Class

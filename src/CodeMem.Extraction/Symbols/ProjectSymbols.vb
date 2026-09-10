' File: ProjectSymbols.vb
' Project: CodeMem.Extraction
' Description: The synthetic project row per project (FR-007, research R14).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.IO
Imports CodeMem.Core
Imports Microsoft.CodeAnalysis

''' <summary>
''' One symbol of kind project per project: id Project:name, located at the project file, no parts, hash of the empty input.
''' </summary>
Public Module ProjectSymbols

    ''' <summary>
    ''' The synthetic doc-comment id of a project.
    ''' </summary>
    ''' <param name="project">The project.</param>
    ''' <returns>Project: followed by the file name without extension.</returns>
    Public Function DocIdOf(project As Project) As String
        Return "Project:" & Path.GetFileNameWithoutExtension(project.FilePath)
    End Function

    ''' <summary>
    ''' Creates the project symbol.
    ''' </summary>
    ''' <param name="project">The project.</param>
    ''' <param name="basePath">The base directory for relative paths.</param>
    ''' <returns>The staged symbol.</returns>
    Public Function Create(project As Project, basePath As String) As ObservedSymbol
        Return New ObservedSymbol With {
            .DocCommentId = DocIdOf(project),
            .Kind = CodeMem.Core.SymbolKind.Project,
            .Name = Path.GetFileNameWithoutExtension(project.FilePath),
            .ContainerDocCommentId = Nothing,
            .ProjectDocCommentId = Nothing,
            .Primary = New SourceLocation(SolutionPaths.Relative(basePath, project.FilePath), 0, 0, 1, 1),
            .BodyHash = TokenTextHasher.BodyHash(New List(Of Byte())()),
            .Parts = New List(Of ObservedPart)()}
    End Function

End Module

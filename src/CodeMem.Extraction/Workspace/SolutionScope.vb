' File: SolutionScope.vb
' Project: CodeMem.Extraction
' Description: The one door for rule 1 (fixpack 003, FR-201): the scope root of a run and whether a declaring file lies under it.
' Author: RCH Automation LLC
' Created: 2026-09-13
'
' 2026-09-15 (feature 004, COR1, research R59): ContainsDirectory added - equality or prefix over a directory normalised as Root is. Additive:
' the extractor never calls it; Contains keeps its file semantics.

Imports System.IO

''' <summary>
''' The scope root is the repository working directory when the run writes one to solutions.repo_root, otherwise the solution's base
''' directory. A file is in scope when its resolved full path (relative segments resolved, separators normalised to the platform's) starts
''' with the root, compared case-insensitively (STOP 1 ruling, 2026-09-13). Consulted by CompiledInputs.SourceTrees for declaring documents
''' and by ExtractionRun for project files; nothing else decides scope (Article XII).
''' </summary>
Public Class SolutionScope

    ''' <summary>The normalised root, with one trailing directory separator.</summary>
    Public ReadOnly Property Root As String

    ''' <summary>True when the root is the repository working directory; False when it is the solution's base directory.</summary>
    Public ReadOnly Property IsRepository As Boolean

    Private Sub New(root As String, isRepository As Boolean)
        Me.Root = root
        Me.IsRepository = isRepository
    End Sub

    ''' <summary>
    ''' Resolves the scope root of a run.
    ''' </summary>
    ''' <param name="repoRoot">The repository working directory the run stamps (GitFacts.RepoRoot), or Nothing or empty when there is no repository or no commit.</param>
    ''' <param name="basePath">The solution's base directory.</param>
    ''' <returns>The scope.</returns>
    Public Shared Function Resolve(repoRoot As String, basePath As String) As SolutionScope
        Dim isRepository As Boolean = Not String.IsNullOrEmpty(repoRoot)
        Return New SolutionScope(Normalize(If(isRepository, repoRoot, basePath), True), isRepository)
    End Function

    ''' <summary>
    ''' Whether a declaring file lies under the scope root.
    ''' </summary>
    ''' <param name="fullPath">The file's path; resolved to a full path here.</param>
    ''' <returns>True when the resolved, separator-normalised path starts with the root (case-insensitive).</returns>
    Public Function Contains(fullPath As String) As Boolean
        Return Normalize(fullPath, False).StartsWith(Root, StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>
    ''' Whether a directory is the scope root or lies under it (feature 004): the argument is normalised as <see cref="Root"/> is - full path,
    ''' platform separators, one trailing separator - and compared case-insensitively for equality or prefix.
    ''' </summary>
    ''' <param name="directory">The directory; resolved to a full path here, never checked for existence.</param>
    ''' <returns>True when the directory is the root or under it.</returns>
    Public Function ContainsDirectory(directory As String) As Boolean
        Dim normalized As String = Normalize(directory, True)
        Return String.Equals(normalized, Root, StringComparison.OrdinalIgnoreCase) OrElse normalized.StartsWith(Root, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Shared Function Normalize(text As String, asDirectory As Boolean) As String
        Dim resolved As String = IO.Path.GetFullPath(text).Replace(IO.Path.AltDirectorySeparatorChar, IO.Path.DirectorySeparatorChar)
        If asDirectory AndAlso Not resolved.EndsWith(IO.Path.DirectorySeparatorChar) Then resolved &= IO.Path.DirectorySeparatorChar
        Return resolved
    End Function

End Class

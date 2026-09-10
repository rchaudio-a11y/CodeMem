' File: CompiledInputs.vb
' Project: CodeMem.Extraction
' Description: THE enumeration of compiled inputs: documents outside obj/, project files, the solution file (FR-005, Article XII, research R3).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.IO
Imports Microsoft.CodeAnalysis

''' <summary>
''' One list that feeds both the source digest and the dirty flag, and one rule that decides which trees declare symbols.
''' </summary>
Public Module CompiledInputs

    ''' <summary>
    ''' Enumerates every compiled source document not under a project's obj/ directory, every project file and the solution file,
    ''' deduplicated by full path and sorted ordinally by relative path.
    ''' </summary>
    ''' <param name="solution">The loaded solution.</param>
    ''' <param name="basePath">The base directory for relative paths.</param>
    ''' <returns>The inputs in digest order.</returns>
    Public Function Enumerate(solution As Solution, basePath As String) As List(Of CompiledInput)
        Dim seen As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Dim result As List(Of CompiledInput) = New List(Of CompiledInput)()
        For Each project As Project In solution.Projects
            For Each document As Document In project.Documents
                If Not IsSourceDocument(project, document) Then Continue For
                Dim full As String = Path.GetFullPath(document.FilePath)
                If Not seen.Add(full) Then Continue For
                result.Add(New CompiledInput With {
                    .FullPath = full,
                    .RelativePath = SolutionPaths.Relative(basePath, full),
                    .Text = Normalize(document.GetTextAsync().GetAwaiter().GetResult().ToString())})
            Next
            Dim projectFull As String = Path.GetFullPath(project.FilePath)
            If seen.Add(projectFull) Then
                result.Add(New CompiledInput With {
                    .FullPath = projectFull,
                    .RelativePath = SolutionPaths.Relative(basePath, projectFull),
                    .Text = Normalize(File.ReadAllText(projectFull))})
            End If
        Next
        If Not String.IsNullOrEmpty(solution.FilePath) Then
            Dim solutionFull As String = Path.GetFullPath(solution.FilePath)
            If seen.Add(solutionFull) Then
                result.Add(New CompiledInput With {
                    .FullPath = solutionFull,
                    .RelativePath = SolutionPaths.Relative(basePath, solutionFull),
                    .Text = Normalize(File.ReadAllText(solutionFull))})
            End If
        End If
        result.Sort(Function(a As CompiledInput, b As CompiledInput) SolutionPaths.Compare(a.RelativePath, b.RelativePath))
        Return result
    End Function

    ''' <summary>
    ''' The syntax trees of a compilation that belong to source documents (the same obj/ rule as <see cref="Enumerate"/>).
    ''' Generated and embedded trees are excluded and therefore declare nothing.
    ''' </summary>
    ''' <param name="project">The project.</param>
    ''' <param name="compilation">Its compilation.</param>
    ''' <returns>The declaring trees.</returns>
    Public Function SourceTrees(project As Project, compilation As Compilation) As HashSet(Of SyntaxTree)
        Dim paths As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each document As Document In project.Documents
            If IsSourceDocument(project, document) Then paths.Add(Path.GetFullPath(document.FilePath))
        Next
        Dim trees As HashSet(Of SyntaxTree) = New HashSet(Of SyntaxTree)()
        For Each tree As SyntaxTree In compilation.SyntaxTrees
            If Not String.IsNullOrEmpty(tree.FilePath) AndAlso paths.Contains(Path.GetFullPath(tree.FilePath)) Then trees.Add(tree)
        Next
        Return trees
    End Function

    Private Function IsSourceDocument(project As Project, document As Document) As Boolean
        If String.IsNullOrEmpty(document.FilePath) Then Return False
        Dim projectDirectory As String = Path.GetDirectoryName(Path.GetFullPath(project.FilePath))
        Dim intermediate As String = Path.Combine(projectDirectory, "obj") & Path.DirectorySeparatorChar
        Return Not Path.GetFullPath(document.FilePath).StartsWith(intermediate, StringComparison.OrdinalIgnoreCase)
    End Function

    Private Function Normalize(text As String) As String
        Dim normalized As String = text.Replace(vbCrLf, vbLf)
        If normalized.Length > 0 AndAlso normalized(0) = ChrW(&HFEFF) Then
            normalized = normalized.Substring(1)
        End If
        Return normalized
    End Function

End Module

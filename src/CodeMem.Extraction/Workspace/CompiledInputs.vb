' File: CompiledInputs.vb
' Project: CodeMem.Extraction
' Description: THE enumeration of compiled inputs: documents outside obj/, project files, the solution file, the four well-known build files walked upward (FR-005, FR-107, Article XII, research R3, R28).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-10 (fixpack 002, F4 cheap half): BuildFiles added and folded into Enumerate (research R28).
' 2026-09-13 (fixpack 003, rule 1): SourceTrees takes the run's SolutionScope and admits only in-scope documents (FR-202); Enumerate is
' unchanged, so the digest still covers every compiled input (FR-206).

Imports System.IO
Imports Microsoft.CodeAnalysis

''' <summary>
''' One list that feeds both the source digest and the dirty flag, and one rule that decides which trees declare symbols.
''' </summary>
Public Module CompiledInputs

    Private ReadOnly BuildFileNames As String() = New String() {"Directory.Build.props", "Directory.Build.targets", "Directory.Packages.props", "global.json"}

    ''' <summary>
    ''' Enumerates every compiled source document not under a project's obj/ directory, every project file, the solution file and the
    ''' build files of <see cref="BuildFiles"/> (fixpack 002, FR-107), deduplicated by full path and sorted ordinally by relative path.
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
        For Each buildFile As String In BuildFiles(basePath)
            Dim buildFull As String = Path.GetFullPath(buildFile)
            If seen.Add(buildFull) Then
                result.Add(New CompiledInput With {
                    .FullPath = buildFull,
                    .RelativePath = SolutionPaths.Relative(basePath, buildFull),
                    .Text = Normalize(File.ReadAllText(buildFull))})   ' an unreadable build file throws: exit 1, never a silently shorter digest
            End If
        Next
        result.Sort(Function(a As CompiledInput, b As CompiledInput) SolutionPaths.Compare(a.RelativePath, b.RelativePath))
        Return result
    End Function

    ''' <summary>
    ''' The four well-known build files found in the base directory and in each ancestor up to the root of its path (FR-107; research R28):
    ''' Directory.Build.props, Directory.Build.targets, Directory.Packages.props and global.json. The selection rule (FR-108) is exactly
    ''' this: one of those four names, existing at one of those levels; files they import in turn are not inputs (F4 full, out of scope).
    ''' The walk stops when <see cref="DirectoryInfo.Parent"/> is Nothing (drive root or the top of a UNC path). File lookup follows the
    ''' file system's own case rules.
    ''' </summary>
    ''' <param name="basePath">The solution's base directory.</param>
    ''' <returns>Full paths, nearest level first, in the fixed name order at each level.</returns>
    Public Function BuildFiles(basePath As String) As List(Of String)
        Dim result As List(Of String) = New List(Of String)()
        Dim dir As DirectoryInfo = New DirectoryInfo(basePath)
        While dir IsNot Nothing
            For Each name As String In BuildFileNames
                Dim candidate As String = Path.Combine(dir.FullName, name)
                If File.Exists(candidate) Then result.Add(candidate)
            Next
            dir = dir.Parent
        End While
        Return result
    End Function

    ''' <summary>
    ''' The syntax trees of a compilation that declare symbols: source documents (the same obj/ rule as <see cref="Enumerate"/>) that lie
    ''' under the run's scope root (fixpack 003, FR-202). Generated and embedded trees, and documents outside the scope root, declare nothing
    ''' and source no edge.
    ''' </summary>
    ''' <param name="project">The project.</param>
    ''' <param name="compilation">Its compilation.</param>
    ''' <param name="scope">The run's scope root (<see cref="SolutionScope"/>).</param>
    ''' <returns>The declaring trees.</returns>
    Public Function SourceTrees(project As Project, compilation As Compilation, scope As SolutionScope) As HashSet(Of SyntaxTree)
        Dim paths As HashSet(Of String) = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each document As Document In project.Documents
            If IsSourceDocument(project, document) AndAlso scope.Contains(document.FilePath) Then paths.Add(Path.GetFullPath(document.FilePath))
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

' File: SolutionLoader.vb
' Project: CodeMem.Extraction
' Description: Opens a .sln, a .slnx (CodeMem's own parse, one OpenProjectAsync per project) or a .vbproj through MSBuildWorkspace under the requested configuration and compiles every project (FR-001, FR-004; 005 FR-421, FR-424).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-17 (feature 005, T031): the three-way extension door sits before MSBuildWorkspace.Create - .sln through OpenSolutionAsync as
' before, .slnx through SlnxReader then OpenProjectAsync per project in document order (a path the workspace already holds is not
' opened again), .vbproj through OpenProjectAsync as before, anything else refused by name before a workspace exists (R61, route b).

Imports System.IO
Imports System.Xml.Linq
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.MSBuild

''' <summary>
''' Owns the workspace for one run. What is compiled is what is stamped: Configuration and TargetFramework are passed as global properties.
''' </summary>
Public Class SolutionLoader
    Implements IDisposable

    Private ReadOnly _workspace As MSBuildWorkspace

    ''' <summary>The loaded solution snapshot.</summary>
    Public ReadOnly Property Solution As Solution

    ''' <summary>The absolute path of the .sln, .slnx or .vbproj that was opened.</summary>
    Public ReadOnly Property OpenedPath As String

    Private Sub New(workspace As MSBuildWorkspace, solution As Solution, openedPath As String)
        _workspace = workspace
        Me.Solution = solution
        Me.OpenedPath = openedPath
    End Sub

    ''' <summary>
    ''' Opens a .sln, a .slnx or a .vbproj. Workspace diagnostics of kind Failure become <see cref="WorkspaceLoadException"/>.
    ''' </summary>
    ''' <param name="path">The solution or project path.</param>
    ''' <param name="configuration">The build configuration.</param>
    ''' <param name="framework">The target framework, or Nothing for the project's own.</param>
    ''' <returns>The loader holding the open workspace.</returns>
    Public Shared Function Open(path As String, configuration As String, framework As String) As SolutionLoader
        Dim fullPath As String = IO.Path.GetFullPath(path)
        Dim extension As String = IO.Path.GetExtension(fullPath)
        Dim isSolution As Boolean = String.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase)
        Dim isSlnx As Boolean = String.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase)
        Dim isProject As Boolean = String.Equals(extension, ".vbproj", StringComparison.OrdinalIgnoreCase)
        If Not (isSolution OrElse isSlnx OrElse isProject) Then
            Throw WorkspaceLoadException.Refusal("unsupported solution file: " & fullPath & " (expected .sln, .slnx or .vbproj)")
        End If
        Dim projectPaths As List(Of String) = Nothing
        If isSlnx Then projectPaths = SlnxReader.ReadProjectPaths(fullPath)
        Dim properties As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
        properties("Configuration") = configuration
        If Not String.IsNullOrEmpty(framework) Then
            properties("TargetFramework") = framework
        End If
        Dim workspace As MSBuildWorkspace = MSBuildWorkspace.Create(properties)
        Dim failures As List(Of String) = New List(Of String)()
        AddHandler workspace.WorkspaceFailed,
            Sub(sender As Object, e As WorkspaceDiagnosticEventArgs)
                If e.Diagnostic.Kind = WorkspaceDiagnosticKind.Failure Then
                    failures.Add(e.Diagnostic.Message)
                End If
            End Sub
        Try
            Dim solution As Solution
            If isSolution Then
                solution = workspace.OpenSolutionAsync(fullPath).GetAwaiter().GetResult()
            ElseIf isSlnx Then
                For Each projectPath As String In projectPaths
                    If Not IsOpen(workspace, projectPath) Then workspace.OpenProjectAsync(projectPath).GetAwaiter().GetResult()
                Next
                solution = workspace.CurrentSolution
            Else
                Dim project As Project = workspace.OpenProjectAsync(fullPath).GetAwaiter().GetResult()
                solution = project.Solution
            End If
            If failures.Count > 0 Then
                Throw New WorkspaceLoadException(failures)
            End If
            Return New SolutionLoader(workspace, solution, fullPath)
        Catch
            workspace.Dispose()
            Throw
        End Try
    End Function

    ''' <summary>
    ''' Compiles every project, in ordinal order of solution-relative project path, collecting Error diagnostics.
    ''' </summary>
    ''' <param name="basePath">The base directory for relative paths.</param>
    ''' <returns>One entry per project.</returns>
    Public Function CompileAll(basePath As String) As List(Of CompiledProject)
        Dim result As List(Of CompiledProject) = New List(Of CompiledProject)()
        For Each project As Project In Solution.Projects
            Dim compilation As Compilation = project.GetCompilationAsync().GetAwaiter().GetResult()
            If compilation Is Nothing Then
                Throw New WorkspaceLoadException(New String() {"no compilation for " & project.FilePath})
            End If
            Dim compiled As CompiledProject = New CompiledProject With {
                .Project = project,
                .Compilation = compilation,
                .RelativePath = SolutionPaths.Relative(basePath, project.FilePath),
                .TargetFramework = ReadTargetFramework(project)}
            For Each diagnostic As Diagnostic In compilation.GetDiagnostics()
                If diagnostic.Severity = DiagnosticSeverity.Error Then
                    compiled.Errors.Add(diagnostic)
                End If
            Next
            result.Add(compiled)
        Next
        result.Sort(Function(a As CompiledProject, b As CompiledProject) SolutionPaths.Compare(a.RelativePath, b.RelativePath))
        Return result
    End Function

    ''' <summary>
    ''' Releases the workspace and its build host.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        _workspace.Dispose()
    End Sub

    Private Shared Function IsOpen(workspace As MSBuildWorkspace, projectPath As String) As Boolean
        For Each project As Project In workspace.CurrentSolution.Projects
            If String.Equals(project.FilePath, projectPath, StringComparison.OrdinalIgnoreCase) Then Return True
        Next
        Return False
    End Function

    Private Shared Function ReadTargetFramework(project As Project) As String
        Dim document As XDocument = XDocument.Load(project.FilePath)
        For Each element As XElement In document.Descendants()
            If element.Name.LocalName = "TargetFramework" AndAlso Not String.IsNullOrWhiteSpace(element.Value) Then
                Return element.Value.Trim()
            End If
        Next
        For Each element As XElement In document.Descendants()
            If element.Name.LocalName = "TargetFrameworks" AndAlso Not String.IsNullOrWhiteSpace(element.Value) Then
                Return element.Value.Split(";"c)(0).Trim()
            End If
        Next
        If Not String.IsNullOrEmpty(project.OutputFilePath) Then
            Dim directory As String = IO.Path.GetFileName(IO.Path.GetDirectoryName(project.OutputFilePath))
            If Not String.IsNullOrEmpty(directory) Then Return directory
        End If
        Throw New WorkspaceLoadException(New String() {"target framework of " & project.FilePath & " could not be determined"})
    End Function

End Class

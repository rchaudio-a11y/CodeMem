' File: SolutionLoader.vb
' Project: CodeMem.Extraction
' Description: Opens a solution or project through MSBuildWorkspace under the requested configuration and compiles every project (FR-001, FR-004).
' Author: RCH Automation LLC
' Created: 2026-09-09

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

    ''' <summary>The absolute path of the .sln or .vbproj that was opened.</summary>
    Public ReadOnly Property OpenedPath As String

    Private Sub New(workspace As MSBuildWorkspace, solution As Solution, openedPath As String)
        _workspace = workspace
        Me.Solution = solution
        Me.OpenedPath = openedPath
    End Sub

    ''' <summary>
    ''' Opens a .sln or .vbproj. Workspace diagnostics of kind Failure become <see cref="WorkspaceLoadException"/>.
    ''' </summary>
    ''' <param name="path">The solution or project path.</param>
    ''' <param name="configuration">The build configuration.</param>
    ''' <param name="framework">The target framework, or Nothing for the project's own.</param>
    ''' <returns>The loader holding the open workspace.</returns>
    Public Shared Function Open(path As String, configuration As String, framework As String) As SolutionLoader
        Dim fullPath As String = IO.Path.GetFullPath(path)
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
            Dim extension As String = IO.Path.GetExtension(fullPath)
            If String.Equals(extension, ".sln", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(extension, ".slnx", StringComparison.OrdinalIgnoreCase) Then
                solution = workspace.OpenSolutionAsync(fullPath).GetAwaiter().GetResult()
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

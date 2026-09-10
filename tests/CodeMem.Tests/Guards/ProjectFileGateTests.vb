' File: ProjectFileGateTests.vb
' Project: CodeMem.Tests
' Description: Review gate — every project file carries the constitution's Option settings and XML documentation output.
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' FIRE: 2026-09-09 removed <OptionInfer>Off</OptionInfer> from CodeMem.Extraction.vbproj → red
'       ("CodeMem.Extraction.vbproj missing <OptionInfer>Off"); reverted → green.

Imports System.IO
Imports Xunit

''' <summary>
''' Asserts the constitution's cross-cutting project-file settings on every <c>.vbproj</c> under <c>src/</c> and <c>tests/</c>.
''' </summary>
Public Class ProjectFileGateTests

    ''' <summary>
    ''' Every project file contains Option Strict On, Option Explicit On, Option Infer Off and GenerateDocumentationFile true.
    ''' </summary>
    <Fact>
    Public Sub EveryProjectFileCarriesTheOptionSettings()
        Dim root As String = RepoPaths.RepositoryRoot()
        Dim projects As List(Of String) = New List(Of String)()
        For Each dir As String In New String() {Path.Combine(root, "src"), Path.Combine(root, "tests")}
            For Each file As String In Directory.GetFiles(dir, "*.vbproj", SearchOption.AllDirectories)
                If Not file.Replace("\"c, "/"c).Contains("/Fixtures/") Then
                    projects.Add(file)
                End If
            Next
        Next
        Assert.True(projects.Count >= 4, "expected at least four project files, found " & projects.Count)

        Dim required As String() = New String() {"<OptionStrict>On", "<OptionExplicit>On", "<OptionInfer>Off", "<GenerateDocumentationFile>true"}
        Dim failures As List(Of String) = New List(Of String)()
        For Each project As String In projects
            Dim text As String = File.ReadAllText(project)
            For Each marker As String In required
                If Not text.Contains(marker) Then
                    failures.Add(Path.GetFileName(project) & " missing " & marker)
                End If
            Next
        Next
        Assert.True(failures.Count = 0, String.Join(Environment.NewLine, failures))
    End Sub

End Class

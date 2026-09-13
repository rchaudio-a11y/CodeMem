' File: AcceptanceRunner.vb
' Project: CodeMem.Tests
' Description: Skip-armed acceptance runner (FR-039): extracts the solution named by CODEMEM_ACCEPT_SOLUTION and reports counts; reported, never gated.
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-13 (fixpack 003, rule 1): the handles-in-source count walks the same in-scope trees the run maps (SolutionScope resolved the way
' ExtractionRun resolves it), so handles_written and handles_in_source still count one thing.

Imports System.Diagnostics
Imports CodeMem.Extraction
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Xunit
Imports Xunit.Abstractions

''' <summary>
''' When the variable is unset the runner is reported Skipped; when set it prints the summary line, handles written versus counted,
''' partial types, both residuals and elapsed time. Only the exit code is asserted.
''' </summary>
Public Class AcceptanceRunner

    Private ReadOnly _output As ITestOutputHelper

    ''' <summary>
    ''' Receives the test output sink.
    ''' </summary>
    ''' <param name="output">Where the report is printed.</param>
    Public Sub New(output As ITestOutputHelper)
        _output = output
    End Sub

    ''' <summary>
    ''' Extracts the named solution through the real executable and reports.
    ''' </summary>
    <SkippableFact>
    Public Sub ReportOnTheNamedSolution()
        Dim solutionPath As String = Environment.GetEnvironmentVariable("CODEMEM_ACCEPT_SOLUTION")
        Skip.If(String.IsNullOrEmpty(solutionPath), "CODEMEM_ACCEPT_SOLUTION is not set")
        solutionPath = IO.Path.GetFullPath(solutionPath)

        Using map As TempMap = New TempMap()
            Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(solutionPath) & " --db " & ExtractorProcess.Quote(map.Path), Nothing, TimeSpan.FromMinutes(30))
            Dim summary As String = run.StandardOutput.Trim()
            Print(summary)
            If run.ExitCode <> 0 Then Print(run.StandardError)

            Dim handlesInSource As Integer = 0
            Dim partialTypes As Integer = 0
            Dim seenTypes As HashSet(Of String) = New HashSet(Of String)(StringComparer.Ordinal)
            Using loader As SolutionLoader = SolutionLoader.Open(solutionPath, "Debug", Nothing)
                Dim basePath As String = SolutionPaths.BaseDirectory(solutionPath)
                Dim scope As SolutionScope = SolutionScope.Resolve(GitProvenance.Read(basePath, CompiledInputs.Enumerate(loader.Solution, basePath)).RepoRoot, basePath)
                For Each project As CompiledProject In loader.CompileAll(basePath)
                    If Not scope.Contains(project.Project.FilePath) Then Continue For
                    For Each tree As SyntaxTree In CompiledInputs.SourceTrees(project.Project, project.Compilation, scope)
                        For Each node As SyntaxNode In tree.GetRoot().DescendantNodes()
                            If TypeOf node Is HandlesClauseItemSyntax OrElse node.IsKind(SyntaxKind.AddHandlerStatement) Then handlesInSource += 1
                        Next
                    Next
                    CountPartialTypes(project.Compilation.Assembly.GlobalNamespace, seenTypes, partialTypes)
                Next
            End Using

            Dim handlesWritten As Integer = 0
            Dim unaccountedObserved As Integer = 0
            Dim unaccountedRegistry As Integer = 0
            If run.ExitCode = 0 Then
                Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
                handlesWritten = MapQueries.ReadEdges(map.Path, solutionId, "handles").Count
                Dim last As RunRow = MapQueries.ReadRuns(map.Path)(MapQueries.ReadRuns(map.Path).Count - 1)
                unaccountedObserved = last.UnaccountedObserved
                unaccountedRegistry = last.UnaccountedRegistry
            End If
            Print("handles_written=" & handlesWritten & " handles_in_source=" & handlesInSource)
            Print("partial_types=" & partialTypes)
            Print("unaccounted_observed=" & unaccountedObserved & " unaccounted_registry=" & unaccountedRegistry)
            Print("elapsed_ms=" & CLng(run.Elapsed.TotalMilliseconds))

            Assert.Equal(0, run.ExitCode)
        End Using
    End Sub

    Private Sub Print(line As String)
        _output.WriteLine(line)
        Console.WriteLine(line)
    End Sub

    Private Shared Sub CountPartialTypes(ns As INamespaceSymbol, seen As HashSet(Of String), ByRef count As Integer)
        For Each member As ISymbol In ns.GetMembers()
            Dim child As INamespaceSymbol = TryCast(member, INamespaceSymbol)
            If child IsNot Nothing Then
                CountPartialTypes(child, seen, count)
                Continue For
            End If
            Dim type As INamedTypeSymbol = TryCast(member, INamedTypeSymbol)
            If type IsNot Nothing Then CountPartialType(type, seen, count)
        Next
    End Sub

    Private Shared Sub CountPartialType(type As INamedTypeSymbol, seen As HashSet(Of String), ByRef count As Integer)
        Dim docId As String = type.GetDocumentationCommentId()
        If docId IsNot Nothing AndAlso seen.Add(docId) AndAlso type.DeclaringSyntaxReferences.Length > 1 Then count += 1
        For Each nested As INamedTypeSymbol In type.GetTypeMembers()
            CountPartialType(nested, seen, count)
        Next
    End Sub

End Class

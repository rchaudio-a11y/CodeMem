' File: FixtureSolution.vb
' Project: CodeMem.Tests
' Description: Collection fixture: locates and restores the committed fixture solution once per test collection (FR-036).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' 2026-09-17 (feature 005, T004): SolutionXPath - the committed Sample.slnx twin naming the same two projects (X01's format-parity fact);
' one restore covers both files, because restore is per project.

Imports System.IO
Imports System.Threading.Tasks
Imports Xunit

''' <summary>
''' The committed fixture solution, restored once and shared read-only by every test in the Fixture collection.
''' </summary>
Public Class FixtureSolution
    Implements IAsyncLifetime

    ''' <summary>Absolute path of <c>Sample.sln</c>.</summary>
    Public ReadOnly Property SolutionPath As String

    ''' <summary>Absolute path of <c>Sample.slnx</c>, the twin of <c>Sample.sln</c> naming the same two projects (feature 005).</summary>
    Public ReadOnly Property SolutionXPath As String

    ''' <summary>Absolute path of <c>Sample.Lib/Sample.Lib.vbproj</c>.</summary>
    Public ReadOnly Property LibProjectPath As String

    ''' <summary>Absolute path of the fixture directory.</summary>
    Public ReadOnly Property FixtureDirectory As String

    ''' <summary>
    ''' Resolves the fixture paths by walking up from the test output directory.
    ''' </summary>
    Public Sub New()
        FixtureDirectory = RepoPaths.FixtureDirectory()
        SolutionPath = Path.Combine(FixtureDirectory, "Sample.sln")
        SolutionXPath = Path.Combine(FixtureDirectory, "Sample.slnx")
        LibProjectPath = Path.Combine(FixtureDirectory, "Sample.Lib", "Sample.Lib.vbproj")
    End Sub

    ''' <summary>
    ''' Restores the fixture once so the design-time build can resolve references (research R10).
    ''' </summary>
    ''' <returns>A completed task.</returns>
    Public Function InitializeAsync() As Task Implements IAsyncLifetime.InitializeAsync
        DotnetCli.Run("restore """ & SolutionPath & """", FixtureDirectory)
        Return Task.CompletedTask
    End Function

    ''' <summary>
    ''' Nothing to release.
    ''' </summary>
    ''' <returns>A completed task.</returns>
    Public Function DisposeAsync() As Task Implements IAsyncLifetime.DisposeAsync
        Return Task.CompletedTask
    End Function

End Class

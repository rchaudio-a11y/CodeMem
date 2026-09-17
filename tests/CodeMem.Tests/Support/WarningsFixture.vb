' File: WarningsFixture.vb
' Project: CodeMem.Tests
' Description: Collection fixture: locates the two warning fixture projects under Fixtures/Warnings and restores each once from the local package folder beside them (feature 005, research R71).
' Author: RCH Automation LLC
' Created: 2026-09-17

Imports System.IO
Imports System.Threading.Tasks
Imports Xunit

''' <summary>
''' The committed warning fixtures, restored once and shared read-only by every fact in the Warnings collection. The Nu1701 restore
''' succeeds with a warning; the Nu1101 restore fails by design (NU1101) and still writes its assets file, which is what the loader reads.
''' </summary>
Public Class WarningsFixture
    Implements IAsyncLifetime

    ''' <summary>Absolute path of <c>Fixtures/Warnings</c>.</summary>
    Public ReadOnly Property WarningsDirectory As String

    ''' <summary>Absolute path of <c>Nu1701/Nu1701.vbproj</c>.</summary>
    Public ReadOnly Property Nu1701ProjectPath As String

    ''' <summary>Absolute path of <c>Nu1101/Nu1101.vbproj</c>.</summary>
    Public ReadOnly Property Nu1101ProjectPath As String

    ''' <summary>Absolute path of the local package folder the fixtures restore from.</summary>
    Public ReadOnly Property PackagesDirectory As String

    ''' <summary>
    ''' Resolves the fixture paths from the test project directory.
    ''' </summary>
    Public Sub New()
        WarningsDirectory = Path.Combine(RepoPaths.TestProjectDirectory(), "Fixtures", "Warnings")
        Nu1701ProjectPath = Path.Combine(WarningsDirectory, "Nu1701", "Nu1701.vbproj")
        Nu1101ProjectPath = Path.Combine(WarningsDirectory, "Nu1101", "Nu1101.vbproj")
        PackagesDirectory = Path.Combine(WarningsDirectory, "packages")
    End Sub

    ''' <summary>
    ''' Restores both projects offline: Nu1701 must succeed; Nu1101 exits non-zero by design and is not an error here.
    ''' </summary>
    ''' <returns>A completed task.</returns>
    Public Function InitializeAsync() As Task Implements IAsyncLifetime.InitializeAsync
        DotnetCli.Run("restore """ & Nu1701ProjectPath & """", Path.GetDirectoryName(Nu1701ProjectPath))
        DotnetCli.TryRun("restore """ & Nu1101ProjectPath & """", Path.GetDirectoryName(Nu1101ProjectPath))
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

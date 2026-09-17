' File: Nu1701MapFixture.vb
' Project: CodeMem.Tests
' Description: Class fixture for X02 (feature 005, T033; analyze I3): one launch of the extractor on the Nu1701 fixture project into a fresh map, the child's environment without any NoWarn* variable, the result kept for (1) and (5).
' Author: RCH Automation LLC
' Created: 2026-09-17

Imports System.IO

''' <summary>
''' Built after the Warnings collection has restored the fixture projects.
''' </summary>
Public Class Nu1701MapFixture
    Implements IDisposable

    ''' <summary>The map the launch wrote.</summary>
    Public ReadOnly Property Map As TempMap

    ''' <summary>The launch.</summary>
    Public ReadOnly Property Run As ExtractorProcess

    ''' <summary>The fixture's Nu1701.vbproj.</summary>
    Public ReadOnly Property ProjectPath As String

    ''' <summary>
    ''' Launches once.
    ''' </summary>
    Public Sub New()
        ProjectPath = Path.Combine(RepoPaths.TestProjectDirectory(), "Fixtures", "Warnings", "Nu1701", "Nu1701.vbproj")
        Map = New TempMap()
        Dim environment As Dictionary(Of String, String) = ChildEnvironment.WithoutNoWarn()
        ChildEnvironment.AssertNoNoWarn(environment)
        Run = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(ProjectPath) & " --db " & ExtractorProcess.Quote(Map.Path), environment)
    End Sub

    ''' <summary>
    ''' Deletes the map.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Map.Dispose()
    End Sub

End Class

' File: CompiledProject.vb
' Project: CodeMem.Extraction
' Description: One project with its compilation, actual target framework and Error diagnostics.
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports Microsoft.CodeAnalysis

''' <summary>
''' The result of compiling one project of the loaded solution.
''' </summary>
Public Class CompiledProject

    ''' <summary>The Roslyn project.</summary>
    Public Property Project As Project

    ''' <summary>The compilation.</summary>
    Public Property Compilation As Compilation

    ''' <summary>Solution-relative path of the project file.</summary>
    Public Property RelativePath As String

    ''' <summary>The target framework the project file declares (first of TargetFrameworks when plural).</summary>
    Public Property TargetFramework As String

    ''' <summary>Every diagnostic of severity Error.</summary>
    Public Property Errors As List(Of Diagnostic) = New List(Of Diagnostic)()

End Class

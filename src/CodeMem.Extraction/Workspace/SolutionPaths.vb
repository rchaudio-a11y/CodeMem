' File: SolutionPaths.vb
' Project: CodeMem.Extraction
' Description: Solution-relative, forward-slash paths: the one convention every stored path follows (spec Assumptions).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.IO

''' <summary>
''' Path helpers for the base directory and solution-relative paths.
''' </summary>
Public Module SolutionPaths

    ''' <summary>
    ''' The directory of the .sln or .vbproj the run was pointed at.
    ''' </summary>
    ''' <param name="solutionPath">The solution or project path.</param>
    ''' <returns>The absolute base directory.</returns>
    Public Function BaseDirectory(solutionPath As String) As String
        Return Path.GetDirectoryName(Path.GetFullPath(solutionPath))
    End Function

    ''' <summary>
    ''' A forward-slash path relative to the base directory; parent segments are allowed for linked files.
    ''' </summary>
    ''' <param name="base">The base directory.</param>
    ''' <param name="fullPath">The absolute path.</param>
    ''' <returns>The relative path.</returns>
    Public Function Relative(base As String, fullPath As String) As String
        Return Path.GetRelativePath(base, Path.GetFullPath(fullPath)).Replace("\"c, "/"c)
    End Function

    ''' <summary>
    ''' Ordinal comparison of two relative paths (the one ordering every (path, offset) sort uses).
    ''' </summary>
    ''' <param name="left">First path.</param>
    ''' <param name="right">Second path.</param>
    ''' <returns>Less than, equal to or greater than zero.</returns>
    Public Function Compare(left As String, right As String) As Integer
        Return String.CompareOrdinal(left, right)
    End Function

End Module

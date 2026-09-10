' File: ExtractionOptions.vb
' Project: CodeMem.Extraction
' Description: The resolved command-line options of one run (FR-001).
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' What one run extracts, into which map, under which configuration.
''' </summary>
Public Class ExtractionOptions

    ''' <summary>Path to a .sln or .vbproj.</summary>
    Public Property SolutionPath As String

    ''' <summary>Path to the map file.</summary>
    Public Property DbPath As String

    ''' <summary>Build configuration; default Debug.</summary>
    Public Property Configuration As String = "Debug"

    ''' <summary>Target framework to compile, or Nothing for the project's first.</summary>
    Public Property Framework As String

    ''' <summary>Solution key override, or Nothing to derive from the file name.</summary>
    Public Property SolutionKey As String

End Class

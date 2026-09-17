' File: TypeUsagesScenario.vb
' Project: CodeMem.Tests
' Description: Class fixture for B03: a fixture copy with an explicit constructor, a sibling call and a cross-project user of AlphaService, extracted once into a temp map (feature 004, T027).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T017): no registry - the configuration names the map only (FR-403).

Imports System.IO
Imports CodeMem.Extraction

''' <summary>
''' Edits Widgets.vb at its one unique anchor inside AlphaService.Serve (an Implements statement must directly follow the class statement,
''' so nothing is inserted before it; the file uses LF line endings) and writes Sample.App/ServiceUser.vb; then extracts.
''' </summary>
Public Class TypeUsagesScenario
    Implements IDisposable

    ''' <summary>The fixture copy.</summary>
    Public ReadOnly Property Copy As FixtureCopy

    ''' <summary>The temp map.</summary>
    Public ReadOnly Property Map As TempMap

    ''' <summary>The map's solution id.</summary>
    Public ReadOnly Property SolutionId As Long

    ''' <summary>An in-process host over the map, both gates off.</summary>
    Public ReadOnly Property Host As BridgeHost

    ''' <summary>
    ''' Copies, edits, extracts.
    ''' </summary>
    Public Sub New()
        Copy = New FixtureCopy()
        Copy.Replace("Sample.Lib/Widgets.vb", "            Return ""Alpha:"" & input",
                     "            Return ""Alpha:"" & input" & vbLf & "        End Function" & vbLf & "        Public Sub New()" & vbLf & "        End Sub" & vbLf &
                     "        Public Function Twice(input As String) As String" & vbLf & "            Return Serve(input) & Serve(input)")
        File.WriteAllText(Path.Combine(Copy.Directory, "Sample.App", "ServiceUser.vb"),
                          "Public Class ServiceUser" & vbLf & "    Public Function Run() As String" & vbLf & "        Dim s As Widgets.AlphaService = New Widgets.AlphaService()" & vbLf &
                          "        Return s.Serve(""x"")" & vbLf & "    End Function" & vbLf & "End Class" & vbLf)
        Map = New TempMap()
        Dim code As ExitCode = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = Copy.SolutionPath, .DbPath = Map.Path, .SolutionKey = "Sample"}, Nothing)
        If code <> ExitCode.Success Then Throw New InvalidOperationException("fixture copy extraction failed: " & code.ToString())
        SolutionId = MapQueries.ReadSolutions(Map.Path)(0).Id
        Host = New BridgeHost(BridgeHost.WriteConfig(Map.Path, Nothing, False, False))
    End Sub

    ''' <summary>
    ''' The active row with a doc-comment id.
    ''' </summary>
    ''' <param name="docCommentId">The doc-comment id.</param>
    ''' <returns>The row's id.</returns>
    Public Function SymbolId(docCommentId As String) As Long
        Dim row As SymbolRow = MapQueries.ReadSymbols(Map.Path, SolutionId).Find(Function(s As SymbolRow) s.DocCommentId = docCommentId AndAlso s.IsActive)
        If row Is Nothing Then Throw New InvalidOperationException(docCommentId & " is not an active row of the copy's map")
        Return row.Id
    End Function

    ''' <summary>
    ''' Deletes the map and the copy.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Map.Dispose()
        Copy.Dispose()
    End Sub

End Class

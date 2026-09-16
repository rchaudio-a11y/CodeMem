' File: TwinScenario.vb
' Project: CodeMem.Tests
' Description: Class fixture for B07: two fixture copies of one edited tree - a file linked into both projects (distinct doc-comment ids under one root namespace, one path, one line) and two overloads on one physical line under one project - extracted into one map as Sample and as Other (feature 004, T048).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.IO
Imports CodeMem.Extraction

''' <summary>
''' Shared/Twin.vb switches its namespace on a compilation symbol so the two copies get distinct doc-comment ids; Sample.App defines
''' TWIN_APP. Sample.Lib/OnOneLine.vb declares two overloads on adjacent lines, then the second row is moved onto the first's line in the
''' map: same kind, name, path, line and project, distinct doc ids (COR3; see MoveSecondOverloadOntoTheFirst).
''' The second copy carries the same edits and is extracted as Other: twins never fold across solutions.
''' </summary>
Public Class TwinScenario
    Implements IDisposable

    ''' <summary>The fixture copy extracted as Sample.</summary>
    Public ReadOnly Property Copy As FixtureCopy

    ''' <summary>The second copy of the same edited tree, extracted as Other.</summary>
    Public ReadOnly Property Other As FixtureCopy

    ''' <summary>The temp map holding both extractions.</summary>
    Public ReadOnly Property Map As TempMap

    ''' <summary>A registry binding Sample under project 131373.</summary>
    Public ReadOnly Property Registry As RegistryFixture

    ''' <summary>The Sample solution id.</summary>
    Public ReadOnly Property SolutionId As Long

    ''' <summary>An in-process host, both gates off.</summary>
    Public ReadOnly Property Host As BridgeHost

    ''' <summary>
    ''' Edits both copies, extracts each, seeds the registry, writes the configuration.
    ''' </summary>
    Public Sub New()
        Copy = New FixtureCopy()
        Prepare(Copy)
        Other = New FixtureCopy()
        Prepare(Other)
        Map = New TempMap()
        Extract(Copy, "Sample")
        Extract(Other, "Other")
        SolutionId = MapQueries.ReadSolutions(Map.Path).Find(Function(s As SolutionRow) s.Key = "Sample").Id
        MoveSecondOverloadOntoTheFirst()
        Registry = New RegistryFixture()
        Registry.Seed(131373, "Sample", SolutionId, "active", Copy.SolutionPath)
        Host = New BridgeHost(BridgeHost.WriteConfig(Map.Path, Registry.Path, Nothing, False, False))
    End Sub

    ''' <summary>
    ''' The active Sample row with a doc-comment id.
    ''' </summary>
    ''' <param name="docCommentId">The doc-comment id.</param>
    ''' <returns>The row's id; fails when there is no active row.</returns>
    Public Function SymbolId(docCommentId As String) As Long
        Dim row As SymbolRow = MapQueries.ReadSymbols(Map.Path, SolutionId).Find(Function(s As SymbolRow) s.DocCommentId = docCommentId AndAlso s.IsActive)
        If row Is Nothing Then Throw New InvalidOperationException(docCommentId & " is not an active row of the Sample solution")
        Return row.Id
    End Function

    ''' <summary>
    ''' Deletes the registry, the map and both copies.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Registry.Dispose()
        Map.Dispose()
        Other.Dispose()
        Copy.Dispose()
    End Sub

    Private Shared Sub Prepare(copy As FixtureCopy)
        Directory.CreateDirectory(Path.Combine(copy.Directory, "Shared"))
        File.WriteAllText(Path.Combine(copy.Directory, "Shared", "Twin.vb"),
                          "#If TWIN_APP Then" & vbLf & "Namespace Global.Sample.App.Shared" & vbLf & "#Else" & vbLf & "Namespace Global.Sample.Lib.Shared" & vbLf & "#End If" & vbLf &
                          "    Public Class Twin" & vbLf & "        Public Function Name() As String" & vbLf & "            Return ""twin""" & vbLf & "        End Function" & vbLf & "    End Class" & vbLf & "End Namespace" & vbLf)
        File.WriteAllText(Path.Combine(copy.Directory, "Sample.Lib", "OnOneLine.vb"),
                          "Public Class OnOneLine" & vbLf & "    Public Sub Overloaded(a As Integer) : End Sub" & vbLf & "    Public Sub Overloaded(a As String) : End Sub" & vbLf & "End Class" & vbLf)
        For Each project As String In New String() {"Sample.Lib/Sample.Lib.vbproj", "Sample.App/Sample.App.vbproj"}
            copy.Replace(project, "</Project>", "  <ItemGroup>" & vbLf & "    <Compile Include=""..\Shared\Twin.vb"" />" & vbLf & "  </ItemGroup>" & vbLf & vbLf & "</Project>")
        Next
        copy.Replace("Sample.App/Sample.App.vbproj", "    <OptionInfer>Off</OptionInfer>", "    <OptionInfer>Off</OptionInfer>" & vbLf & "    <DefineConstants>$(DefineConstants),TWIN_APP=True</DefineConstants>")
    End Sub

    ''' <summary>
    ''' VB refuses two method declarations on one logical line (BC32009) and the extractor records raw line spans, so the same-project
    ''' shape of COR3 - two active rows of one project sharing kind, name, path and start line - is made here: the second Overloaded
    ''' overload's start_line is set to the first's in the temp map. The rows, their doc ids and their project are the extractor's own.
    ''' </summary>
    Private Sub MoveSecondOverloadOntoTheFirst()
        Dim rows As List(Of SymbolRow) = MapQueries.ReadSymbols(Map.Path, SolutionId).FindAll(
            Function(s As SymbolRow) s.IsActive AndAlso s.Kind = "method" AndAlso s.Name = "Overloaded" AndAlso s.Path.EndsWith("OnOneLine.vb", StringComparison.Ordinal))
        If rows.Count <> 2 Then Throw New InvalidOperationException("expected two Overloaded rows, found " & rows.Count)
        rows.Sort(Function(a As SymbolRow, b As SymbolRow) a.StartLine.CompareTo(b.StartLine))
        MapQueries.SetStartLine(Map.Path, rows(1).Id, rows(0).StartLine)
    End Sub

    Private Sub Extract(copy As FixtureCopy, key As String)
        Dim code As ExitCode = ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = copy.SolutionPath, .DbPath = Map.Path, .SolutionKey = key}, Nothing)
        If code <> ExitCode.Success Then Throw New InvalidOperationException("extraction as " & key & " failed: " & code.ToString())
    End Sub

End Class

' File: MemOsReferenceGateTests.vb
' Project: CodeMem.Tests
' Description: Review gate (feature 004, FR-343): no project file references, packages or links anything under rchaudio-a11y\MemOS.
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' Vacuous-Red rule (Article II): the positive count of ProjectReference elements is asserted first, so an empty or unparsed tree cannot
' pass the absence assertion by accident. A guard on absence: it passes before any bridge code exists and is trusted through its fire.
' GREEN: 2026-09-15 (T007) first run, five project files then six (Bridging, Bridge added); 4 ProjectReference elements found.
' FIRE:  2026-09-15 (T007) inserted <ProjectReference Include="..\..\..\rchaudio-a11y\MemOS\MemOS.Core\MemOS.Core.vbproj" /> into
'        src/CodeMem.Bridging/CodeMem.Bridging.vbproj and ran with --no-build -> red ("CodeMem.Bridging.vbproj: ProjectReference
'        Include=..."); reverted -> green.

Imports System.IO
Imports System.Xml.Linq
Imports Xunit

''' <summary>
''' Parses every .vbproj under src/ and tests/ (fixtures excluded) as XML and inspects the Include of every ProjectReference,
''' PackageReference and Compile element.
''' </summary>
Public Class MemOsReferenceGateTests

    ''' <summary>
    ''' At least one ProjectReference is found, and no Include value contains MemOS or rchaudio-a11y.
    ''' </summary>
    <Fact>
    Public Sub NoProjectFileReferencesMemOs()
        Dim root As String = RepoPaths.RepositoryRoot()
        Dim projectReferences As Integer = 0
        Dim offenders As List(Of String) = New List(Of String)()
        For Each dir As String In New String() {Path.Combine(root, "src"), Path.Combine(root, "tests")}
            For Each project As String In Directory.GetFiles(dir, "*.vbproj", SearchOption.AllDirectories)
                If project.Replace("\"c, "/"c).Contains("/Fixtures/") Then Continue For
                Dim document As XDocument = XDocument.Load(project)
                For Each element As XElement In document.Descendants()
                    Dim kind As String = element.Name.LocalName
                    If kind <> "ProjectReference" AndAlso kind <> "PackageReference" AndAlso kind <> "Compile" Then Continue For
                    Dim include As XAttribute = element.Attribute("Include")
                    If include Is Nothing Then Continue For
                    If kind = "ProjectReference" Then projectReferences += 1
                    If include.Value.IndexOf("MemOS", StringComparison.OrdinalIgnoreCase) >= 0 OrElse include.Value.IndexOf("rchaudio-a11y", StringComparison.OrdinalIgnoreCase) >= 0 Then
                        offenders.Add(Path.GetFileName(project) & ": " & kind & " Include=""" & include.Value & """")
                    End If
                Next
            Next
        Next
        Assert.True(projectReferences > 0, "no ProjectReference found: the scan is vacuous")
        Assert.True(offenders.Count = 0, String.Join(Environment.NewLine, offenders))
    End Sub

End Class

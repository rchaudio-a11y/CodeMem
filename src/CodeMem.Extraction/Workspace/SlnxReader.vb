' File: SlnxReader.vb
' Project: CodeMem.Extraction
' Description: The loader's own parse of a .slnx: a well-formed XML document with a Solution root; every Project element at any depth with a Path attribute, resolved against the file's directory with forward slashes normalised; each path checked for existence before any project is opened (005 FR-421; research R61; data-model §10; contracts/extractor.md §2).
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' Folders, configurations, properties and Type attributes are not read (plan §Known limits). The four refusals are one stderr line each,
' exit 1, nothing written - WorkspaceLoadException with the line as its whole message.

Imports System.IO
Imports System.Xml
Imports System.Xml.Linq

''' <summary>
''' Route (b) of R61: the parse is CodeMem's, the opening is the workspace's, one project at a time in document order.
''' </summary>
Public Module SlnxReader

    ''' <summary>
    ''' Reads the project paths a .slnx names.
    ''' </summary>
    ''' <param name="slnxPath">The .slnx, as a full path.</param>
    ''' <returns>The full paths of every project, in document order, each existing.</returns>
    ''' <exception cref="WorkspaceLoadException">Not well-formed; no Solution root; no project; a project that does not exist.</exception>
    Public Function ReadProjectPaths(slnxPath As String) As List(Of String)
        Dim document As XDocument
        Try
            document = XDocument.Load(slnxPath)
        Catch ex As XmlException
            Throw WorkspaceLoadException.Refusal("solution file is not well-formed XML: " & slnxPath & ": " & ex.Message)
        End Try
        If document.Root Is Nothing OrElse document.Root.Name.LocalName <> "Solution" Then
            Throw WorkspaceLoadException.Refusal("solution file has no Solution root: " & slnxPath)
        End If
        Dim directory As String = Path.GetDirectoryName(slnxPath)
        Dim paths As List(Of String) = New List(Of String)()
        For Each element As XElement In document.Root.Descendants()
            If element.Name.LocalName <> "Project" Then Continue For
            Dim attribute As XAttribute = element.Attribute("Path")
            If attribute Is Nothing OrElse String.IsNullOrWhiteSpace(attribute.Value) Then Continue For
            paths.Add(Path.GetFullPath(Path.Combine(directory, attribute.Value.Replace("/"c, Path.DirectorySeparatorChar))))
        Next
        If paths.Count = 0 Then Throw WorkspaceLoadException.Refusal("solution file names no project: " & slnxPath)
        For Each projectPath As String In paths
            If Not File.Exists(projectPath) Then
                Throw WorkspaceLoadException.Refusal("solution file names a project that does not exist: " & projectPath & " (in " & slnxPath & ")")
            End If
        Next
        Return paths
    End Function

End Module

' File: DependsOnRule.vb
' Project: CodeMem.Extraction
' Description: The depends_on verb: project row to project row per ProjectReference, located at the referenced file name in the .vbproj text (research R13).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.IO
Imports CodeMem.Core
Imports Microsoft.CodeAnalysis

''' <summary>
''' Every project reference is written. When the file name does not occur in the project text (a props-injected reference) the edge sits at offset 0, line 1.
''' </summary>
Public Class DependsOnRule
    Implements EdgeRule

    ''' <inheritdoc/>
    Public Sub Collect(context As EdgeContext, sink As List(Of ObservedEdge)) Implements EdgeRule.Collect
        Dim project As Project = context.Project.Project
        Dim source As String = ProjectSymbols.DocIdOf(project)
        Dim text As String = File.ReadAllText(project.FilePath)
        Dim relativePath As String = SolutionPaths.Relative(context.BasePath, project.FilePath)
        For Each reference As ProjectReference In project.ProjectReferences
            Dim target As Project = context.Solution.GetProject(reference.ProjectId)
            If target Is Nothing Then Continue For
            Dim fileName As String = Path.GetFileName(target.FilePath)
            Dim offset As Integer = text.IndexOf(fileName, StringComparison.Ordinal)
            Dim location As SourceLocation
            If offset >= 0 Then
                location = LocationIn(text, relativePath, offset, fileName.Length)
            Else
                location = New SourceLocation(relativePath, 0, 0, 1, 1)
            End If
            sink.Add(New ObservedEdge With {
                .SourceDocCommentId = source,
                .Verb = EdgeVerb.DependsOn,
                .TargetDocCommentId = ProjectSymbols.DocIdOf(target),
                .ViaDocCommentId = Nothing,
                .Location = location})
        Next
    End Sub

    ''' <summary>
    ''' Builds a location in plain text, counting CRLF, CR and LF line breaks.
    ''' </summary>
    ''' <param name="text">The file text.</param>
    ''' <param name="relativePath">The file's solution-relative path.</param>
    ''' <param name="offset">0-based offset.</param>
    ''' <param name="length">Span length.</param>
    ''' <returns>The location.</returns>
    Public Shared Function LocationIn(text As String, relativePath As String, offset As Integer, length As Integer) As SourceLocation
        Dim line As Integer = 1
        Dim lineStart As Integer = 0
        Dim i As Integer = 0
        While i < offset
            If text(i) = ControlChars.Cr Then
                If i + 1 < offset AndAlso text(i + 1) = ControlChars.Lf Then i += 1
                line += 1
                lineStart = i + 1
            ElseIf text(i) = ControlChars.Lf Then
                line += 1
                lineStart = i + 1
            End If
            i += 1
        End While
        Return New SourceLocation(relativePath, offset, length, line, offset - lineStart + 1)
    End Function

End Class

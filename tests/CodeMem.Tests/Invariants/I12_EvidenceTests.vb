' File: I12_EvidenceTests.vb
' Project: CodeMem.Tests
' Description: I12 - every symbol row (projects excepted) and every edge row carries a span whose text is the surface name of what it evidences (FR-018, SC-004).
' Author: RCH Automation LLC
' Created: 2026-09-09
'
' RED:   2026-09-09 no edges exist yet (only symbol rows carry evidence) - reported to the Architect.
' GREEN: 2026-09-09 after the symbol walker (T055-T062) and the eight edge rules (T065-T080); whole US1 filter green in 8 s.

Imports System.IO
Imports System.Text.RegularExpressions
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' Reads every span back from the fixture's files and compares the text with the expected surface name.
''' </summary>
<Collection("Fixture")>
Public Class I12_EvidenceTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared fixture.
    ''' </summary>
    ''' <param name="fixture">The restored fixture solution.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' Every span is solution-relative, non-empty, starts at the recorded line and column, and reads as the expected name.
    ''' </summary>
    <Fact>
    Public Sub EverySpanReadsAsTheSurfaceName()
        Using map As TempMap = New TempMap()
            Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
            Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
            Dim symbols As List(Of SymbolRow) = MapQueries.ReadSymbols(map.Path, solutionId)
            Dim edges As List(Of EdgeRow) = MapQueries.ReadEdges(map.Path, solutionId, Nothing)
            Assert.True(edges.Count > 0, "no edges exist")
            Dim byId As Dictionary(Of Long, SymbolRow) = New Dictionary(Of Long, SymbolRow)()
            For Each s As SymbolRow In symbols
                byId(s.Id) = s
            Next
            Dim failures As List(Of String) = New List(Of String)()
            Dim texts As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)

            For Each s As SymbolRow In symbols
                If s.Kind = "project" Then Continue For
                Check(failures, texts, "symbol " & s.DocCommentId, s.Path, s.StartOffset, s.Length, s.StartLine, s.StartColumn, s.Name)
            Next
            For Each e As EdgeRow In edges
                Dim expected As String
                Select Case e.Verb
                    Case "part_of" : expected = byId(e.SourceSymbolId).Name
                    Case "depends_on" : expected = e.TargetDocCommentId.Substring("Project:".Length) & ".vbproj"
                    Case Else : expected = SurfaceName(e.TargetDocCommentId)
                End Select
                Check(failures, texts, e.Verb & " " & e.SourceDocCommentId & " -> " & e.TargetDocCommentId, e.Path, e.StartOffset, e.Length, e.StartLine, e.StartColumn, expected)
            Next
            Assert.True(failures.Count = 0, String.Join(Environment.NewLine, failures))
        End Using
    End Sub

    Private Sub Check(failures As List(Of String), texts As Dictionary(Of String, String), what As String, path As String, offset As Integer, length As Integer, line As Integer, column As Integer, expected As String)
        If String.IsNullOrEmpty(path) OrElse IO.Path.IsPathRooted(path) OrElse path.Contains("\"c) Then
            failures.Add(what & ": path not solution-relative: " & path)
            Return
        End If
        Dim text As String = Nothing
        If Not texts.TryGetValue(path, text) Then
            text = File.ReadAllText(IO.Path.Combine(_fixture.FixtureDirectory, path.Replace("/"c, IO.Path.DirectorySeparatorChar)))
            texts(path) = text
        End If
        If length <= 0 OrElse offset < 0 OrElse offset + length > text.Length Then
            failures.Add(what & ": empty or out-of-range span " & offset & "+" & length & " in " & path)
            Return
        End If
        Dim actual As String = text.Substring(offset, length)
        If Not String.Equals(actual, expected, StringComparison.Ordinal) Then
            failures.Add(what & ": span text '" & actual & "' <> '" & expected & "' at " & path & ":" & line & "," & column)
        End If
        Dim computedLine As Integer = 1
        Dim lineStart As Integer = 0
        Dim i As Integer = 0
        While i < offset
            If text(i) = ControlChars.Cr Then
                If i + 1 < offset AndAlso text(i + 1) = ControlChars.Lf Then i += 1
                computedLine += 1
                lineStart = i + 1
            ElseIf text(i) = ControlChars.Lf Then
                computedLine += 1
                lineStart = i + 1
            End If
            i += 1
        End While
        Dim computedColumn As Integer = offset - lineStart + 1
        If computedLine <> line OrElse computedColumn <> column Then
            failures.Add(what & ": line/column " & line & "," & column & " <> computed " & computedLine & "," & computedColumn & " at " & path)
        End If
    End Sub

    ''' <summary>
    ''' Derives the VB surface name from a documentation-comment id: prefix and parameter list stripped, #ctor mapped to the type,
    ''' generic arity stripped, last dotted segment, special types mapped to their keywords.
    ''' </summary>
    ''' <param name="docId">The doc-comment id.</param>
    ''' <returns>The surface name.</returns>
    Public Shared Function SurfaceName(docId As String) As String
        Select Case docId
            Case "T:System.Int32" : Return "Integer"
            Case "T:System.String" : Return "String"
            Case "T:System.Boolean" : Return "Boolean"
            Case "T:System.Object" : Return "Object"
            Case "T:System.Int64" : Return "Long"
            Case "T:System.Int16" : Return "Short"
            Case "T:System.Byte" : Return "Byte"
            Case "T:System.SByte" : Return "SByte"
            Case "T:System.UInt16" : Return "UShort"
            Case "T:System.UInt32" : Return "UInteger"
            Case "T:System.UInt64" : Return "ULong"
            Case "T:System.Single" : Return "Single"
            Case "T:System.Double" : Return "Double"
            Case "T:System.Decimal" : Return "Decimal"
            Case "T:System.Char" : Return "Char"
            Case "T:System.DateTime" : Return "Date"
        End Select
        Dim body As String = docId.Substring(docId.IndexOf(":"c) + 1)
        Dim paren As Integer = body.IndexOf("("c)
        If paren >= 0 Then body = body.Substring(0, paren)
        If body.EndsWith(".#ctor", StringComparison.Ordinal) Then body = body.Substring(0, body.Length - ".#ctor".Length)
        body = Regex.Replace(body, "`+\d+", "")
        Dim dot As Integer = body.LastIndexOf("."c)
        Return If(dot >= 0, body.Substring(dot + 1), body)
    End Function

End Class

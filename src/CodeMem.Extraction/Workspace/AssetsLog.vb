' File: AssetsLog.vb
' Project: CodeMem.Extraction
' Description: Classifies a workspace failure through NuGet's own record: the text after "with message: " looked up in the named project's obj/project.assets.json logs[] by message; the entry's code and level answer (005 FR-427; Q8 as ruled; research R62; data-model §11).
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' No match, no assets file, or a failure without the "Msbuild failed when processing the file '<project>' with message: <text>" shape
' answers Nothing, and the loader aborts as it always has. No NoWarn is read, set or passed anywhere (R62).

Imports System.IO
Imports System.Text.Json
Imports System.Text.RegularExpressions

''' <summary>
''' A project whose BaseIntermediateOutputPath is not obj/ keeps its assets file elsewhere; such a failure finds no entry and aborts as
''' today (R62's known limit).
''' </summary>
Public Module AssetsLog

    Private ReadOnly FailureShape As Regex = New Regex("^Msbuild failed when processing the file '(.+?)' with message: (.*)$", RegexOptions.Compiled Or RegexOptions.Singleline)

    ''' <summary>
    ''' Classifies one failure message.
    ''' </summary>
    ''' <param name="failureMessage">The WorkspaceFailed diagnostic's message.</param>
    ''' <returns>The match, or Nothing when NuGet's record does not explain the failure.</returns>
    Public Function Classify(failureMessage As String) As AssetsLogMatch
        If failureMessage Is Nothing Then Return Nothing
        Dim shape As Match = FailureShape.Match(failureMessage.Trim())
        If Not shape.Success Then Return Nothing
        Dim project As String = shape.Groups(1).Value
        Dim text As String = shape.Groups(2).Value.Trim()
        Dim assets As String = Path.Combine(Path.GetDirectoryName(project), "obj", "project.assets.json")
        If Not File.Exists(assets) Then Return Nothing
        Try
            Using document As JsonDocument = JsonDocument.Parse(File.ReadAllBytes(assets))
                Dim logs As JsonElement
                If Not document.RootElement.TryGetProperty("logs", logs) OrElse logs.ValueKind <> JsonValueKind.Array Then Return Nothing
                For Each entry As JsonElement In logs.EnumerateArray()
                    Dim message As JsonElement
                    If entry.TryGetProperty("message", message) AndAlso message.ValueKind = JsonValueKind.String AndAlso String.Equals(message.GetString().Trim(), text, StringComparison.Ordinal) Then
                        Return New AssetsLogMatch With {.Code = TextOf(entry, "code"), .Level = TextOf(entry, "level"), .ProjectPath = project, .Message = text}
                    End If
                Next
            End Using
        Catch ex As JsonException
            Return Nothing
        Catch ex As IOException
            Return Nothing
        Catch ex As UnauthorizedAccessException
            Return Nothing
        End Try
        Return Nothing
    End Function

    Private Function TextOf(entry As JsonElement, name As String) As String
        Dim value As JsonElement
        If entry.TryGetProperty(name, value) AndAlso value.ValueKind = JsonValueKind.String Then Return value.GetString()
        Return Nothing
    End Function

End Module

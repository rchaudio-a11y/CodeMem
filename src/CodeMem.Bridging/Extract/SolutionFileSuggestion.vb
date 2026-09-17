' File: SolutionFileSuggestion.vb
' Project: CodeMem.Bridging
' Description: The not-in-map answer's one rule: the given directory's own solution files, top level only, never a parent or a child; one file suggests the key and the command that adds it, more than one is ambiguous, none leaves placeholders (005 FR-411; Q4 as ruled; research R66; plan Article XI row).
' Author: RCH Automation LLC
' Created: 2026-09-17

Imports System.IO

''' <summary>
''' Two call sites - the extract refusal (TargetResolver) and map_status's not-in-map list (NotInMapReader) - and one rule (Article XII).
''' Extensions are compared whole, so *.sln never matches a .slnx.
''' </summary>
Public Module SolutionFileSuggestion

    ''' <summary>
    ''' Inspects one directory.
    ''' </summary>
    ''' <param name="given">The directory as given.</param>
    ''' <returns>The inspection; Readable false with no files when the directory does not exist or cannot be listed.</returns>
    Public Function Inspect(given As String) As SolutionFileInspection
        Dim result As SolutionFileInspection = New SolutionFileInspection With {.Files = New List(Of String)()}
        If String.IsNullOrWhiteSpace(given) OrElse Not Directory.Exists(given) Then Return result
        Dim entries As String()
        Try
            entries = Directory.GetFiles(given, "*", SearchOption.TopDirectoryOnly)
        Catch ex As IOException
            Return result
        Catch ex As UnauthorizedAccessException
            Return result
        End Try
        result.Readable = True
        result.Files.AddRange(WithExtension(entries, ".sln"))
        result.Files.AddRange(WithExtension(entries, ".slnx"))
        If result.Files.Count = 1 Then
            result.SuggestedKey = Path.GetFileNameWithoutExtension(result.Files(0))
            result.Command = "extract --solution-key " & result.SuggestedKey & " --solution " & result.Files(0)
        ElseIf result.Files.Count > 1 Then
            result.Ambiguous = True
        End If
        Return result
    End Function

    ''' <summary>
    ''' The refusal kind an inspection answers with: AmbiguousSolutionFile for more than one file, PathNotInMap otherwise.
    ''' </summary>
    ''' <param name="inspection">The inspection.</param>
    ''' <returns>The kind.</returns>
    Public Function KindOf(inspection As SolutionFileInspection) As BridgeRefusalKind
        Return If(inspection.Ambiguous, BridgeRefusalKind.AmbiguousSolutionFile, BridgeRefusalKind.PathNotInMap)
    End Function

    ''' <summary>
    ''' The facts BridgeRefusal.Named needs for the answer: path and roots always; file, key and command for one file; files when ambiguous.
    ''' </summary>
    ''' <param name="inspection">The inspection.</param>
    ''' <param name="path">The directory as given.</param>
    ''' <param name="roots">The mapped roots, comma-separated, or "none".</param>
    ''' <returns>The facts.</returns>
    Public Function Facts(inspection As SolutionFileInspection, path As String, roots As String) As IDictionary(Of String, String)
        Dim result As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
        result("path") = path
        result("roots") = roots
        If inspection.Ambiguous Then
            result("files") = String.Join(", ", inspection.Files)
        ElseIf inspection.Files.Count = 1 Then
            result("file") = inspection.Files(0)
            result("key") = inspection.SuggestedKey
            result("command") = inspection.Command
        End If
        Return result
    End Function

    Private Function WithExtension(entries As String(), extension As String) As List(Of String)
        Dim matched As List(Of String) = New List(Of String)()
        For Each entry As String In entries
            If String.Equals(Path.GetExtension(entry), extension, StringComparison.OrdinalIgnoreCase) Then matched.Add(entry)
        Next
        matched.Sort(StringComparer.Ordinal)
        Return matched
    End Function

End Module

' File: NotInMapReader.vb
' Project: CodeMem.Bridging
' Description: The observed-but-unmapped directories: every extract.log line refused as PathNotInMap or AmbiguousSolutionFile by repoPath, one entry per normalised directory with the latest line's time and origin, dropped once a mapped root contains it, each survivor inspected for its solution files (005 FR-419, FR-420; Q1 as ruled; research R64; data-model §4).
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' Nothing is held in memory and nothing is written: the log is read on every call, so a restarted bridge lists the same directories and a
' directory leaves the list the moment the map holds a root that contains it. Lines with 004's retired kinds are history and are ignored.

Imports System.IO
Imports CodeMem.Extraction

''' <summary>
''' One reader for map_status and extract(stale); the containment question and the suggestion are the resolver's own doors
''' (SolutionScope, SolutionFileSuggestion).
''' </summary>
Public Module NotInMapReader

    Private Const TargetPrefix As String = "repoPath="

    ''' <summary>
    ''' Reads the log.
    ''' </summary>
    ''' <param name="mappedRoots">Every map solution's key and root (MapStatusReader.RootOf).</param>
    ''' <param name="logPath">The log file.</param>
    ''' <returns>The entries ordered by path, or no entries and the reason the log could not be read.</returns>
    Public Function Read(mappedRoots As List(Of (Key As String, Root As String)), logPath As String) As (Entries As List(Of NotInMapEntryEnvelope), ErrorText As String)
        Dim entries As List(Of NotInMapEntryEnvelope) = New List(Of NotInMapEntryEnvelope)()
        If Not File.Exists(logPath) Then Return (entries, Nothing)
        Dim text As String
        Try
            Using stream As FileStream = New FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Using reader As StreamReader = New StreamReader(stream, System.Text.Encoding.UTF8)
                    text = reader.ReadToEnd()
                End Using
            End Using
        Catch ex As IOException
            Return (entries, "extract.log could not be read at '" & logPath & "': " & ex.Message)
        Catch ex As UnauthorizedAccessException
            Return (entries, "extract.log could not be read at '" & logPath & "': " & ex.Message)
        End Try
        Dim observed As Dictionary(Of String, (Utc As String, Origin As String)) = New Dictionary(Of String, (Utc As String, Origin As String))(StringComparer.OrdinalIgnoreCase)
        For Each line As String In text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            Dim columns As String() = line.Split(vbTab(0))
            If columns.Length < 4 Then Continue For
            If columns(3) <> "PathNotInMap" AndAlso columns(3) <> "AmbiguousSolutionFile" Then Continue For
            If Not columns(2).StartsWith(TargetPrefix, StringComparison.Ordinal) Then Continue For
            Dim key As String
            Try
                key = SolutionScope.NormalizeDirectory(columns(2).Substring(TargetPrefix.Length))
            Catch ex As ArgumentException
                Continue For
            End Try
            observed(key) = (columns(0), columns(1))
        Next
        For Each pair As KeyValuePair(Of String, (Utc As String, Origin As String)) In observed
            If IsContained(pair.Key, mappedRoots) Then Continue For
            Dim inspection As SolutionFileInspection = SolutionFileSuggestion.Inspect(pair.Key)
            entries.Add(New NotInMapEntryEnvelope With {
                .Path = pair.Key,
                .Verdict = "not_in_map",
                .ObservedUtc = pair.Value.Utc,
                .Origin = pair.Value.Origin,
                .SolutionFiles = inspection.Files,
                .SuggestedKey = inspection.SuggestedKey,
                .Command = inspection.Command,
                .Note = NoteOf(inspection)})
        Next
        entries.Sort(Function(a As NotInMapEntryEnvelope, b As NotInMapEntryEnvelope) String.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase))
        Return (entries, Nothing)
    End Function

    Private Function IsContained(directory As String, mappedRoots As List(Of (Key As String, Root As String))) As Boolean
        For Each mapped As (Key As String, Root As String) In mappedRoots
            If SolutionScope.Resolve(mapped.Root, mapped.Root).ContainsDirectory(directory) Then Return True
        Next
        Return False
    End Function

    Private Function NoteOf(inspection As SolutionFileInspection) As String
        If Not inspection.Readable Then Return "directory could not be read now"
        If inspection.Ambiguous Then Return "more than one solution file"
        Return Nothing
    End Function

End Module

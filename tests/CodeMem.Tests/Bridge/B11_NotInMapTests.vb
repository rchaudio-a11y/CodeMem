' File: B11_NotInMapTests.vb
' Project: CodeMem.Tests
' Description: US3 (feature 005): map_status lists the directories extract has refused as not in the map, read from extract.log - once per directory, the later line winning, across bridge instances, gone once a mapped root contains it, 004-era lines ignored, an unreadable log reported; stale lists them and never launches for one (FR-419, FR-420; Q1 as ruled; STOP 1 decision 8).
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' The log beside the test executable is shared by every extract fact of the suite: each fact here uses its own unique directory and asserts
' only on entries whose path is that directory (analyze A2). Hand-written lines carry the seven columns of contracts/cli-config-hook.md §5.
' RED:   2026-09-17 (T026) behind B10's compile failure (no ExtractRequest.SolutionPath); (T027, the first build with the shape) 8 of 8 red for
'        the stated reason - no reader: (1), (2), (3), (5) Assert.Single on an empty notInMap; (4) "the directory was not listed before the
'        add"; (6) notInMapError null where a string was expected; (7) 0 of 200 listed; (8) KeyNotFoundException - the stale result had no
'        notInMap. (5) was given its control at T027 so it could not pass on the empty list.
' GREEN: 2026-09-17 (T028) 8 of 8 on the first build with NotInMapReader (one name fix on the way: the local text shadowed System.Text).
' FIRE:  2026-09-17 (T028) the reader made to forget - a birth stamp set by each BridgeTools instance and every line older than it dropped,
'        the per-instance memory Q1 ruled out -> (3) red (Assert.Single on the fresh host's empty list), (4), (5) and (7) red with it (their
'        hosts were born after their lines); (1), (2), (6), (8) green on the same host. Reverted from byte copies -> 8 of 8.

Imports System.Diagnostics
Imports System.IO
Imports System.Text.Json
Imports CodeMem.Bridging
Imports Xunit

''' <summary>
''' Fast facts through the in-process host over ExtractScenario's map (Sample alone) and the shared added map; nothing here launches.
''' </summary>
<Collection("ExtractLog")>
Public Class B11_NotInMapTests
    Implements IClassFixture(Of ExtractScenario)

    Private ReadOnly _scenario As ExtractScenario
    Private ReadOnly _added As AddedSolutionFixture

    ''' <summary>
    ''' Receives the shared extraction and the shared add.
    ''' </summary>
    ''' <param name="scenario">The class fixture.</param>
    ''' <param name="added">The collection fixture.</param>
    Public Sub New(scenario As ExtractScenario, added As AddedSolutionFixture)
        _scenario = scenario
        _added = added
    End Sub

    ''' <summary>
    ''' (1) A directory refused as not in the map is listed once under notInMap with verdict not_in_map, the origin, a time, its one solution
    ''' file, the suggested key and the command (FR-419).
    ''' </summary>
    <Fact>
    Public Sub ARefusedDirectoryIsListedOnceWithKeyAndCommand()
        Dim dir As String = _scenario.UniquePathUnderNoRoot()
        Directory.CreateDirectory(dir)
        Dim file As String = Path.Combine(dir, "Fresh.slnx")
        IO.File.WriteAllText(file, "<Solution />")
        Dim host As BridgeHost = _scenario.Host(True, True)
        Refuse(host, dir)
        Dim entry As JsonElement = Assert.Single(EntriesFor(host, dir))
        Assert.Equal("not_in_map", entry.GetProperty("verdict").GetString())
        Assert.Equal("tool", entry.GetProperty("origin").GetString())
        Assert.False(String.IsNullOrEmpty(entry.GetProperty("observedUtc").GetString()), "no observedUtc")
        Assert.Equal(New String() {file}, Strings(entry.GetProperty("solutionFiles")))
        Assert.Equal("Fresh", entry.GetProperty("suggestedKey").GetString())
        Assert.Equal("extract --solution-key Fresh --solution " & file, entry.GetProperty("command").GetString())
        Assert.Equal(JsonValueKind.Null, entry.GetProperty("note").ValueKind)
    End Sub

    ''' <summary>
    ''' (2) Two refusals for one directory are one entry carrying the later line's time and origin; the log holds both lines.
    ''' </summary>
    <Fact>
    Public Sub TwoRefusalsForOneDirectoryAreOneEntryWithTheLaterTime()
        Dim dir As String = _scenario.UniquePathUnderNoRoot()
        Directory.CreateDirectory(dir)
        Dim host As BridgeHost = _scenario.Host(True, True)
        Refuse(host, dir)
        Dim firstUtc As String = Assert.Single(EntriesFor(host, dir)).GetProperty("observedUtc").GetString()
        Dim hook As ExtractResult = host.Door.Run(New ExtractRequest With {.Origin = ExtractOrigin.GreenBuild, .RepoPath = dir})
        Assert.Equal("PathNotInMap", hook.Refusal.Kind)
        Dim later As JsonElement = Assert.Single(EntriesFor(host, dir))
        Assert.Equal("green_build", later.GetProperty("origin").GetString())
        Assert.True(String.CompareOrdinal(later.GetProperty("observedUtc").GetString(), firstUtc) >= 0, "the later line did not win")
        Assert.Equal(2, LogLines.Containing(dir).Count)
    End Sub

    ''' <summary>
    ''' (3) A fresh bridge instance over the same log lists the same directories: nothing is held in memory (Q1 as ruled).
    ''' </summary>
    <Fact>
    Public Sub AFreshInstanceListsTheSameDirectories()
        Dim dir As String = _scenario.UniquePathUnderNoRoot()
        Directory.CreateDirectory(dir)
        Refuse(_scenario.Host(True, True), dir)
        Dim restarted As BridgeHost = New BridgeHost(_scenario.Config(True, True))
        Assert.Single(EntriesFor(restarted, dir))
    End Sub

    ''' <summary>
    ''' (4) Once a mapped root contains the directory the entry is gone and the log lines stand (FR-419): the shared fixture refused the
    ''' Fresh copy by path, saw it listed, then added it by key with its path.
    ''' </summary>
    <Fact>
    Public Sub MappingTheSolutionRemovesTheEntryAndKeepsTheLine()
        Assert.True(_added.ListedBeforeAdding, "the directory was not listed before the add")
        Dim before As Integer = LogLines.Containing(_added.Copy.Directory).Count
        Assert.True(before >= 2, "lines for the directory: " & before)
        Dim host As BridgeHost = New BridgeHost(_added.ConfigPath)
        Assert.Empty(EntriesFor(host, _added.Copy.Directory))
        Assert.Equal(before, LogLines.Containing(_added.Copy.Directory).Count)
    End Sub

    ''' <summary>
    ''' (5) A 004-era line (resolution PathNotRegistered) is history and is not listed, while a PathNotInMap line written the same way is
    ''' (the control that keeps this guard from passing on an empty list).
    ''' </summary>
    <Fact>
    Public Sub AFourOhFourEraLineIsIgnored()
        Dim history As String = _scenario.UniquePathUnderNoRoot()
        Dim current As String = _scenario.UniquePathUnderNoRoot()
        AppendLines(Line("tool", "repoPath=" & history, "PathNotRegistered"), Line("tool", "repoPath=" & current, "PathNotInMap"))
        Dim host As BridgeHost = _scenario.Host(True, True)
        Assert.Single(EntriesFor(host, current))
        Assert.Empty(EntriesFor(host, history))
        Assert.Single(LogLines.Containing(history))
    End Sub

    ''' <summary>
    ''' (6) A log that cannot be read is reported in notInMapError, the list empty and the solution entries intact.
    ''' </summary>
    <Fact>
    Public Sub AnUnreadableLogIsReportedAndEntriesStand()
        Dim host As BridgeHost = _scenario.Host(True, True)
        Using locked As FileStream = New FileStream(LogLines.LogPath(), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None)
            Dim reply As BridgeReply = host.Invoke("map_status", Args())
            Assert.False(reply.IsError, reply.Text)
            Dim root As JsonElement = reply.Root()
            Assert.Equal(JsonValueKind.String, root.GetProperty("notInMapError").ValueKind)
            Assert.Contains("extract.log", root.GetProperty("notInMapError").GetString(), StringComparison.Ordinal)
            Assert.Equal(0, root.GetProperty("notInMap").GetArrayLength())
            Assert.Equal(1, root.GetProperty("entries").GetArrayLength())
            Assert.Equal("Sample", root.GetProperty("entries")(0).GetProperty("solutionKey").GetString())
        End Using
    End Sub

    ''' <summary>
    ''' (7) map_status with two hundred observed directories in the log answers under three seconds (SC; the directories need not exist).
    ''' </summary>
    <Fact>
    Public Sub MapStatusWithTwoHundredLogLinesIsUnderThreeSeconds()
        Dim parent As String = _scenario.UniquePathUnderNoRoot()
        Dim lines As List(Of String) = New List(Of String)()
        For i As Integer = 1 To 200
            lines.Add(Line("tool", "repoPath=" & Path.Combine(parent, "d" & i.ToString(Globalization.CultureInfo.InvariantCulture)), "PathNotInMap"))
        Next
        AppendLines(lines.ToArray())
        Dim host As BridgeHost = _scenario.Host(True, True)
        Dim watch As Stopwatch = Stopwatch.StartNew()
        Dim reply As BridgeReply = host.Invoke("map_status", Args())
        watch.Stop()
        Assert.False(reply.IsError, reply.Text)
        Dim prefix As String = AddedSolutionFixture.NormalisedDirectory(parent)
        Dim listed As Integer = 0
        For Each entry As JsonElement In reply.Root().GetProperty("notInMap").EnumerateArray()
            If entry.GetProperty("path").GetString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase) Then listed += 1
        Next
        Assert.Equal(200, listed)
        Assert.True(watch.ElapsedMilliseconds < 3000, "map_status took " & watch.ElapsedMilliseconds & " ms")
    End Sub

    ''' <summary>
    ''' (8) extract(stale) lists the observed directories under notInMap and never launches for one (FR-420).
    ''' </summary>
    <Fact>
    Public Sub StaleListsObservedDirectoriesAndNeverLaunchesForThem()
        Dim dir As String = _scenario.UniquePathUnderNoRoot()
        Directory.CreateDirectory(dir)
        Dim host As BridgeHost = _scenario.Host(True, True)
        Refuse(host, dir)
        Dim reply As BridgeReply = host.Invoke("extract", Args("stale", True))
        Assert.False(reply.IsError, reply.Text)
        Dim key As String = AddedSolutionFixture.NormalisedDirectory(dir)
        Dim listed As Boolean = False
        For Each entry As JsonElement In reply.Root().GetProperty("notInMap").EnumerateArray()
            If String.Equals(entry.GetProperty("path").GetString(), key, StringComparison.OrdinalIgnoreCase) Then listed = True
        Next
        Assert.True(listed, "stale did not list " & dir)
        For Each request As LaunchRequest In host.Launcher.Requests
            Assert.False(request.SolutionPath.StartsWith(key, StringComparison.OrdinalIgnoreCase), "launched for " & request.SolutionPath)
        Next
    End Sub

    Private Shared Sub Refuse(host As BridgeHost, dir As String)
        Dim reply As BridgeReply = host.Invoke("extract", Args("repoPath", dir))
        Assert.True(reply.IsError, "not refused: " & reply.Text)
        Assert.Equal("PathNotInMap", reply.Root().GetProperty("refusal").GetProperty("kind").GetString())
    End Sub

    Private Shared Function EntriesFor(host As BridgeHost, dir As String) As List(Of JsonElement)
        Dim reply As BridgeReply = host.Invoke("map_status", Args())
        Assert.False(reply.IsError, reply.Text)
        Dim key As String = AddedSolutionFixture.NormalisedDirectory(dir)
        Dim result As List(Of JsonElement) = New List(Of JsonElement)()
        For Each entry As JsonElement In reply.Root().GetProperty("notInMap").EnumerateArray()
            If String.Equals(entry.GetProperty("path").GetString(), key, StringComparison.OrdinalIgnoreCase) Then result.Add(entry.Clone())
        Next
        Return result
    End Function

    Private Shared Function Line(origin As String, target As String, resolution As String) As String
        Return String.Join(vbTab, DateTime.UtcNow.ToString("o", Globalization.CultureInfo.InvariantCulture), origin, target, resolution, "passed", "-", "refused: (a line written by B11 for one fact)")
    End Function

    Private Shared Sub AppendLines(ParamArray lines As String())
        Using stream As FileStream = New FileStream(LogLines.LogPath(), FileMode.Append, FileAccess.Write, FileShare.Read)
            Using writer As StreamWriter = New StreamWriter(stream)
                For Each line As String In lines
                    writer.Write(line & vbLf)
                Next
            End Using
        End Using
    End Sub

    Private Shared Function Strings(array As JsonElement) As String()
        Dim result As List(Of String) = New List(Of String)()
        For Each item As JsonElement In array.EnumerateArray()
            result.Add(item.GetString())
        Next
        Return result.ToArray()
    End Function

    Private Shared Function Args(ParamArray pairs As Object()) As Dictionary(Of String, Object)
        Dim result As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            result(CStr(pairs(i))) = pairs(i + 1)
        Next
        Return result
    End Function

End Class

' File: ExtractDoor.vb
' Project: CodeMem.Bridging
' Description: The one door for the extract verb, every origin: cardinality, configuration, the two gates, resolution, one extraction at a time, the launch, the run read back, the log (FR-325..FR-334, FR-351; Article XII; research R48).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-15 (T037): the skeleton behind B05's and B06's Reds. (T042): the door filled in this order (INC2, CON4, FR-334): cardinality ->
' configuration -> gates (before resolution, STOP 1 ruling 2) -> the store -> the map, read once and closed before the launch -> the
' semaphore (one extraction per process, FR-332 as scoped) -> the extractor's existence -> the launch under the 540 s budget (TIM1) -> the
' line -> the run read back on a fresh read-only open -> the log line -> the result.
' 2026-09-17 (feature 005, T015): RunStale reads map_status from the map alone (no registry); Run's store stage leaves at T021 with the resolver.
' 2026-09-17 (feature 005, T021): Run's store stage is gone - the resolver reads the map's solutions rows (R65) and answers not in the
' map from the directory's own solution files (R66). The map is the only file the door opens.
' 2026-09-17 (feature 005, T027): solutionPath beside solutionKey (R67) - cardinality names it, the log's target column reads
' solutionKey=<key> solution=<path>, the launch line is unchanged in shape.
' 2026-09-17 (feature 005, T028): RunStale carries map_status's notInMap list and never launches for an entry of it (FR-420).

Imports System.Diagnostics
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports CodeMem.Core
Imports Microsoft.Data.Sqlite

''' <summary>
''' Run and RunStale are the only two entries; the MCP tool, the extract command line and the hook entry all call them.
''' </summary>
Public Class ExtractDoor

    ''' <summary>The child's budget inside the hook's 600 s (TIM1).</summary>
    Public Shared ReadOnly Budget As TimeSpan = TimeSpan.FromSeconds(540)

    Private Shared ReadOnly Running As SemaphoreSlim = New SemaphoreSlim(1, 1)
    Private Shared ReadOnly RunIdPattern As Regex = New Regex("\brun_id=(\d+)", RegexOptions.Compiled)
    Private Shared _runningKey As String

    Private ReadOnly _configPath As String
    Private ReadOnly _launcher As IExtractorLauncher

    ''' <summary>
    ''' Binds the door to a configuration file and a launcher.
    ''' </summary>
    ''' <param name="configPath">The configuration file read on every call, or Nothing for the file beside the executable.</param>
    ''' <param name="launcher">The launcher.</param>
    Public Sub New(configPath As String, launcher As IExtractorLauncher)
        _configPath = configPath
        _launcher = launcher
    End Sub

    ''' <summary>
    ''' One extraction for one target.
    ''' </summary>
    ''' <param name="request">The request.</param>
    ''' <returns>The result, refused or run; one log line either way.</returns>
    Public Function Run(request As ExtractRequest) As ExtractResult
        Dim watch As Stopwatch = Stopwatch.StartNew()
        Dim asGiven As String = If(request.LogTarget, TargetAsGiven(request))
        Dim result As ExtractResult = New ExtractResult With {
            .ReadAtUtc = BridgeJson.NowUtc(),
            .Origin = OriginText(request.Origin),
            .Target = New ExtractTargetEnvelope With {.AsGiven = TargetAsGiven(request)},
            .Gate = "-"}
        Dim resolution As String = "-"
        Dim config As BridgeConfig = Nothing
        Try
            RequireOneTarget(request)
            config = BridgeConfigFile.Load(_configPath)
            result.Target.MapPath = config.MapPath
            Dim gate As BridgeRefusal = ExtractGates.Check(config, request.Origin)
            If gate IsNot Nothing Then
                result.Gate = ExtractGates.GateName(config, request.Origin)
                Throw New BridgeRefusalException(gate)
            End If
            result.Gate = "passed"
            Dim target As ResolvedTarget
            Using map As MapDatabase = MapAccess.OpenRead(config)
                Try
                    target = TargetResolver.Resolve(request, config, map)
                Catch ex As BridgeRefusalException
                    resolution = ex.Refusal.Kind.ToString()
                    Throw
                End Try
                map.EndRead()
            End Using
            result.Target.ResolvedKey = target.SolutionKey
            result.Target.SolutionPath = target.SolutionPath
            resolution = target.SolutionKey
            If Not Running.Wait(0) Then
                Throw Refuse(BridgeRefusalKind.ExtractionRunning, "key", If(_runningKey, "?"))
            End If
            Try
                _runningKey = target.SolutionKey
                Dim extractorPath As String = If(String.IsNullOrWhiteSpace(config.ExtractorPath), Path.Combine(AppContext.BaseDirectory, "CodeMem.Extractor.dll"), config.ExtractorPath)
                If Not File.Exists(extractorPath) Then Throw Refuse(BridgeRefusalKind.ExtractorNotFound, "extractorPath", extractorPath, "configPath", config.SourcePath)
                Dim launch As LaunchResult = _launcher.Launch(New LaunchRequest With {
                    .ExtractorPath = extractorPath, .SolutionPath = target.SolutionPath, .MapPath = config.MapPath, .SolutionKey = target.SolutionKey, .Budget = Budget})
                result.Launched = True
                result.ExitCode = launch.ExitCode
                result.TimedOut = launch.TimedOut
                If launch.TimedOut Then
                    result.Line = "extractor killed after " & CInt(Budget.TotalSeconds) & " s"
                    Dim timedOut As BridgeRefusal = BridgeRefusal.Named(BridgeRefusalKind.ChildTimedOut, Facts("key", target.SolutionKey, "budget", CInt(Budget.TotalSeconds).ToString()))
                    result.Refusal = New ExtractRefusalEnvelope With {.Kind = timedOut.Kind.ToString(), .Text = timedOut.Text}
                Else
                    result.Line = LineOf(launch)
                    If launch.ExitCode.HasValue AndAlso launch.ExitCode.Value = 0 Then result.Run = ReadRun(config, result.Line)
                End If
            Finally
                _runningKey = Nothing
                Running.Release()
            End Try
        Catch ex As BridgeRefusalException
            result.Refusal = New ExtractRefusalEnvelope With {.Kind = ex.Refusal.Kind.ToString(), .Text = ex.Refusal.Text}
        Catch ex As SqliteException
            Dim translated As BridgeRefusal = If(config Is Nothing, BridgeRefusal.Named(BridgeRefusalKind.Unopenable, Facts("role", "map", "path", "?", "driver", ex.Message)), MapAccess.Translate(ex, config).Refusal)
            result.Refusal = New ExtractRefusalEnvelope With {.Kind = translated.Kind.ToString(), .Text = translated.Text}
        End Try
        watch.Stop()
        result.ElapsedMs = watch.ElapsedMilliseconds
        Dim logLine As String = If(result.Launched, If(result.Line, ""), If(result.Refusal Is Nothing, "", "refused: " & result.Refusal.Text))
        Dim logged As LogOutcome = ExtractLog.Append(result.ReadAtUtc, result.Origin, asGiven, resolution, result.Gate, ExitColumn(result), logLine)
        result.Logged = logged.Logged
        result.LogError = logged.Reason
        Return result
    End Function

    ''' <summary>
    ''' Every bound solution whose map_status verdict is behind, dirty or diverged, extracted in sequence through <see cref="Run"/>, one log
    ''' line each and no header (FR-329, INC3); the rest listed with their verdicts.
    ''' </summary>
    ''' <param name="request">The request, with Stale true.</param>
    ''' <returns>The result.</returns>
    Public Function RunStale(request As ExtractRequest) As StaleResult
        Dim result As StaleResult = New StaleResult With {.ReadAtUtc = BridgeJson.NowUtc(), .Origin = OriginText(request.Origin), .Gate = "-", .Considered = New List(Of StaleEntryEnvelope)(), .NotInMap = New List(Of NotInMapEntryEnvelope)()}
        Try
            RequireOneTarget(request)
            Dim config As BridgeConfig = BridgeConfigFile.Load(_configPath)
            Dim gate As BridgeRefusal = ExtractGates.Check(config, request.Origin)
            If gate IsNot Nothing Then
                result.Gate = ExtractGates.GateName(config, request.Origin)
                Throw New BridgeRefusalException(gate)
            End If
            result.Gate = "passed"
            Dim status As MapStatusEnvelope
            Using map As MapDatabase = MapAccess.OpenRead(config)
                status = MapStatusReader.Read(config, map)
                map.EndRead()
            End Using
            result.NotInMap = status.NotInMap
            For Each entry As MapStatusEntryEnvelope In status.Entries
                Dim item As StaleEntryEnvelope = New StaleEntryEnvelope With {.SolutionKey = entry.SolutionKey, .Verdict = entry.Verdict}
                If entry.Verdict = "behind" OrElse entry.Verdict = "dirty" OrElse entry.Verdict = "diverged" Then
                    item.Result = Run(New ExtractRequest With {.Origin = request.Origin, .SolutionKey = entry.SolutionKey, .LogTarget = "stale"})
                    item.Extracted = item.Result.Launched
                    If item.Extracted Then result.ExtractedCount += 1
                End If
                result.Considered.Add(item)
            Next
        Catch ex As BridgeRefusalException
            result.Refusal = New ExtractRefusalEnvelope With {.Kind = ex.Refusal.Kind.ToString(), .Text = ex.Refusal.Text}
            ExtractLog.Append(result.ReadAtUtc, result.Origin, "stale", "-", result.Gate, "-", "refused: " & ex.Refusal.Text)
        End Try
        Return result
    End Function

    Private Shared Sub RequireOneTarget(request As ExtractRequest)
        Dim named As Integer = 0
        If request.SolutionKey IsNot Nothing Then named += 1
        If request.RepoPath IsNot Nothing Then named += 1
        If request.Stale Then named += 1
        If named <> 1 Then Throw Refuse(BridgeRefusalKind.TargetMissing)
        If request.SolutionPath IsNot Nothing AndAlso request.SolutionKey Is Nothing Then Throw Refuse(BridgeRefusalKind.TargetMissing)
    End Sub

    Private Shared Function LineOf(launch As LaunchResult) As String
        Dim stdoutLine As String = LastLine(launch.StandardOutput)
        Dim stderrLine As String = FirstLine(launch.StandardError)
        If launch.ExitCode.HasValue AndAlso launch.ExitCode.Value = 0 Then Return If(stdoutLine, If(stderrLine, ""))
        Return If(stderrLine, If(stdoutLine, ""))
    End Function

    Private Shared Function LastLine(text As String) As String
        If String.IsNullOrEmpty(text) Then Return Nothing
        Dim lines As String() = text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        For i As Integer = lines.Length - 1 To 0 Step -1
            If lines(i).Trim().Length > 0 Then Return lines(i).Trim()
        Next
        Return Nothing
    End Function

    Private Shared Function FirstLine(text As String) As String
        If String.IsNullOrEmpty(text) Then Return Nothing
        For Each line As String In text.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            If line.Trim().Length > 0 Then Return line.Trim()
        Next
        Return Nothing
    End Function

    Private Shared Function ReadRun(config As BridgeConfig, line As String) As ExtractRunEnvelope
        If line Is Nothing Then Return Nothing
        Dim match As Match = RunIdPattern.Match(line)
        If Not match.Success Then Return Nothing
        Dim runId As Long = Long.Parse(match.Groups(1).Value, Globalization.CultureInfo.InvariantCulture)
        Try
            Using map As MapDatabase = MapAccess.OpenRead(config)
                Dim run As RunRecord = ExtractRunsRepository.ReadById(map, runId)
                map.EndRead()
                If run Is Nothing Then Return Nothing
                Return New ExtractRunEnvelope With {
                    .RunId = run.Id,
                    .SymbolsObserved = run.Counts.SymbolsObserved,
                    .SymbolsMatched = run.Counts.SymbolsMatched,
                    .SymbolsReactivated = run.Counts.SymbolsReactivated,
                    .SymbolsNew = run.Counts.SymbolsNew,
                    .SymbolsRetired = run.Counts.SymbolsRetired,
                    .RegistryActiveBefore = run.Counts.RegistryActiveBefore,
                    .NotesOrphaned = run.Counts.NotesOrphaned,
                    .RenameCandidates = run.Counts.RenameCandidates,
                    .UnaccountedObserved = run.Counts.UnaccountedObserved,
                    .UnaccountedRegistry = run.Counts.UnaccountedRegistry}
            End Using
        Catch ex As BridgeRefusalException
            Return Nothing
        Catch ex As SqliteException
            Return Nothing
        End Try
    End Function

    Private Shared Function ExitColumn(result As ExtractResult) As String
        If Not result.Launched Then Return "-"
        If result.TimedOut Then Return "timeout"
        Return If(result.ExitCode.HasValue, result.ExitCode.Value.ToString(Globalization.CultureInfo.InvariantCulture), "-")
    End Function

    ''' <summary>
    ''' The wire text of an origin: tool, green_build or manual.
    ''' </summary>
    ''' <param name="origin">The origin.</param>
    ''' <returns>The text.</returns>
    Public Shared Function OriginText(origin As ExtractOrigin) As String
        Select Case origin
            Case ExtractOrigin.GreenBuild
                Return "green_build"
            Case ExtractOrigin.Manual
                Return "manual"
            Case Else
                Return "tool"
        End Select
    End Function

    Private Shared Function TargetAsGiven(request As ExtractRequest) As String
        If request.Stale Then Return "stale"
        If request.SolutionKey IsNot Nothing Then Return "solutionKey=" & request.SolutionKey & If(request.SolutionPath Is Nothing, "", " solution=" & request.SolutionPath)
        If request.RepoPath IsNot Nothing Then Return "repoPath=" & request.RepoPath
        Return "-"
    End Function

    Private Shared Function Refuse(kind As BridgeRefusalKind, ParamArray pairs As String()) As BridgeRefusalException
        Return New BridgeRefusalException(BridgeRefusal.Named(kind, Facts(pairs)))
    End Function

    Private Shared Function Facts(ParamArray pairs As String()) As IDictionary(Of String, String)
        Dim result As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            result(pairs(i)) = pairs(i + 1)
        Next
        Return result
    End Function

End Class

' File: B06_HookEntryTests.vb
' Project: CodeMem.Tests
' Description: The executable's hook entry with the spike's payloads piped to stdin: the green payload resolves and answers, the failure payload does nothing, cd-prefix and positional targets parsed after options, a quoted path, a cross-repository path, ambiguity never a fallback, a named path under no root never replaced by the session directory, interrupted, gates off reported, not JSON, not a dotnet command, and the entry's speed (FR-335, FR-336, FR-338, spec Q5/Q6, COR2, G1).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' The facts that only need the resolved key name run against a config whose extractorPath does not exist: resolution succeeds, the launch
' is refused as ExtractorNotFound, and the answer line names the key without running an extraction; (1) runs the real extractor.
' RED:   2026-09-15 (T039) with the assembly compiling again (T037, T041): 11 red - the hook branch of Program.vb threw
'        NotImplementedException, so every entry exited non-zero where 0 was asserted; (8) green (it times four non-cases without asserting
'        their outcome). The named Red.
' GREEN: 2026-09-16 (T045) after HookRequest.Parse and HookEntry.Run were filled in: (1)-(8) green with B05 and B01 - 39 passed / 0 failed
'        in 34 s. (3d)'s Seed copies the fixture tree only when the target is absent, so a second scenario in the same host reuses it.
' FIRE:  2026-09-16 (T046) each fault injected alone, its one fact run, the file restored from a byte copy: (c) HookRequest accepted
'        PostToolUseFailure - (2) red (the failure payload reached the door and answered "codemem extract (green build, ...)"); (e) the
'        first of two candidates taken - (3e) red (no "more than one target"); (h) HookEntry retried the door with the payload's cwd on
'        PathNotRegistered - (3f) red (the answer resolved Sample instead of naming the path; G1). After the last revert: B05 + B06
'        33 passed / 0 failed in 26 s.

Imports System.Diagnostics
Imports System.IO
Imports System.Text.Json
Imports System.Text.Json.Nodes
Imports CodeMem.Extraction
Imports LibGit2Sharp
Imports Xunit
Imports Xunit.Abstractions

''' <summary>
''' Every fact spawns <c>CodeMem.Bridge.dll hook --config &lt;path&gt;</c> through BridgeProcess.RunOnce and reads the one JSON line it prints.
''' </summary>
<Collection("ExtractLog")>
Public Class B06_HookEntryTests
    Implements IClassFixture(Of ExtractScenario)

    Private ReadOnly _scenario As ExtractScenario
    Private ReadOnly _output As ITestOutputHelper

    ''' <summary>
    ''' Receives the shared extraction and the output sink.
    ''' </summary>
    ''' <param name="scenario">The class fixture.</param>
    ''' <param name="output">Where timings are printed.</param>
    Public Sub New(scenario As ExtractScenario, output As ITestOutputHelper)
        _scenario = scenario
        _output = output
    End Sub

    ''' <summary>
    ''' (1) The green payload, its command naming the copy's Sample.sln, with both gates on and the real launcher: exit 0, one JSON line
    ''' whose additionalContext names Sample and exit 0, one log line with origin green_build.
    ''' </summary>
    <Fact>
    Public Sub GreenPayloadResolvesAndAnswers()
        Dim marker As String = _scenario.Copy.Directory
        Dim before As Integer = LogLines.Containing(marker).Count
        Dim reply As ProcessReply = Hook(_scenario.Config(_scenario.Registry, True, True), Payload("posttooluse-green.json", "dotnet build """ & _scenario.Copy.SolutionPath & """ -c Debug --nologo -v q"))
        Assert.Equal(0, reply.ExitCode)
        Dim context As String = ContextOf(reply)
        Assert.StartsWith("codemem extract (green build,", context)
        Assert.Contains("Sample", context)
        Assert.Contains("exit 0", context)
        Dim lines As List(Of String()) = LogLines.Containing(marker)
        Assert.Equal(before + 1, lines.Count)
        Assert.Equal("green_build", lines(lines.Count - 1)(1))
    End Sub

    ''' <summary>
    ''' (2) The failure payload: exit 0, "is not a Bash PostToolUse", no log line, no launch, the map's hash unchanged.
    ''' </summary>
    <Fact>
    Public Sub FailurePayloadDoesNothing()
        Dim before As Integer = LogLines.Containing("").Count
        Dim hash As String = MapSnapshot.FileBytesHash(_scenario.Map.Path)
        Dim reply As ProcessReply = Hook(_scenario.Config(_scenario.Registry, True, True), File.ReadAllText(Fixture("posttoolusefailure-red.json")))
        Assert.Equal(0, reply.ExitCode)
        Assert.Contains("is not a Bash PostToolUse", ContextOf(reply))
        Assert.Equal(before, LogLines.Containing("").Count)
        Assert.Equal(hash, MapSnapshot.FileBytesHash(_scenario.Map.Path))
        Print("(2)", reply)
    End Sub

    ''' <summary>
    ''' (3) cd X &amp;&amp; dotnet test Y.vbproj: the project file's directory, under the root, resolves to Sample.
    ''' </summary>
    <Fact>
    Public Sub CdPrefixAndProjectPathResolveTheProjectDirectory()
        Dim reply As ProcessReply = Hook(NoExtractorConfig(_scenario.Registry), Payload("posttooluse-cd-and-test.json", "cd " & _scenario.Copy.Directory & " && dotnet test Sample.Lib\Sample.Lib.vbproj --nologo"))
        Assert.Equal(0, reply.ExitCode)
        Assert.Contains(": Sample:", ContextOf(reply))
    End Sub

    ''' <summary>
    ''' (3b) Options before the path still name it (COR2): dotnet build -c Debug "Sample.sln" --nologo resolves to Sample.
    ''' </summary>
    <Fact>
    Public Sub OptionsBeforeThePathStillNameIt()
        Dim reply As ProcessReply = Hook(NoExtractorConfig(_scenario.Registry), Payload("posttooluse-green.json", "dotnet build -c Debug """ & _scenario.Copy.SolutionPath & """ --nologo"))
        Assert.Equal(0, reply.ExitCode)
        Assert.Contains(": Sample:", ContextOf(reply))
    End Sub

    ''' <summary>
    ''' (3c) A quoted path with a space is one token (COR2): a copy under a directory with a space resolves to its own key.
    ''' </summary>
    <Fact>
    Public Sub QuotedPathsAreOneToken()
        Dim spaced As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "with space " & Guid.NewGuid().ToString("N"))
        Using registry As RegistryFixture = New RegistryFixture()
            Try
                Dim key As String = Seed(registry, spaced, "Spaced")
                Dim solutionPath As String = Path.Combine(spaced, "Sample", "Sample.sln")
                Dim reply As ProcessReply = Hook(NoExtractorConfig(registry), Payload("posttooluse-green.json", "dotnet build """ & solutionPath & """ --nologo"))
                Assert.Equal(0, reply.ExitCode)
                Assert.Contains(": " & key & ":", ContextOf(reply))
            Finally
                If Directory.Exists(spaced) Then GitFixture.RemoveGitDirectory(spaced)
                If Directory.Exists(spaced) Then Directory.Delete(spaced, True)
            End Try
        End Using
    End Sub

    ''' <summary>
    ''' (3d) A cross-repository path resolves to its own root, never the session's (COR2, Q6): cwd is the first root, the command names the
    ''' second copy's solution.
    ''' </summary>
    <Fact>
    Public Sub ACrossRepositoryPathResolvesToItsOwnRoot()
        Using other As FixtureCopy = New FixtureCopy()
            Using registry As RegistryFixture = New RegistryFixture()
                Dim key As String = Seed(registry, other.ParentDirectory, "Other")
                Dim reply As ProcessReply = Hook(NoExtractorConfig(registry), Payload("posttooluse-green.json", "dotnet build """ & other.SolutionPath & """", _scenario.Root))
                Assert.Equal(0, reply.ExitCode)
                Dim context As String = ContextOf(reply)
                Assert.Contains(": " & key & ":", context)
                Assert.DoesNotContain(": Sample:", context)
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (3e) Two candidates are ambiguous, never a fallback (COR2): more than one target named, no launch, no fallback to cwd.
    ''' </summary>
    <Fact>
    Public Sub TwoCandidatesAreAmbiguousNeverFallback()
        Dim before As Integer = LogLines.Containing("").Count
        Dim reply As ProcessReply = Hook(_scenario.Config(_scenario.Registry, True, True), Payload("posttooluse-green.json", "dotnet build A.sln B.sln", _scenario.Root))
        Assert.Equal(0, reply.ExitCode)
        Dim context As String = ContextOf(reply)
        Assert.Contains("more than one target", context)
        Assert.Contains("A.sln", context)
        Assert.Contains("B.sln", context)
        Assert.DoesNotContain("Sample", context)
        Assert.Equal(before, LogLines.Containing("").Count)
    End Sub

    ''' <summary>
    ''' (3f) A named path under no registered root is refused naming that path and the session directory is never tried in its place
    ''' (Q6 as ruled, G1): cwd is the registered root, no new run row, the hash unchanged, one log line resolved as PathNotRegistered.
    ''' </summary>
    <Fact>
    Public Sub ANamedPathUnderNoRootIsRefusedNeverTheSessionDirectory()
        Dim nowhere As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "nowhere-" & Guid.NewGuid().ToString("N"))
        Dim runsBefore As Integer = MapQueries.ReadRuns(_scenario.Map.Path).Count
        Dim hash As String = MapSnapshot.FileBytesHash(_scenario.Map.Path)
        Dim reply As ProcessReply = Hook(_scenario.Config(_scenario.Registry, True, True), Payload("posttooluse-green.json", "dotnet build """ & Path.Combine(nowhere, "Nowhere.sln") & """", _scenario.Root))
        Assert.Equal(0, reply.ExitCode)
        Dim context As String = ContextOf(reply)
        Assert.Contains("No registered solution's repository root contains", context)
        Assert.Contains(nowhere, context)
        Assert.DoesNotContain("Sample", context)
        Assert.Equal(runsBefore, MapQueries.ReadRuns(_scenario.Map.Path).Count)
        Assert.Equal(hash, MapSnapshot.FileBytesHash(_scenario.Map.Path))
        Dim line As String() = Assert.Single(LogLines.Containing(nowhere))
        Assert.Equal("PathNotRegistered", line(3))
    End Sub

    ''' <summary>
    ''' (4) An interrupted tool response does nothing.
    ''' </summary>
    <Fact>
    Public Sub InterruptedPayloadDoesNothing()
        Dim reply As ProcessReply = Hook(_scenario.Config(_scenario.Registry, True, True), Payload("posttooluse-green.json", "dotnet build """ & _scenario.Copy.SolutionPath & """", Nothing, True))
        Assert.Equal(0, reply.ExitCode)
        Assert.Contains("interrupted; nothing ran", ContextOf(reply))
        Print("(4)", reply)
    End Sub

    ''' <summary>
    ''' (5) Both gates off: exit 0, the refusal reported naming extract.enabled, one log line.
    ''' </summary>
    <Fact>
    Public Sub GateOffIsReportedNotThrown()
        Dim marker As String = "gateoff-" & Guid.NewGuid().ToString("N")
        Dim reply As ProcessReply = Hook(_scenario.Config(_scenario.Registry, False, False), Payload("posttooluse-green.json", "dotnet build """ & Path.Combine(_scenario.Root, marker, "Any.sln") & """"))
        Assert.Equal(0, reply.ExitCode)
        Dim context As String = ContextOf(reply)
        Assert.Contains("refused", context)
        Assert.Contains("extract.enabled", context)
        Assert.Equal(1, LogLines.Containing(marker).Count)
    End Sub

    ''' <summary>
    ''' (6) Stdin that is not JSON is reported.
    ''' </summary>
    <Fact>
    Public Sub NotJsonIsReported()
        Dim reply As ProcessReply = Hook(_scenario.Config(_scenario.Registry, True, True), "not json")
        Assert.Equal(0, reply.ExitCode)
        Assert.Contains("payload not JSON", ContextOf(reply))
        Print("(6)", reply)
    End Sub

    ''' <summary>
    ''' (7) A command that is not dotnet build or test does nothing: no log line, no launch.
    ''' </summary>
    <Fact>
    Public Sub NotADotnetCommandDoesNothing()
        Dim before As Integer = LogLines.Containing("").Count
        Dim reply As ProcessReply = Hook(_scenario.Config(_scenario.Registry, True, True), Payload("posttooluse-green.json", "ls -la"))
        Assert.Equal(0, reply.ExitCode)
        Assert.Contains("not a dotnet build or test", ContextOf(reply))
        Assert.Equal(before, LogLines.Containing("").Count)
        Print("(7)", reply)
    End Sub

    ''' <summary>
    ''' (8) The entry is fast: the non-cases of (2), (4), (6) and (7) each return within 1 s of RunOnce elapsed, process start included.
    ''' </summary>
    <Fact>
    Public Sub TheEntryIsFast()
        Dim config As String = _scenario.Config(_scenario.Registry, True, True)
        Dim inputs As Dictionary(Of String, String) = New Dictionary(Of String, String)(StringComparer.Ordinal) From {
            {"(2) failure payload", File.ReadAllText(Fixture("posttoolusefailure-red.json"))},
            {"(4) interrupted", Payload("posttooluse-green.json", "dotnet build x.sln", Nothing, True)},
            {"(6) not json", "not json"},
            {"(7) not dotnet", Payload("posttooluse-green.json", "ls -la")}}
        For Each pair As KeyValuePair(Of String, String) In inputs
            Dim reply As ProcessReply = Hook(config, pair.Value)
            _output.WriteLine(pair.Key & ": " & reply.Elapsed.TotalMilliseconds & " ms")
            Assert.True(reply.Elapsed < TimeSpan.FromSeconds(1), pair.Key & " took " & reply.Elapsed.TotalMilliseconds & " ms")
        Next
    End Sub

    Private Function Seed(registry As RegistryFixture, root As String, key As String) As String
        Directory.CreateDirectory(root)
        If Not File.Exists(Path.Combine(root, "Sample", "Sample.sln")) Then CopyTree(_scenario.Copy.Directory, Path.Combine(root, "Sample"))
        Using repo As Repository = GitFixture.Init(root)
            GitFixture.CommitAll(repo, "c1")
        End Using
        Dim solutionPath As String = Path.Combine(root, "Sample", "Sample.sln")
        DotnetCli.Run("restore """ & solutionPath & """", Path.Combine(root, "Sample"))
        Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = solutionPath, .DbPath = _scenario.Map.Path, .SolutionKey = key}, Nothing))
        registry.Seed(131373, "Sample", _scenario.SampleId, "active", _scenario.Copy.SolutionPath)
        registry.Seed(131373, key, MapQueries.ReadSolutions(_scenario.Map.Path).Find(Function(s As SolutionRow) s.Key = key).Id, "active", solutionPath)
        Return key
    End Function

    Private Shared Sub CopyTree(source As String, target As String)
        Directory.CreateDirectory(target)
        For Each file As String In Directory.GetFiles(source)
            IO.File.Copy(file, Path.Combine(target, Path.GetFileName(file)))
        Next
        For Each dir As String In Directory.GetDirectories(source)
            Dim name As String = Path.GetFileName(dir)
            If String.Equals(name, "bin", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(name, "obj", StringComparison.OrdinalIgnoreCase) Then Continue For
            CopyTree(dir, Path.Combine(target, name))
        Next
    End Sub

    Private Function NoExtractorConfig(registry As RegistryFixture) As String
        Return BridgeHost.WriteConfig(_scenario.Map.Path, Path.Combine(Path.GetTempPath(), "codemem-tests", "no-extractor-" & Guid.NewGuid().ToString("N") & ".dll"), True, True)
    End Function

    Private Shared Function Hook(configPath As String, stdin As String) As ProcessReply
        Return BridgeProcess.RunOnce("hook --config " & BridgeProcess.Quote(configPath), stdin)
    End Function

    Private Shared Function ContextOf(reply As ProcessReply) As String
        Dim lines As String() = reply.StandardOutput.Split(New String() {vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
        Assert.True(lines.Length = 1, "expected exactly one line on stdout, got " & lines.Length & ": " & reply.StandardOutput & reply.StandardError)
        Using document As JsonDocument = JsonDocument.Parse(lines(0))
            Dim hookOutput As JsonElement = document.RootElement.GetProperty("hookSpecificOutput")
            Assert.Equal("PostToolUse", hookOutput.GetProperty("hookEventName").GetString())
            Return hookOutput.GetProperty("additionalContext").GetString()
        End Using
    End Function

    Private Shared Function Fixture(name As String) As String
        Return Path.Combine(AppContext.BaseDirectory, "Fixtures", "Hooks", name)
    End Function

    Private Shared Function Payload(name As String, command As String, Optional cwd As String = Nothing, Optional interrupted As Boolean = False) As String
        Dim node As JsonObject = CType(JsonNode.Parse(File.ReadAllText(Fixture(name))), JsonObject)
        CType(node("tool_input"), JsonObject)("command") = command
        If cwd IsNot Nothing Then node("cwd") = cwd
        If interrupted Then CType(node("tool_response"), JsonObject)("interrupted") = True
        Return node.ToJsonString()
    End Function

    Private Sub Print(label As String, reply As ProcessReply)
        _output.WriteLine(label & " " & reply.Elapsed.TotalMilliseconds & " ms")
    End Sub

End Class

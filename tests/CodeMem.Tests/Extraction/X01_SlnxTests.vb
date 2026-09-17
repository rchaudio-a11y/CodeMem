' File: X01_SlnxTests.vb
' Project: CodeMem.Tests
' Description: US4 (feature 005): a .slnx is an equal input - the same fact set as the .sln, the same default key, the path passed recorded; the loader's own parse refuses a missing project, malformed XML, a foreign root and an empty solution by name before any project is opened; folders and forward slashes open; an unsupported extension is refused before a workspace exists; the usage names the three inputs (FR-421-FR-424; SC-406; R61; STOP 1 decision 2).
' Author: RCH Automation LLC
' Created: 2026-09-17
'
' Every extraction runs in-process through ExtractionRun.Execute into a TempMap; the refusals run additionally through the executable
' (ExtractorProcess) for the stderr line and the elapsed time (CON3). The parity pair is extracted once by SlnxPairFixture.
' RED:   2026-09-17 (T030) 10 of 10 on the first build: (1), (2), (3), (8) on the .slnx exit code (Failure, not Success) - the workspace's
'        OpenSolutionAsync answers "InvalidProjectFileException: No file format header found" (R61's route (a) with today's packages);
'        (4)-(7) that same line where the parse's own refusal was expected; (9) the workspace's "file extension '.txt' is not associated"
'        where "unsupported solution file" was expected; (10) the usage without the three inputs.
' GREEN: 2026-09-17 (T031) 10 of 10 on the first build with SlnxReader, the loader's three-way door before MSBuildWorkspace.Create and
'        the usage line; (4) and (9) answer in well under five seconds (no build host is started before the door).
' FIRE:  2026-09-17 (T031) the loader made to open only the first project path (Exit For after the first OpenProjectAsync) -> (1) and
'        (8) red: "fact sets differ: 118 only in the first (C|38|...); 3 only in the second (C|25|...)" - the App's symbols missing,
'        the run's observed count 25 against 38; reverted from a byte copy -> 10 of 10.

Imports System.IO
Imports System.Linq
Imports CodeMem.Extraction
Imports CodeMem.Extractor
Imports Xunit

''' <summary>
''' The refusal facts write their own small .slnx files under the temp directory; nothing here touches the committed fixture.
''' </summary>
<Collection("Fixture")>
Public Class X01_SlnxTests
    Implements IClassFixture(Of SlnxPairFixture)

    Private ReadOnly _pair As SlnxPairFixture

    ''' <summary>
    ''' Receives the shared pair.
    ''' </summary>
    ''' <param name="fixture">The collection fixture (restored before the pair was built).</param>
    ''' <param name="pair">The class fixture.</param>
    Public Sub New(fixture As FixtureSolution, pair As SlnxPairFixture)
        _pair = pair
    End Sub

    ''' <summary>
    ''' (1) Sample.slnx yields the fact set Sample.sln yields, in two fresh maps (FR-422, SC-406): at least one symbol, part and edge line.
    ''' </summary>
    <Fact>
    Public Sub ASlnxYieldsTheSlnFactSet()
        Assert.Equal(ExitCode.Success, _pair.SlnExit)
        Assert.Equal(ExitCode.Success, _pair.SlnxExit)
        Dim sln As List(Of String) = MapQueries.FactSet(_pair.SlnMap.Path, _pair.SolutionId(_pair.SlnMap))
        Dim slnx As List(Of String) = MapQueries.FactSet(_pair.SlnxMap.Path, _pair.SolutionId(_pair.SlnxMap))
        AssertSameFacts(sln, slnx)
        For Each prefix As String In New String() {"S|", "P|", "E|"}
            Assert.Contains(slnx, Function(line As String) line.StartsWith(prefix, StringComparison.Ordinal))
        Next
    End Sub

    ''' <summary>
    ''' (2) The key defaults to the file name without extension for both forms: Sample twice (FR-423).
    ''' </summary>
    <Fact>
    Public Sub TheKeyDefaultsToTheFileNameForBoth()
        Assert.Equal(ExitCode.Success, _pair.SlnxExit)
        Assert.Equal("Sample", MapQueries.ReadSolutions(_pair.SlnMap.Path)(0).Key)
        Assert.Equal("Sample", MapQueries.ReadSolutions(_pair.SlnxMap.Path)(0).Key)
    End Sub

    ''' <summary>
    ''' (3) last_seen_path records the file passed, extension included (FR-423).
    ''' </summary>
    <Fact>
    Public Sub LastSeenPathRecordsTheFilePassed()
        Assert.Equal(ExitCode.Success, _pair.SlnxExit)
        Assert.Equal(_pair.SlnPath, MapQueries.ReadSolutions(_pair.SlnMap.Path)(0).LastSeenPath)
        Assert.Equal(_pair.SlnxPath, MapQueries.ReadSolutions(_pair.SlnxMap.Path)(0).LastSeenPath)
        Assert.EndsWith(".slnx", MapQueries.ReadSolutions(_pair.SlnxMap.Path)(0).LastSeenPath)
    End Sub

    ''' <summary>
    ''' (4) A .slnx naming a project path that does not exist is refused naming the path before any project is opened (FR-421): exit 1,
    ''' the map holds no run, and the answer comes in under five seconds - no build host was started (SC-406; analyze I4).
    ''' </summary>
    <Fact>
    Public Sub AMissingProjectPathIsRefusedBeforeAnyOpen()
        Dim dir As String = TempDirectory()
        Dim file As String = Path.Combine(dir, "Missing.slnx")
        IO.File.WriteAllText(file, "<Solution>" & vbLf & "  <Project Path=""Missing/Missing.vbproj"" />" & vbLf & "</Solution>" & vbLf)
        Dim run As ExtractorProcess = Refused(file, "names a project that does not exist")
        Assert.Contains(Path.Combine(dir, "Missing", "Missing.vbproj"), run.StandardError, StringComparison.Ordinal)
        Assert.True(run.Elapsed < TimeSpan.FromSeconds(5), "took " & run.Elapsed.TotalMilliseconds & " ms")
    End Sub

    ''' <summary>
    ''' (5) A .slnx that is not well-formed XML is refused by name, the file named.
    ''' </summary>
    <Fact>
    Public Sub MalformedXmlIsRefusedByName()
        Dim file As String = Path.Combine(TempDirectory(), "Broken.slnx")
        IO.File.WriteAllText(file, "<Solution><Project Path=""Sample.Lib/Sample.Lib.vbproj""" & vbLf)
        Dim run As ExtractorProcess = Refused(file, "not well-formed XML")
        Assert.Contains(file, run.StandardError, StringComparison.Ordinal)
    End Sub

    ''' <summary>
    ''' (6) A .slnx whose root is not Solution is refused by name.
    ''' </summary>
    <Fact>
    Public Sub ANonSolutionRootIsRefusedByName()
        Dim file As String = Path.Combine(TempDirectory(), "Root.slnx")
        IO.File.WriteAllText(file, "<Project Path=""Sample.Lib/Sample.Lib.vbproj"" />" & vbLf)
        Dim run As ExtractorProcess = Refused(file, "has no Solution root")
        Assert.Contains(file, run.StandardError, StringComparison.Ordinal)
    End Sub

    ''' <summary>
    ''' (7) A .slnx naming no project is refused by name.
    ''' </summary>
    <Fact>
    Public Sub NoProjectIsRefusedByName()
        Dim file As String = Path.Combine(TempDirectory(), "Empty.slnx")
        IO.File.WriteAllText(file, "<Solution>" & vbLf & "  <Folder Name=""/src/"" />" & vbLf & "</Solution>" & vbLf)
        Dim run As ExtractorProcess = Refused(file, "names no project")
        Assert.Contains(file, run.StandardError, StringComparison.Ordinal)
    End Sub

    ''' <summary>
    ''' (8) Folders and forward slashes open: a .slnx whose two projects sit inside a Folder element with / paths extracts to the .sln's fact
    ''' set (FR-421).
    ''' </summary>
    <Fact>
    Public Sub FoldersAndForwardSlashesOpen()
        Assert.Equal(ExitCode.Success, _pair.SlnExit)
        Using copy As FixtureCopy = New FixtureCopy()
            Dim nested As String = Path.Combine(copy.Directory, "Nested.slnx")
            IO.File.WriteAllText(nested, "<Solution>" & vbLf & "  <Folder Name=""/src/"">" & vbLf & "    <Project Path=""Sample.Lib/Sample.Lib.vbproj"" />" & vbLf &
                                 "    <Project Path=""Sample.App/Sample.App.vbproj"" />" & vbLf & "  </Folder>" & vbLf & "</Solution>" & vbLf)
            Using map As TempMap = New TempMap()
                Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = nested, .DbPath = map.Path, .SolutionKey = "Sample"}, Nothing))
                AssertSameFacts(MapQueries.FactSet(_pair.SlnMap.Path, _pair.SolutionId(_pair.SlnMap)), MapQueries.FactSet(map.Path, MapQueries.ReadSolutions(map.Path)(0).Id))
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (9) An unsupported extension is refused by name before a workspace is created (FR-424): a copy of Sample.sln named .txt, exit 1,
    ''' under five seconds.
    ''' </summary>
    <Fact>
    Public Sub AnUnsupportedExtensionIsRefusedBeforeAWorkspace()
        Dim file As String = Path.Combine(TempDirectory(), "Sample.txt")
        IO.File.Copy(_pair.SlnPath, file)
        Dim run As ExtractorProcess = Refused(file, "unsupported solution file")
        Assert.Contains(file, run.StandardError, StringComparison.Ordinal)
        Assert.True(run.Elapsed < TimeSpan.FromSeconds(5), "took " & run.Elapsed.TotalMilliseconds & " ms")
    End Sub

    ''' <summary>
    ''' (10) The usage names the three inputs on its inputs line and in the --solution placeholder (contracts/extractor.md §4).
    ''' </summary>
    <Fact>
    Public Sub UsageNamesTheThreeInputs()
        Dim usage As String = CommandLine.Usage
        Assert.Contains("--solution <path.sln|path.slnx|path.vbproj>", usage, StringComparison.Ordinal)
        Dim inputs As String = usage.Split(New String() {Environment.NewLine}, StringSplitOptions.None).FirstOrDefault(Function(line As String) line.StartsWith("inputs:", StringComparison.Ordinal))
        Assert.NotNull(inputs)
        For Each extension As String In New String() {".sln", ".slnx", ".vbproj"}
            Assert.Contains(extension, inputs, StringComparison.Ordinal)
        Next
    End Sub

    Private Shared Function Refused(solutionPath As String, phrase As String) As ExtractorProcess
        Using inProcess As TempMap = New TempMap()
            Assert.Equal(ExitCode.Failure, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = solutionPath, .DbPath = inProcess.Path}, Nothing))
            Assert.True(HoldsNoRun(inProcess.Path), "a run was written in-process")
        End Using
        Using map As TempMap = New TempMap()
            Dim run As ExtractorProcess = ExtractorProcess.Run("--solution " & ExtractorProcess.Quote(solutionPath) & " --db " & ExtractorProcess.Quote(map.Path))
            Assert.True(run.ExitCode = 1, "exit " & run.ExitCode & ": " & run.StandardError & run.StandardOutput)
            Assert.True(run.StandardError.Contains(phrase, StringComparison.Ordinal), "expected '" & phrase & "' in: " & run.StandardError)
            Assert.True(HoldsNoRun(map.Path), "a run was written")
            Return run
        End Using
    End Function

    Private Shared Function HoldsNoRun(mapPath As String) As Boolean
        If Not IO.File.Exists(mapPath) Then Return True
        Try
            Return MapQueries.ReadRuns(mapPath).Count = 0
        Catch ex As Microsoft.Data.Sqlite.SqliteException
            Return True
        End Try
    End Function

    Private Shared Function TempDirectory() As String
        Dim dir As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "slnx-" & Guid.NewGuid().ToString("N"))
        Directory.CreateDirectory(dir)
        Return dir
    End Function

    Private Shared Sub AssertSameFacts(expected As List(Of String), actual As List(Of String))
        Dim onlyExpected As List(Of String) = expected.Except(actual).ToList()
        Dim onlyActual As List(Of String) = actual.Except(expected).ToList()
        Assert.True(onlyExpected.Count = 0 AndAlso onlyActual.Count = 0,
                    "fact sets differ: " & onlyExpected.Count & " only in the first (" & If(onlyExpected.Count > 0, onlyExpected(0), "-") & "); " &
                    onlyActual.Count & " only in the second (" & If(onlyActual.Count > 0, onlyActual(0), "-") & ")")
        Assert.Equal(expected.Count, actual.Count)
    End Sub

End Class

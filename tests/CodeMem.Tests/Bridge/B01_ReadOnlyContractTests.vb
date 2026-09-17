' File: B01_ReadOnlyContractTests.vb
' Project: CodeMem.Tests
' Description: The read-only contract (FR-342, CON3, CON4): the map is byte-identical after every read tool through the executable over stdio; the read-only door refuses a write and creates nothing; one read transaction per call.
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' Fact (2) writes through the door with an INSERT into solutions rather than map_identity (T010's text): map_identity's CHECK (id = 1)
' and its one existing row make every INSERT there fail on a read-write connection too, so the fire (widen the mode, watch the write
' succeed) could never go red through it. The statement lives in MapQueries (the allowed test SQL file). No double quotes in this header:
' SqlLocationGateTests pairs quotes across comment lines.
' RED:   2026-09-15 (T010) the assembly does not compile: ReadOnlyConnectionString and OpenReadOnly are not members of MapDatabase
'        (BC30456 x3); BridgeTools, BridgeHost, ReadSeams, BridgeReply are not declared (BC30451/BC30002 x9) - twelve errors, recorded as
'        the Red now that the six facts are written; the skeleton lands first in T011, the door next (CON2, analyze pass 2 I1).
' GREEN: 2026-09-15 (T011) (1)-(3) after the skeleton (ReadSeams, BridgeTools, BridgeHost, BridgeReply) and the door
'        (ReadOnlyConnectionString, OpenReadOnly, BeginRead, EndRead); build 0 warnings / 0 errors.
' FIRE:  2026-09-15 (T011) Mode changed to SqliteOpenMode.ReadWriteCreate (the driver's default; plain ReadWrite would still refuse a missing
'        file) -> (1) red (Mode=ReadOnly absent), (2) red (the INSERT succeeded: expected 8, actual 0), (3) red (no exception: a file was
'        created); reverted -> green.
' RED:   2026-09-15 (T015) with the server wired and no tool registered: (4) red for the stated reason (no tools listed: the scan is
'        vacuous); (5) green through BridgeProcess (serverInfo.name codemem, 0 tools listed and 0 registered, exit 0, no -journal);
'        (6) red on NotImplementedException("US1") from BridgeTools.Solutions; (1)-(3) green; the four gates green (11 of 13).
' GREEN: 2026-09-15 (T023) 6 of 6 with the five readers: (4) over five tools through the executable, (5) five tools listed (compared as
'        sorted sets: the SDK lists in its own order) with every description equal to its constant, (6) busy code 5 during the call, the
'        original name in the response, the rename succeeding afterwards - once MapAccess issued BEGIN before the schema inspection, so the
'        SHARED lock is taken inside the read transaction (a deferred BEGIN alone held nothing: the first run recorded 0, not 5).
' FIRE:  2026-09-15 (T024 d) BridgeTools.RunRead ended the read (EndRead) before the reader instead of after serialisation -> (6) red (the
'        rename succeeded mid-call: expected 5, actual 0); restored from a byte copy -> green.
' 2026-09-17 (feature 005, T017): the registry fixture and its seed leave (4), (5) and (6) - the configuration names the map only
' (FR-403); the facts are otherwise unchanged and stayed green through T014-T016 (no fact of this file named projectId).

Imports System.IO
Imports System.Text.Json
Imports CodeMem.Bridging
Imports CodeMem.Core
Imports CodeMem.Extraction
Imports Microsoft.Data.Sqlite
Imports Xunit

''' <summary>
''' Six facts: the connection string, the write refusal, the missing path, the hash after every read tool over stdio, the executable's
''' serverInfo, tool list and descriptions, and the commit-straddling fact through the read seam.
''' </summary>
<Collection("Fixture")>
Public Class B01_ReadOnlyContractTests

    Private ReadOnly _fixture As FixtureSolution

    ''' <summary>
    ''' Receives the shared, restored fixture solution.
    ''' </summary>
    ''' <param name="fixture">The collection fixture.</param>
    Public Sub New(fixture As FixtureSolution)
        _fixture = fixture
    End Sub

    ''' <summary>
    ''' (1) The read-only connection string carries Mode=ReadOnly and a 3 s busy timeout (research R44, R53).
    ''' </summary>
    <Fact>
    Public Sub ReadOnlyConnectionStringCarriesModeReadOnly()
        Dim connectionString As String = MapDatabase.ReadOnlyConnectionString(Path.Combine(Path.GetTempPath(), "codemem-tests", "any.sqlite"))
        Assert.Contains("Mode=ReadOnly", connectionString)
        Assert.Contains("Default Timeout=3", connectionString)
    End Sub

    ''' <summary>
    ''' (2) A write through the read-only door fails with the driver's read-only error, SQLITE_READONLY (8).
    ''' </summary>
    <Fact>
    Public Sub AWriteThroughTheReadOnlyDoorIsRefused()
        Using map As TempMap = New TempMap()
            Extract(map)
            Using db As MapDatabase = MapDatabase.OpenReadOnly(map.Path)
                Assert.Equal(8, MapQueries.AttemptWrite(db))
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (3) Opening a missing path read-only fails with SQLITE_CANTOPEN (14) and creates no file.
    ''' </summary>
    <Fact>
    Public Sub OpeningAMissingPathCreatesNothing()
        Dim absent As String = Path.Combine(Path.GetTempPath(), "codemem-tests", "absent-" & Guid.NewGuid().ToString("N") & ".sqlite")
        Dim ex As SqliteException = Assert.Throws(Of SqliteException)(Sub() MapDatabase.OpenReadOnly(absent).Dispose())
        Assert.Equal(14, ex.SqliteErrorCode)
        Assert.False(File.Exists(absent), "a read-only open created a file")
    End Sub

    ''' <summary>
    ''' (4) Through the spawned executable, every listed tool but extract is called once with the arguments the table below supplies; each
    ''' reply is not an error and the map's SHA-256 is unchanged after each; more than zero tools were called (the vacuous guard). CON3.
    ''' </summary>
    <Fact>
    Public Sub EveryReadToolLeavesTheMapByteIdenticalOverStdio()
        Using map As TempMap = New TempMap()
            Dim solutionId As Long = Extract(map)
            Dim config As String = BridgeHost.WriteConfig(map.Path, Nothing, False, False)
            Dim consumerId As Long = SymbolId(map, "T:Sample.Consumer")
            Dim before As String = MapSnapshot.FileBytesHash(map.Path)
            Dim called As Integer = 0
            Using server As BridgeProcess = BridgeProcess.Serve(config)
                server.Initialize()
                For Each name As String In server.ListTools()
                    If name = "extract" Then Continue For
                    Dim reply As ToolReply = server.CallTool(name, ArgumentsFor(name, consumerId))
                    Assert.False(reply.IsError, name & ": " & reply.Text)
                    Assert.Equal(before, MapSnapshot.FileBytesHash(map.Path))
                    called += 1
                Next
                Assert.True(called > 0, "no tools listed: the scan is vacuous")
                Assert.Equal(0, server.Close())
            End Using
        End Using
    End Sub

    ''' <summary>
    ''' (5) The executable answers initialize with serverInfo.name codemem, lists exactly the registered tool names, returns each registered
    ''' description verbatim (FR-312 on the production route, G4), and leaves no -journal beside the map after the session.
    ''' </summary>
    <Fact>
    Public Sub TheExecutableServesOverStdio()
        Using map As TempMap = New TempMap()
            Dim solutionId As Long = Extract(map)
            Dim config As String = BridgeHost.WriteConfig(map.Path, Nothing, False, False)
            Using server As BridgeProcess = BridgeProcess.Serve(config)
                Dim info As JsonElement = server.Initialize()
                Assert.Equal("codemem", info.GetProperty("name").GetString())
                Dim expectedNames As List(Of String) = New List(Of String)(BridgeTools.RegisteredToolNames)
                Dim listedNames As List(Of String) = server.ListTools()
                expectedNames.Sort(StringComparer.Ordinal)
                listedNames.Sort(StringComparer.Ordinal)
                Assert.Equal(expectedNames, listedNames)
                Dim descriptions As Dictionary(Of String, String) = server.ListToolDescriptions()
                For Each name As String In BridgeTools.RegisteredToolNames
                    Assert.Equal(BridgeTools.RegisteredToolDescriptions(name), descriptions(name))
                Next
                Assert.Equal(0, server.Close())
            End Using
            Assert.False(File.Exists(map.Path & "-journal"), "a journal appeared beside the map")
        End Using
    End Sub

    ''' <summary>
    ''' (6) A publication cannot straddle a tool's reads (CON4, research R58): a rename attempted between scope resolution and the reader,
    ''' on a second read-write connection with a 1 s busy timeout, gets SQLITE_BUSY (5); the response carries the original name; the same
    ''' rename succeeds after the call.
    ''' </summary>
    <Fact>
    Public Sub APublicationCannotStraddleAToolsReads()
        Using map As TempMap = New TempMap()
            Dim solutionId As Long = Extract(map)
            Dim config As String = BridgeHost.WriteConfig(map.Path, Nothing, False, False)
            Dim recorded As Integer = -1
            Dim seams As ReadSeams = New ReadSeams With {.AfterScopeResolved = Sub() recorded = MapQueries.TryRenameSolution(map.Path, solutionId, "changed", 1)}
            Dim host As BridgeHost = New BridgeHost(config, seams)
            Dim reply As BridgeReply = host.Invoke("solutions", New Dictionary(Of String, Object)())
            Assert.False(reply.IsError, reply.Text)
            Assert.Equal(5, recorded)
            Assert.Equal("Sample", reply.Root().GetProperty("solutions")(0).GetProperty("name").GetString())
            Assert.Equal(0, MapQueries.TryRenameSolution(map.Path, solutionId, "changed", 1))
        End Using
    End Sub

    ''' <summary>
    ''' The arguments (4) supplies per tool name; a name this table does not know fails the fact.
    ''' </summary>
    ''' <param name="name">The tool name.</param>
    ''' <param name="consumerId">The id of T:Sample.Consumer in the map under test.</param>
    ''' <returns>The arguments as JSON text.</returns>
    Private Shared Function ArgumentsFor(name As String, consumerId As Long) As String
        Select Case name
            Case "solutions", "map_status"
                Return "{}"
            Case "symbol_search"
                Return "{""solutionKey"":""Sample"",""name"":""Widget""}"
            Case "symbol_detail", "references", "type_usages"
                Return "{""solutionKey"":""Sample"",""symbolId"":" & consumerId & "}"
            Case "orphans"
                Return "{""solutionKey"":""Sample""}"
            Case Else
                Throw New InvalidOperationException("no arguments for tool " & name & ": extend the table")
        End Select
    End Function

    Private Function Extract(map As TempMap) As Long
        Assert.Equal(ExitCode.Success, ExtractionRun.Execute(New ExtractionOptions With {.SolutionPath = _fixture.SolutionPath, .DbPath = map.Path}, Nothing))
        Return MapQueries.ReadSolutions(map.Path)(0).Id
    End Function

    Private Shared Function SymbolId(map As TempMap, docCommentId As String) As Long
        Dim solutionId As Long = MapQueries.ReadSolutions(map.Path)(0).Id
        Dim row As SymbolRow = MapQueries.ReadSymbols(map.Path, solutionId).Find(Function(s As SymbolRow) s.DocCommentId = docCommentId AndAlso s.IsActive)
        Assert.True(row IsNot Nothing, docCommentId & " is not an active row")
        Return row.Id
    End Function

End Class

' File: B07_TwinPresentationTests.vb
' Project: CodeMem.Tests
' Description: The presentation rule: one declaration compiled into N projects, the header naming every twin, raw rows preserved under one project, never folded across solutions, the description (FR-340, FR-341, spec Q2, COR3; SC-309's fixture half).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' Three departures from T048's text, each forced by the compiler or the contract: (i) VB refuses two method declarations on one logical
' line (BC32009) and the extractor records raw line spans, so the same-project shape is made by moving the second overload's start_line
' onto the first's in the temp map (TwinScenario); (ii) the overloads are named Overloaded, not M - the name filter is a contains match
' and "M" would match every method with an m in its name; (iii) DefineConstants takes the VB form $(DefineConstants),TWIN_APP=True - the
' semicolon form is a C# idiom vbc rejects (BC31030).
' RED:   2026-09-16 (T048) against the pass-through folder: (1) red (total 3 where 2 was asserted - two Twin rows and the fixture's Twins),
'        (3) red (the same 3), (2) red (compiledInto of 1 where 2 was asserted); (4) green, as T048 foresaw (the boundary the T050 fire
'        exercises); (5) green, not T048's red - the fold sentence was already verbatim in BridgeToolDescriptions since T014.
' GREEN: 2026-09-16 (T050) after TwinFolder's fold, CodeSymbolsRepository.ReadTwins and the detail header: B07 (1)-(5) with B02 and
'        B01 - 20 passed / 0 failed in 18 s. (3) was corrected on the way: a solution key resolves against the map, so Other answers too.
' FIRE:  2026-09-16 (T050) the all-distinct check dropped from TwinFolder.FoldsTogether (every group of more than one row folded) ->
'        (4) red (the two Overloaded rows folded into one declaration: total 1 where 2 was asserted); restored from a byte copy ->
'        B07 5 passed.

Imports System.Text.Json
Imports CodeMem.Bridging
Imports Xunit

''' <summary>
''' Facts on TwinScenario: a linked file, an overload pair on one line, a partial type, a second solution of the same tree.
''' </summary>
<Collection("Fixture")>
Public Class B07_TwinPresentationTests
    Implements IClassFixture(Of TwinScenario)

    Private Shared ReadOnly EightVerbs As String() = New String() {"part_of", "calls", "uses", "implements", "extends", "imports", "depends_on", "handles"}

    Private ReadOnly _scenario As TwinScenario

    ''' <summary>
    ''' Receives the shared extraction.
    ''' </summary>
    ''' <param name="scenario">The class fixture.</param>
    Public Sub New(scenario As TwinScenario)
        _scenario = scenario
    End Sub

    ''' <summary>
    ''' (1) The linked class presents once: total 2 (the folded Twin and the fixture's own Twins class, which the contains filter also
    ''' finds), the Twin declaration with compiledInto of 2 naming both projects, both symbol ids and both doc ids, one path and one line;
    ''' its method twins fold likewise (found by path among the methods named Name).
    ''' </summary>
    <Fact>
    Public Sub TwinsPresentAsOneDeclaration()
        Dim reply As BridgeReply = _scenario.Host.Invoke("symbol_search", Args("solutionKey", "Sample", "name", "Twin", "kind", "class"))
        Assert.False(reply.IsError, reply.Text)
        Dim root As JsonElement = reply.Root()
        Assert.Equal(2, root.GetProperty("total").GetInt32())
        Assert.Equal(2, root.GetProperty("symbols").GetArrayLength())
        Dim declaration As JsonElement = OnlyNamed(root, "Twin")
        Dim compiledInto As JsonElement = declaration.GetProperty("compiledInto")
        Assert.Equal(2, compiledInto.GetArrayLength())
        Dim projects As List(Of String) = New List(Of String)()
        Dim projectIds As HashSet(Of Long) = New HashSet(Of Long)()
        Dim ids As HashSet(Of Long) = New HashSet(Of Long)()
        Dim docIds As HashSet(Of String) = New HashSet(Of String)(StringComparer.Ordinal)
        For Each twin As JsonElement In compiledInto.EnumerateArray()
            projects.Add(twin.GetProperty("projectName").GetString())
            projectIds.Add(twin.GetProperty("projectSymbolId").GetInt64())
            ids.Add(twin.GetProperty("symbolId").GetInt64())
            docIds.Add(twin.GetProperty("docCommentId").GetString())
        Next
        Assert.Contains("Sample.App", projects)
        Assert.Contains("Sample.Lib", projects)
        Assert.Equal(2, projectIds.Count)
        Assert.Equal(2, ids.Count)
        Assert.Contains("T:Sample.App.Shared.Twin", docIds)
        Assert.Contains("T:Sample.Lib.Shared.Twin", docIds)
        Assert.Equal("Shared/Twin.vb", declaration.GetProperty("path").GetString())
        Assert.Equal(6, declaration.GetProperty("line").GetInt32())
        Dim methods As BridgeReply = _scenario.Host.Invoke("symbol_search", Args("solutionKey", "Sample", "name", "Name", "kind", "method"))
        Assert.False(methods.IsError, methods.Text)
        Dim inTwin As List(Of JsonElement) = New List(Of JsonElement)()
        For Each method As JsonElement In methods.Root().GetProperty("symbols").EnumerateArray()
            If method.GetProperty("path").GetString() = "Shared/Twin.vb" Then inTwin.Add(method)
        Next
        Dim twinMethod As JsonElement = Assert.Single(inTwin)
        Assert.Equal(2, twinMethod.GetProperty("compiledInto").GetArrayLength())
    End Sub

    ''' <summary>
    ''' (2) symbol_detail of either twin names both, the requested id first; parts and edges are the requested id's own (its part_of target
    ''' is its own project's namespace); every occurrence passes the identity invariant.
    ''' </summary>
    <Fact>
    Public Sub DetailOfEitherTwinNamesBoth()
        For Each docId As String In New String() {"T:Sample.App.Shared.Twin", "T:Sample.Lib.Shared.Twin"}
            Dim id As Long = _scenario.SymbolId(docId)
            Dim reply As BridgeReply = _scenario.Host.Invoke("symbol_detail", Args("solutionKey", "Sample", "symbolId", id))
            Assert.False(reply.IsError, reply.Text)
            Dim root As JsonElement = reply.Root()
            Dim compiledInto As JsonElement = root.GetProperty("symbol").GetProperty("compiledInto")
            Assert.Equal(2, compiledInto.GetArrayLength())
            Assert.Equal(id, compiledInto(0).GetProperty("symbolId").GetInt64())
            Assert.Equal(docId, compiledInto(0).GetProperty("docCommentId").GetString())
            Assert.NotEqual(id, compiledInto(1).GetProperty("symbolId").GetInt64())
            Assert.Equal(id, root.GetProperty("symbol").GetProperty("id").GetInt64())
            Assert.Equal(1, root.GetProperty("parts").GetArrayLength())
            Dim partOf As JsonElement = root.GetProperty("outbound").GetProperty("part_of")
            Assert.Equal(1, partOf.GetArrayLength())
            Assert.Equal(If(docId.Contains(".App."), "N:Sample.App.Shared", "N:Sample.Lib.Shared"), partOf(0).GetProperty("targetDocCommentId").GetString())
            For Each side As String In New String() {"outbound", "inbound"}
                For Each verb As String In EightVerbs
                    For Each edge As JsonElement In root.GetProperty(side).GetProperty(verb).EnumerateArray()
                        OccurrenceAssertions.AssertIdentity(edge)
                    Next
                Next
            Next
        Next
    End Sub

    ''' <summary>
    ''' (3) The same tree extracted again as Other into the same map: under each solution the fold presents one Twin declaration with two
    ''' entries, never four, and no symbol id appears under both (a solution key resolves against the map; no registry row is needed).
    ''' </summary>
    <Fact>
    Public Sub TwinsAcrossSolutionsDoNotFold()
        Assert.Equal(2, MapQueries.ReadSolutions(_scenario.Map.Path).Count)
        Dim seen As HashSet(Of Long) = New HashSet(Of Long)()
        For Each key As String In New String() {"Sample", "Other"}
            Dim reply As BridgeReply = _scenario.Host.Invoke("symbol_search", Args("solutionKey", key, "name", "Twin", "kind", "class"))
            Assert.False(reply.IsError, reply.Text)
            Assert.Equal(2, reply.Root().GetProperty("total").GetInt32())
            Dim compiledInto As JsonElement = OnlyNamed(reply.Root(), "Twin").GetProperty("compiledInto")
            Assert.Equal(2, compiledInto.GetArrayLength())
            For Each twin As JsonElement In compiledInto.EnumerateArray()
                Assert.True(seen.Add(twin.GetProperty("symbolId").GetInt64()), "a twin id presented under both solutions")
            Next
        Next
        Assert.Equal(4, seen.Count)
    End Sub

    ''' <summary>
    ''' (4) Rows sharing the four fields under one project stay one declaration per row (COR3): two overloads on one line are two
    ''' declarations with compiledInto of one each; a partial type is one symbol with two parts and compiledInto of one.
    ''' </summary>
    <Fact>
    Public Sub SameProjectRowsStayTwoDeclarations()
        Dim overloadPair As BridgeReply = _scenario.Host.Invoke("symbol_search", Args("solutionKey", "Sample", "name", "Overloaded", "kind", "method"))
        Assert.False(overloadPair.IsError, overloadPair.Text)
        Assert.Equal(2, overloadPair.Root().GetProperty("total").GetInt32())
        Assert.Equal(2, overloadPair.Root().GetProperty("symbols").GetArrayLength())
        Dim ids As HashSet(Of Long) = New HashSet(Of Long)()
        For Each declaration As JsonElement In overloadPair.Root().GetProperty("symbols").EnumerateArray()
            Assert.Equal(1, declaration.GetProperty("compiledInto").GetArrayLength())
            Assert.Equal("Sample.Lib", declaration.GetProperty("project").GetProperty("name").GetString())
            Assert.Equal("Sample.Lib/OnOneLine.vb", declaration.GetProperty("path").GetString())
            Assert.Equal(2, declaration.GetProperty("line").GetInt32())
            ids.Add(declaration.GetProperty("id").GetInt64())
        Next
        Assert.Equal(2, ids.Count)
        Dim partialType As BridgeReply = _scenario.Host.Invoke("symbol_search", Args("solutionKey", "Sample", "name", "MainForm", "kind", "class"))
        Assert.False(partialType.IsError, partialType.Text)
        Assert.Equal(1, partialType.Root().GetProperty("symbols").GetArrayLength())
        Assert.Equal(1, partialType.Root().GetProperty("symbols")(0).GetProperty("compiledInto").GetArrayLength())
        Dim detail As BridgeReply = _scenario.Host.Invoke("symbol_detail", Args("solutionKey", "Sample", "symbolId", partialType.Root().GetProperty("symbols")(0).GetProperty("id").GetInt64()))
        Assert.False(detail.IsError, detail.Text)
        Assert.Equal(2, detail.Root().GetProperty("parts").GetArrayLength())
        Assert.Equal(1, detail.Root().GetProperty("symbol").GetProperty("compiledInto").GetArrayLength())
    End Sub

    ''' <summary>
    ''' (5) The registered search description states the fold.
    ''' </summary>
    <Fact>
    Public Sub TheSearchDescriptionStatesTheFold()
        Assert.Contains("compiled into N projects", BridgeTools.RegisteredToolDescriptions("symbol_search"))
    End Sub

    Private Shared Function OnlyNamed(root As JsonElement, name As String) As JsonElement
        Dim named As List(Of JsonElement) = New List(Of JsonElement)()
        For Each symbol As JsonElement In root.GetProperty("symbols").EnumerateArray()
            If symbol.GetProperty("name").GetString() = name Then named.Add(symbol)
        Next
        Return Assert.Single(named)
    End Function

    Private Shared Function Args(ParamArray pairs As Object()) As Dictionary(Of String, Object)
        Dim result As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            result(CStr(pairs(i))) = pairs(i + 1)
        Next
        Return result
    End Function

End Class

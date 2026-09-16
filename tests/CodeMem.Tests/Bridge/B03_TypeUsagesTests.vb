' File: B03_TypeUsagesTests.vb
' Project: CodeMem.Tests
' Description: type_usages on a fixture copy: the union of constructor calls, member calls and uses, implements and extends; fromInside and fromOutside; deduplication by edge id; references unchanged; the non-type refusal; the description (FR-316..FR-319, FR-344, DUP1).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' RED:   2026-09-15 (T027) (1), (1b), (1c), (3), (4) red on NotImplementedException("US2") from BridgeTools.TypeUsages; (5) red on
'        KeyNotFound (type_usages not yet registered) - the named Red. (2) green already: it pins references' existing narrowness (FR-318),
'        a guard on unchanged behaviour rather than a Red-first fact, trusted through the T030 fires on the new tool beside it.
' GREEN: 2026-09-15 (T030) 7 of 7 after ReadTypeUsages (T028) and the reader, envelopes and registration (T029); B01 (4) over six tools.
' FIRE:  2026-09-15 (T030) (a) every occurrence counted as outside in TypeUsagesReader (fromInside False, fromOutside unconditional) ->
'        (1) red (the Twice occurrences asserted fromInside); (b) UNION -> UNION ALL on the implements/extends arm of ReadTypeUsages ->
'        (1c) red ("T:Sample.Widgets.ISampleService: edge 133 repeated"); each restored from a byte copy -> green.

Imports System.IO
Imports System.Text.Json
Imports CodeMem.Bridging
Imports CodeMem.Extraction
Imports Xunit

''' <summary>
''' The copy gains an explicit constructor and a sibling call inside AlphaService, and a ServiceUser in the other project that constructs
''' it and calls Serve; extracted once per class (TypeUsagesScenario).
''' </summary>
<Collection("Fixture")>
Public Class B03_TypeUsagesTests
    Implements IClassFixture(Of TypeUsagesScenario)

    Private Shared ReadOnly SevenVerbs As String() = New String() {"calls", "uses", "implements", "extends", "imports", "depends_on", "handles"}

    Private ReadOnly _scenario As TypeUsagesScenario

    ''' <summary>
    ''' Receives the shared extraction.
    ''' </summary>
    ''' <param name="scenario">The class fixture.</param>
    Public Sub New(scenario As TypeUsagesScenario)
        _scenario = scenario
    End Sub

    ''' <summary>
    ''' (1) type_usages of AlphaService unions the constructor call and the Serve call from ServiceUser (outside), the two Serve calls from
    ''' Twice (inside) and the uses occurrence of the As clause; byVerb has seven keys; total and fromOutside agree with the flags.
    ''' </summary>
    <Fact>
    Public Sub TypeUsagesUnionsConstructorsMembersAndImplements()
        Dim root As JsonElement = Usages("T:Sample.Widgets.AlphaService")
        AssertHas(root.GetProperty("byVerb"), SevenVerbs)
        Dim occurrences As JsonElement = root.GetProperty("occurrences")
        Dim outside As Integer = 0
        Dim constructorFromRun As Integer = 0
        Dim serveFromRun As Integer = 0
        Dim serveFromTwice As Integer = 0
        Dim usesFromRun As Integer = 0
        For Each occurrence As JsonElement In occurrences.EnumerateArray()
            OccurrenceAssertions.AssertIdentity(occurrence)
            Dim fromInside As Boolean = occurrence.GetProperty("fromInside").GetBoolean()
            If Not fromInside Then outside += 1
            Dim source As String = occurrence.GetProperty("source").GetProperty("name").GetString()
            Dim role As String = occurrence.GetProperty("target").GetProperty("role").GetString()
            Dim verb As String = occurrence.GetProperty("verb").GetString()
            If source = "Run" AndAlso role = "constructor" AndAlso verb = "calls" Then
                constructorFromRun += 1
                Assert.False(fromInside)
            End If
            If source = "Run" AndAlso role = "member" AndAlso verb = "calls" Then
                serveFromRun += 1
                Assert.False(fromInside)
            End If
            If source = "Twice" AndAlso role = "member" AndAlso verb = "calls" Then
                serveFromTwice += 1
                Assert.True(fromInside)
            End If
            If source = "Run" AndAlso role = "type" AndAlso verb = "uses" Then
                usesFromRun += 1
                Assert.False(fromInside)
            End If
        Next
        Assert.Equal(1, constructorFromRun)
        Assert.Equal(1, serveFromRun)
        Assert.Equal(2, serveFromTwice)
        Assert.Equal(1, usesFromRun)
        Assert.Equal(occurrences.GetArrayLength(), root.GetProperty("total").GetInt32())
        Assert.Equal(outside, root.GetProperty("fromOutside").GetInt32())
        Dim verbSum As Integer = 0
        For Each verb As String In SevenVerbs
            verbSum += root.GetProperty("byVerb").GetProperty(verb).GetInt32()
        Next
        Assert.Equal(root.GetProperty("total").GetInt32(), verbSum)
    End Sub

    ''' <summary>
    ''' (1b) implements is an inbound use of the interface: ISampleService sees AlphaService and BetaService as type-role sources; no calls.
    ''' </summary>
    <Fact>
    Public Sub ImplementsIsAnInboundUseOfTheInterface()
        Dim root As JsonElement = Usages("T:Sample.Widgets.ISampleService")
        Assert.True(root.GetProperty("byVerb").GetProperty("implements").GetInt32() >= 2, "fewer than two implements occurrences")
        Assert.Equal(0, root.GetProperty("byVerb").GetProperty("calls").GetInt32())
        Dim sources As List(Of String) = New List(Of String)()
        For Each occurrence As JsonElement In root.GetProperty("occurrences").EnumerateArray()
            If occurrence.GetProperty("verb").GetString() = "implements" Then
                Assert.Equal("type", occurrence.GetProperty("target").GetProperty("role").GetString())
                sources.Add(occurrence.GetProperty("source").GetProperty("name").GetString())
            End If
        Next
        Assert.Contains("AlphaService", sources)
        Assert.Contains("BetaService", sources)
    End Sub

    ''' <summary>
    ''' (1c) Every edge appears exactly once across AlphaService, ISampleService and MidWidget: no edgeId repeats (DUP1), the arms overlap harmlessly.
    ''' </summary>
    <Fact>
    Public Sub EveryEdgeAppearsExactlyOnce()
        For Each docId As String In New String() {"T:Sample.Widgets.AlphaService", "T:Sample.Widgets.ISampleService", "T:Sample.Widgets.MidWidget"}
            Dim seen As HashSet(Of Long) = New HashSet(Of Long)()
            Dim occurrences As JsonElement = Usages(docId).GetProperty("occurrences")
            Assert.True(occurrences.GetArrayLength() > 0, docId & ": no occurrences")
            For Each occurrence As JsonElement In occurrences.EnumerateArray()
                Assert.True(seen.Add(occurrence.GetProperty("edgeId").GetInt64()), docId & ": edge " & occurrence.GetProperty("edgeId").GetInt64() & " repeated")
            Next
        Next
    End Sub

    ''' <summary>
    ''' (2) references on the type stays narrow: the uses occurrence at the As clause and no calls (the constructor call targets the constructor).
    ''' </summary>
    <Fact>
    Public Sub ReferencesOnTheTypeStaysNarrow()
        Dim reply As BridgeReply = _scenario.Host.Invoke("references", Args("solutionKey", "Sample", "symbolId", _scenario.SymbolId("T:Sample.Widgets.AlphaService")))
        Assert.False(reply.IsError, reply.Text)
        Dim verbs As List(Of String) = New List(Of String)()
        For Each occurrence As JsonElement In reply.Root().GetProperty("occurrences").EnumerateArray()
            verbs.Add(occurrence.GetProperty("verb").GetString())
        Next
        Assert.Contains("uses", verbs)
        Assert.DoesNotContain("calls", verbs)
    End Sub

    ''' <summary>
    ''' (3) extends is an inbound use: MidWidget sees the extends occurrence from LeafWidget.
    ''' </summary>
    <Fact>
    Public Sub ExtendsIsAnInboundUse()
        Dim root As JsonElement = Usages("T:Sample.Widgets.MidWidget")
        Dim found As Boolean = False
        For Each occurrence As JsonElement In root.GetProperty("occurrences").EnumerateArray()
            If occurrence.GetProperty("verb").GetString() = "extends" AndAlso occurrence.GetProperty("source").GetProperty("name").GetString() = "LeafWidget" Then found = True
        Next
        Assert.True(found, "no extends occurrence from LeafWidget")
    End Sub

    ''' <summary>
    ''' (4) A method is refused by name with the remedy.
    ''' </summary>
    <Fact>
    Public Sub NonTypesAreRefused()
        Dim reply As BridgeReply = _scenario.Host.Invoke("type_usages", Args("solutionKey", "Sample", "symbolId", _scenario.SymbolId("M:Sample.Consumer.Build")))
        Assert.True(reply.IsError, "a method was not refused")
        Assert.Contains("not a type", reply.Text)
        Assert.Contains("Use references", reply.Text)
    End Sub

    ''' <summary>
    ''' (5) The registered description names the constructor gap, the inside flag and the two counts.
    ''' </summary>
    <Fact>
    Public Sub TheTypeUsagesDescriptionNamesTheConstructorGap()
        Dim description As String = BridgeTools.RegisteredToolDescriptions("type_usages")
        For Each phrase As String In New String() {"calls to each of its constructors", "from inside", "total", "fromOutside"}
            Assert.Contains(phrase, description)
        Next
    End Sub

    Private Function Usages(docCommentId As String) As JsonElement
        Dim reply As BridgeReply = _scenario.Host.Invoke("type_usages", Args("solutionKey", "Sample", "symbolId", _scenario.SymbolId(docCommentId)))
        Assert.False(reply.IsError, reply.Text)
        Return reply.Root()
    End Function

    Private Shared Function Args(ParamArray pairs As Object()) As Dictionary(Of String, Object)
        Dim result As Dictionary(Of String, Object) = New Dictionary(Of String, Object)(StringComparer.Ordinal)
        For i As Integer = 0 To pairs.Length - 1 Step 2
            result(CStr(pairs(i))) = pairs(i + 1)
        Next
        Return result
    End Function

    Private Shared Sub AssertHas(element As JsonElement, ParamArray names As String())
        Dim value As JsonElement
        For Each name As String In names
            Assert.True(element.TryGetProperty(name, value), "property missing: " & name & " in " & element.GetRawText())
        Next
    End Sub

End Class

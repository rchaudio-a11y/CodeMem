' File: UsageTextsGateTests.vb
' Project: CodeMem.Tests
' Description: Review gate of constitution v1.5.0 (feature 006, FR-511-FR-513, FR-518-FR-521): the shipped usage texts agree with the tools - the instructions advertised exactly and naming every registered tool, the kit's phrases, the README's tool table equal to the registered tools, nothing private under docs/, nine tools in the README.
' Author: RCH Automation LLC
' Created: 2026-09-29
'
' Phrases compare with every run of whitespace collapsed to one space (spec Q4): the snippet breaks its sentence across an indented line.
' A tool name is a whole word when no identifier character touches it on either side, so solutions does not match inside solutionKey.
' RED:   2026-09-29 (T016) with the product at 637e96d: the assembly does not compile - BridgeServerInstructions is not declared (BC30451,
'        (1)-(3) and B01 (5)). With T016's empty stub: (1) red - initialize carries no instructions; (2) red - all nine names missing; (3)
'        red - all four phrases missing. (4)-(7) red for US3 as named: (4) missing rename_candidates, (5) SKILL.md does not exist, (7)
'        README does not say MCP-9 tools; (6) green first (a guard on absence), trusted through T026's fire.
' GREEN: 2026-09-29 (T020) (1)-(3) once T017 wrote 191490 section 1 from the store (hash d8a173d3 matched) and RunAsync set it.
' FIRE:  2026-09-29 (T020) each injected alone, restored from a byte copy: (1) a vbCrLf concatenated onto the constant -> red, substring
'        found at 1639; the ServerInstructions line removed from RunAsync -> (1) and B01 (5) red, initialize carries no instructions; (2)
'        the rename_candidates line deleted from the constant -> red, missing rename_candidates; (3) the proof line changed -> red,
'        Prove, don't grep missing.
' GREEN: 2026-09-29 (T026) (4)-(7) once the kit files, the README and the fragment landed from the store (six verbatim hashes OK).
' FIRE:  2026-09-29 (T026) each injected alone, restored from a byte copy: (4) the rename_candidates row deleted -> red, missing
'        rename_candidates; a ghost row added -> red, extra ghost; (5) the sentence deleted from SKILL.md -> red, SKILL.md does not carry
'        it (its first Red was file missing, analyze C1); (6) section 2's private block appended to the snippet -> red, the snippet names
'        153204; (7) the sketch set back to 8 MCP tools -> red, README does not say 9 MCP tools.

Imports System.IO
Imports System.Text.RegularExpressions
Imports CodeMem.Bridging
Imports Xunit

''' <summary>
''' Seven facts over the server's instructions, the docs/claude-code/ kit and README.md.
''' </summary>
Public Class UsageTextsGateTests

    Private Shared ReadOnly Whitespace As Regex = New Regex("\s+", RegexOptions.Compiled)
    Private Shared ReadOnly TableTool As Regex = New Regex("\*\*`([^`]+)`\*\*", RegexOptions.Compiled)

    ''' <summary>
    ''' (1) initialize carries instructions equal to BridgeServerInstructions.Text, with no carriage return (FR-511, spec Q12).
    ''' </summary>
    <Fact>
    Public Sub TheInstructionsAreAdvertisedExactlyWithoutCarriageReturns()
        Dim config As String = BridgeHost.WriteConfig(Path.Combine(Path.GetTempPath(), "codemem-tests", "absent-" & Guid.NewGuid().ToString("N") & ".sqlite"), Nothing, False, False)
        Using server As BridgeProcess = BridgeProcess.Serve(config)
            Dim result As System.Text.Json.JsonElement = server.InitializeResult()
            Dim instructions As System.Text.Json.JsonElement
            Assert.True(result.TryGetProperty("instructions", instructions), "initialize carries no instructions")
            Dim advertised As String = instructions.GetString()
            Assert.False(String.IsNullOrEmpty(advertised), "the instructions are empty")
            Assert.Equal(BridgeServerInstructions.Text, advertised)
            Assert.DoesNotContain(vbCr, advertised, StringComparison.Ordinal)
            Assert.Equal(0, server.Close())
        End Using
    End Sub

    ''' <summary>
    ''' (2) Every registered tool is named in the instructions as a whole word - the v1.5.0 gate's test (FR-512).
    ''' </summary>
    <Fact>
    Public Sub EveryRegisteredToolIsNamedInTheInstructions()
        Dim names As String() = BridgeTools.RegisteredToolNames
        Assert.Equal(9, names.Length)
        Dim missing As List(Of String) = New List(Of String)()
        For Each name As String In names
            If Not HasWord(BridgeServerInstructions.Text, name) Then missing.Add(name)
        Next
        Assert.True(missing.Count = 0, "registered tools missing from the instructions: " & String.Join(", ", missing))
    End Sub

    ''' <summary>
    ''' (3) The instructions carry the kit's phrases: the constructor line, the proof line, and what the map does not hold (FR-513, spec Q4).
    ''' </summary>
    <Fact>
    Public Sub TheInstructionsCarryTheKitsPhrases()
        Dim collapsed As String = Collapse(BridgeServerInstructions.Text)
        Dim missing As List(Of String) = New List(Of String)()
        For Each phrase As String In New String() {"VB constructors are all named New", "Prove, don't grep", "Use text search for what the map does not hold", "runtime behaviour"}
            If Not collapsed.Contains(phrase, StringComparison.Ordinal) Then missing.Add(phrase)
        Next
        Assert.True(missing.Count = 0, "phrases missing from the instructions: " & String.Join(" | ", missing))
    End Sub

    ''' <summary>
    ''' (4) The README's tool table lists exactly the registered tools, both directions (FR-521, ruled B).
    ''' </summary>
    <Fact>
    Public Sub TheReadmeToolTableIsExactlyTheRegisteredTools()
        Dim rows As List(Of String) = ReadmeToolRows()
        Assert.True(rows.Count > 0, "no tool rows found under the tools heading: the scan is vacuous")
        Dim registered As HashSet(Of String) = New HashSet(Of String)(BridgeTools.RegisteredToolNames, StringComparer.Ordinal)
        Dim listed As HashSet(Of String) = New HashSet(Of String)(rows, StringComparer.Ordinal)
        Dim missing As List(Of String) = New List(Of String)(registered.Except(listed))
        Dim extra As List(Of String) = New List(Of String)(listed.Except(registered))
        Assert.True(missing.Count = 0 AndAlso extra.Count = 0, "README tool table - missing: " & String.Join(", ", missing) & "; extra: " & String.Join(", ", extra))
    End Sub

    ''' <summary>
    ''' (5) The skill and the snippet each say the map proves structure and runtime behaviour needs its own proof (FR-518).
    ''' </summary>
    <Fact>
    Public Sub TheSkillAndTheSnippetSayTheMapProvesStructure()
        Const Sentence As String = "The map proves structure; runtime behaviour needs its own proof."
        For Each relative As String In New String() {"docs/claude-code/skills/codemem/SKILL.md", "docs/claude-code/CLAUDE.snippet.md"}
            Dim file As String = Path.Combine(RepoPaths.RepositoryRoot(), relative.Replace("/"c, Path.DirectorySeparatorChar))
            Assert.True(IO.File.Exists(file), relative & " does not exist")
            Assert.True(Collapse(IO.File.ReadAllText(file)).Contains(Sentence, StringComparison.Ordinal), relative & " does not carry: " & Sentence)
        Next
    End Sub

    ''' <summary>
    ''' (6) No file under docs/ carries the private block's markers (FR-519): §2's private lines never enter the repository.
    ''' </summary>
    <Fact>
    Public Sub NothingPrivateIsUnderDocs()
        Dim folder As String = Path.Combine(RepoPaths.RepositoryRoot(), "docs")
        Dim files As String() = Directory.GetFiles(folder, "*", SearchOption.AllDirectories)
        Assert.True(files.Length > 0, "no file under docs/: the scan is vacuous")
        Dim offenders As List(Of String) = New List(Of String)()
        For Each file As String In files
            Dim content As String = IO.File.ReadAllText(file)
            For Each marker As String In New String() {"Rick", "153204"}
                If content.Contains(marker, StringComparison.Ordinal) Then offenders.Add(Path.GetRelativePath(folder, file) & ": " & marker)
            Next
        Next
        Assert.True(offenders.Count = 0, String.Join(Environment.NewLine, offenders))
    End Sub

    ''' <summary>
    ''' (7) The README states the tool count as nine in all four places and eight in none (spec Q8 (a)).
    ''' </summary>
    <Fact>
    Public Sub TheReadmeSaysNineTools()
        Dim readme As String = IO.File.ReadAllText(Path.Combine(RepoPaths.RepositoryRoot(), "README.md"))
        For Each nine As String In New String() {"MCP-9%20tools", "9 MCP tools", "Nine, over stdio.", "the bridge and its nine tools"}
            Assert.True(readme.Contains(nine, StringComparison.Ordinal), "README does not say: " & nine)
        Next
        For Each eight As String In New String() {"MCP-8%20tools", "8 MCP tools", "Eight, over stdio", "its eight tools"}
            Assert.False(readme.Contains(eight, StringComparison.Ordinal), "README still says: " & eight)
        Next
    End Sub

    ''' <summary>
    ''' The tool name of every row of the README's table under the tools heading, header and separator rows skipped.
    ''' </summary>
    ''' <returns>The names, in table order.</returns>
    Private Shared Function ReadmeToolRows() As List(Of String)
        Dim lines As String() = IO.File.ReadAllLines(Path.Combine(RepoPaths.RepositoryRoot(), "README.md"))
        Dim names As List(Of String) = New List(Of String)()
        Dim i As Integer = Array.FindIndex(lines, Function(l As String) l.Trim() = "## The tools your assistant gets")
        If i < 0 Then Return names
        Dim inTable As Boolean = False
        For j As Integer = i + 1 To lines.Length - 1
            Dim line As String = lines(j).Trim()
            If line.StartsWith("|", StringComparison.Ordinal) Then
                inTable = True
                Dim cells As String() = line.Split("|"c)
                If cells.Length < 2 Then Continue For
                Dim match As Match = TableTool.Match(cells(1))
                If match.Success Then names.Add(match.Groups(1).Value)
            ElseIf inTable Then
                Exit For
            End If
        Next
        Return names
    End Function

    Private Shared Function Collapse(value As String) As String
        Return Whitespace.Replace(value, " ")
    End Function

    Private Shared Function HasWord(value As String, name As String) As Boolean
        Return Regex.IsMatch(value, "(?<![A-Za-z0-9_])" & Regex.Escape(name) & "(?![A-Za-z0-9_])")
    End Function

End Class

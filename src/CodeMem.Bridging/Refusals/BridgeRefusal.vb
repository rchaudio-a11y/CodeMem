' File: BridgeRefusal.vb
' Project: CodeMem.Bridging
' Description: The one wording owner: every refusal text of contracts/tools.md §6 (004 and 005), built from a kind and the facts it names (FR-306, FR-409, Article XII).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T013): the four new texts (ProjectIdRemoved, ConfigKeyRetired, PathNotInMap in its two shapes, AmbiguousSolutionFile);
' ScopeMissing, SolutionKeyUnknown, TargetMissing and AmbiguousRoot revised to 005 contracts/tools.md §6; Unopenable's store variant gone;
' ScopeConflict gone. The 004 texts of the kinds that leave at T021 and T024 stay until their kind leaves.
' 2026-09-17 (T021): the five resolver texts gone (KeyNotRegistered, KeyUnbound, KeyInactive, MapMissingSolution, PathNotRegistered).

''' <summary>
''' A refusal is text on the wire, never an exception (058's rule). <see cref="Named"/> is the only place a sentence is composed; facts assert
''' each kind's distinguishing phrase, never the whole sentence.
''' </summary>
Public Class BridgeRefusal

    ''' <summary>The kind.</summary>
    Public ReadOnly Property Kind As BridgeRefusalKind

    ''' <summary>The sentence.</summary>
    Public ReadOnly Property Text As String

    Private Sub New(kind As BridgeRefusalKind, text As String)
        Me.Kind = kind
        Me.Text = text
    End Sub

    ''' <summary>
    ''' Composes the refusal of a kind from its facts (a missing fact reads as a question mark, never as an exception).
    ''' </summary>
    ''' <param name="kind">The kind.</param>
    ''' <param name="facts">The named values the sentence carries.</param>
    ''' <returns>The refusal.</returns>
    Public Shared Function Named(kind As BridgeRefusalKind, facts As IDictionary(Of String, String)) As BridgeRefusal
        Return New BridgeRefusal(kind, TextOf(kind, facts))
    End Function

    Private Shared Function TextOf(kind As BridgeRefusalKind, facts As IDictionary(Of String, String)) As String
        Select Case kind
            Case BridgeRefusalKind.Unconfigured
                Dim key As String = F(facts, "key")
                If key = "file" Then Return "The bridge is not configured: no file at '" & F(facts, "configPath") & "'. Create it and call again; nothing was opened."
                If key = "json" Then Return "The bridge is not configured: '" & F(facts, "configPath") & "' is not JSON. Fix it and call again; nothing was opened."
                Return "The bridge is not configured: '" & key & "' is missing from '" & F(facts, "configPath") & "'. Set it and call again; nothing was opened."
            Case BridgeRefusalKind.ConfigKeyRetired
                Return "'" & F(facts, "key") & "' is no longer a key of '" & F(facts, "configPath") & "': the bridge opens no store. Remove it and call again; nothing was opened."
            Case BridgeRefusalKind.MapAbsent
                Return "No CodeMem map exists at '" & F(facts, "mapPath") & "'. Fix mapPath in '" & F(facts, "configPath") & "', or run the CodeMem extractor to create the map. Nothing was created."
            Case BridgeRefusalKind.NotAMap
                Return "The file at '" & F(facts, "mapPath") & "' is not a CodeMem map (no map identity). Point mapPath at a map the CodeMem extractor produced."
            Case BridgeRefusalKind.VersionBelow
                Return "The CodeMem map at '" & F(facts, "mapPath") & "' is at schema version " & F(facts, "found") & "; this bridge requires " & F(facts, "required") & ". Run the CodeMem extractor once: it upgrades the map in place; the bridge is read-only and cannot."
            Case BridgeRefusalKind.VersionAbove
                Return "The CodeMem map at '" & F(facts, "mapPath") & "' is at schema version " & F(facts, "found") & "; this bridge requires " & F(facts, "required") & ". Revise the bridge against the newer contract; nothing to do on the map's side."
            Case BridgeRefusalKind.Busy
                Return "An extraction is in progress on '" & F(facts, "mapPath") & "'; the map was not readable within " & F(facts, "seconds") & " seconds. Retry when it finishes; this is normal for a large solution."
            Case BridgeRefusalKind.Unopenable
                Return "The CodeMem map at '" & F(facts, "path") & "' could not be opened: " & F(facts, "driver") & ". Check the path and its permissions."
            Case BridgeRefusalKind.RegistryAbsent
                Return "The store at '" & F(facts, "storePath") & "' holds no code_map_solutions table. Nothing is wrong with the map; the registry migration has not gone live on that store."
            Case BridgeRefusalKind.ScopeMissing
                Return "Supply solutionKey — one map solution, exact; solutions lists the keys. Nothing was opened."
            Case BridgeRefusalKind.ProjectIdRemoved
                Return "projectId is not an argument of this bridge: scope by solutionKey (solutions lists the keys). MemOS's own codemem tools take a project id. Nothing was opened."
            Case BridgeRefusalKind.FilterMissing
                Return "Supply a name, a kind, or both. A search with no filter is not run."
            Case BridgeRefusalKind.KindUnknown
                Return "'" & F(facts, "given") & "' is not a symbol kind in the CodeMem map. Use one of: " & F(facts, "kinds") & "."
            Case BridgeRefusalKind.SymbolIdMissing
                Return "Supply symbolId — the map's symbol id, as symbol_search returns it."
            Case BridgeRefusalKind.KindNotExamined
                Return "'" & F(facts, "given") & "' is a symbol kind orphans does not examine: " & F(facts, "reason") & ". Filter by an examined kind, or omit kind."
            Case BridgeRefusalKind.SolutionKeyUnknown
                Return "The CodeMem map at '" & F(facts, "mapPath") & "' holds no solution with key '" & F(facts, "key") & "'. Keys are exact; solutions lists them. To add a solution, run: extract --solution-key " & F(facts, "key") & " --solution <path to its .sln or .slnx>."
            Case BridgeRefusalKind.SymbolNotFound
                Return "The CodeMem map at '" & F(facts, "mapPath") & "' holds no symbol with id " & F(facts, "symbolId") & ". Find the id with symbol_search."
            Case BridgeRefusalKind.SymbolOutOfScope
                Return "Symbol " & F(facts, "id") & " ('" & F(facts, "name") & "') belongs to solution '" & F(facts, "solutionKey") & "', which is not in the requested scope (" & F(facts, "scope") & "). Ask with that solution's key."
            Case BridgeRefusalKind.SymbolRetired
                Return "Symbol " & F(facts, "id") & " ('" & F(facts, "name") & "', " & F(facts, "kind") & ", " & F(facts, "path") & ") in solution '" & F(facts, "solutionKey") & "' is retired; it was last seen in extract run " & F(facts, "lastSeenRunId") & ". Search again for the current symbol, or consult the rename candidates whose retired symbol is " & F(facts, "id") & "."
            Case BridgeRefusalKind.NotAProjectRow
                Return "Symbol " & F(facts, "id") & " ('" & F(facts, "name") & "', " & F(facts, "kind") & ", " & F(facts, "path") & ":" & F(facts, "line") & ") is not a project row; projectSymbolId takes the id of a project-kind symbol — the ids the result's byProject lists."
            Case BridgeRefusalKind.NotAType
                Return "Symbol " & F(facts, "id") & " ('" & F(facts, "name") & "', " & F(facts, "kind") & ") is not a type; type_usages takes a class, module, structure, interface, enum or delegate. Use references for a member."
            Case BridgeRefusalKind.GateOff
                Return "extract is refused: " & F(facts, "gate") & " is false in '" & F(facts, "configPath") & "'. The Architect flips it; nothing ran and the map is unchanged."
            Case BridgeRefusalKind.TargetMissing
                Return "Supply exactly one of solutionKey, repoPath or stale; solutionPath only beside solutionKey. Nothing ran."
            Case BridgeRefusalKind.PathNotInMap
                If facts IsNot Nothing AndAlso facts.ContainsKey("file") Then
                    Return "'" & F(facts, "path") & "' is not in the map: no mapped solution's root contains it. Mapped roots: " & F(facts, "roots") & ". It holds " & F(facts, "file") & "; to add it, run: " & F(facts, "command") & " (the extract tool: solutionKey and solutionPath). Nothing ran and nothing was added."
                End If
                Return "'" & F(facts, "path") & "' is not in the map: no mapped solution's root contains it, and it holds no solution file. Mapped roots: " & F(facts, "roots") & ". To add a solution, run: extract --solution-key <key> --solution <path to its .sln or .slnx>. Nothing ran and nothing was added."
            Case BridgeRefusalKind.AmbiguousSolutionFile
                Return "'" & F(facts, "path") & "' is not in the map and holds more than one solution file (" & F(facts, "files") & "); no key is suggested. Choose one and run: extract --solution-key <key> --solution <that file>. Nothing ran and nothing was added."
            Case BridgeRefusalKind.AmbiguousRoot
                Return "'" & F(facts, "path") & "' lies under a root shared by more than one mapped solution (" & F(facts, "keys") & "); name the solutionKey. Nothing ran."
            Case BridgeRefusalKind.ExtractionRunning
                Return "An extraction launched by this bridge is still running (" & F(facts, "key") & "); wait for its result. Nothing ran."
            Case BridgeRefusalKind.ExtractorNotFound
                Return "The extractor was not found at '" & F(facts, "extractorPath") & "'. Set extractorPath in '" & F(facts, "configPath") & "' or build CodeMem.sln. Nothing ran."
            Case BridgeRefusalKind.AmbiguousTarget
                Return "The build names more than one target (" & F(facts, "candidates") & "); the hook resolves none of them and never falls back to the working directory. Nothing ran."
            Case BridgeRefusalKind.ChildTimedOut
                Return "The extractor for '" & F(facts, "key") & "' exceeded " & F(facts, "budget") & " s and was stopped; the map holds whatever it published before and nothing after. Run it by hand to see why."
            Case Else
                Throw New ArgumentOutOfRangeException(NameOf(kind), kind, "unknown refusal kind")
        End Select
    End Function

    Private Shared Function F(facts As IDictionary(Of String, String), key As String) As String
        Dim value As String = Nothing
        If facts IsNot Nothing AndAlso facts.TryGetValue(key, value) AndAlso value IsNot Nothing Then Return value
        Return "?"
    End Function

End Class

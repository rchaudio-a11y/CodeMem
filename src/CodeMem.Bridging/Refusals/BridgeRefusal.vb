' File: BridgeRefusal.vb
' Project: CodeMem.Bridging
' Description: The one wording owner: every refusal text of contracts/tools.md §6, built from a kind and the facts it names (FR-306, Article XII).
' Author: RCH Automation LLC
' Created: 2026-09-15

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
                If F(facts, "role") = "store" Then Return "The MemOS store at '" & F(facts, "path") & "' could not be opened: " & F(facts, "driver") & ". Check the path and its permissions."
                Return "The CodeMem map at '" & F(facts, "path") & "' could not be opened: " & F(facts, "driver") & ". Check the path and its permissions."
            Case BridgeRefusalKind.RegistryAbsent
                Return "The MemOS store at '" & F(facts, "storePath") & "' holds no code_map_solutions table. Nothing is wrong with the map; the registry migration has not gone live on that store."
            Case BridgeRefusalKind.ScopeMissing
                Return "Supply exactly one of projectId (a MemOS project, resolved through the code_map_solutions registry) or solutionKey (one map solution). Neither was supplied."
            Case BridgeRefusalKind.ScopeConflict
                Return "Supply exactly one of projectId or solutionKey. Both were supplied; they can disagree, so neither is chosen for you."
            Case BridgeRefusalKind.FilterMissing
                Return "Supply a name, a kind, or both. A search with no filter is not run."
            Case BridgeRefusalKind.KindUnknown
                Return "'" & F(facts, "given") & "' is not a symbol kind in the CodeMem map. Use one of: " & F(facts, "kinds") & "."
            Case BridgeRefusalKind.SymbolIdMissing
                Return "Supply symbolId — the map's symbol id, as symbol_search returns it."
            Case BridgeRefusalKind.KindNotExamined
                Return "'" & F(facts, "given") & "' is a symbol kind orphans does not examine: " & F(facts, "reason") & ". Filter by an examined kind, or omit kind."
            Case BridgeRefusalKind.SolutionKeyUnknown
                Return "The CodeMem map at '" & F(facts, "mapPath") & "' holds no solution with key '" & F(facts, "key") & "'. Keys are exact; solutions lists them."
            Case BridgeRefusalKind.SymbolNotFound
                Return "The CodeMem map at '" & F(facts, "mapPath") & "' holds no symbol with id " & F(facts, "symbolId") & ". Find the id with symbol_search."
            Case BridgeRefusalKind.SymbolOutOfScope
                Return "Symbol " & F(facts, "id") & " ('" & F(facts, "name") & "') belongs to solution '" & F(facts, "solutionKey") & "', which is not in the requested scope (" & F(facts, "scope") & "). Ask with that solution's key, or with a project bound to it."
            Case BridgeRefusalKind.SymbolRetired
                Return "Symbol " & F(facts, "id") & " ('" & F(facts, "name") & "', " & F(facts, "kind") & ", " & F(facts, "path") & ") in solution '" & F(facts, "solutionKey") & "' is retired; it was last seen in extract run " & F(facts, "lastSeenRunId") & ". Search again for the current symbol, or consult the rename candidates whose retired symbol is " & F(facts, "id") & "."
            Case BridgeRefusalKind.NotAProjectRow
                Return "Symbol " & F(facts, "id") & " ('" & F(facts, "name") & "', " & F(facts, "kind") & ", " & F(facts, "path") & ":" & F(facts, "line") & ") is not a project row; projectSymbolId takes the id of a project-kind symbol — the ids the result's byProject lists."
            Case BridgeRefusalKind.NotAType
                Return "Symbol " & F(facts, "id") & " ('" & F(facts, "name") & "', " & F(facts, "kind") & ") is not a type; type_usages takes a class, module, structure, interface, enum or delegate. Use references for a member."
            Case BridgeRefusalKind.GateOff
                Return "extract is refused: " & F(facts, "gate") & " is false in '" & F(facts, "configPath") & "'. The Architect flips it; nothing ran and the map is unchanged."
            Case BridgeRefusalKind.TargetMissing
                Return "Supply exactly one of solutionKey, repoPath or stale. Nothing ran."
            Case BridgeRefusalKind.PathNotRegistered
                Return "No registered solution's repository root contains '" & F(facts, "path") & "'. Registered roots: " & F(facts, "roots") & ". Nothing ran."
            Case BridgeRefusalKind.AmbiguousRoot
                Return "'" & F(facts, "path") & "' lies under a root bound to more than one registered solution (" & F(facts, "keys") & "); name the solutionKey. Nothing ran."
            Case BridgeRefusalKind.KeyNotRegistered
                Return "No code_map_solutions row has solution_key '" & F(facts, "key") & "'. Keys are exact; solutions lists the map's, map_status the registry's. Nothing ran."
            Case BridgeRefusalKind.KeyUnbound
                Return "Registry row '" & F(facts, "key") & "' has no codemem_solution_id: it has never been published. Run the extractor by hand once with --solution-key " & F(facts, "key") & " and bind the row in MemOS; the bridge does not extract an unbound solution."
            Case BridgeRefusalKind.KeyInactive
                Return "Registry row '" & F(facts, "key") & "' is " & F(facts, "state") & ", not active. Nothing ran."
            Case BridgeRefusalKind.MapMissingSolution
                Return "Registry row '" & F(facts, "key") & "' binds map solution " & F(facts, "id") & ", but the map at '" & F(facts, "mapPath") & "' holds no solution " & F(facts, "id") & ". Nothing ran."
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

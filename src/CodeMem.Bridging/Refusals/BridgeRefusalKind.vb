' File: BridgeRefusalKind.vb
' Project: CodeMem.Bridging
' Description: The closed set of refusal kinds, one per distinct remedy (FR-306; 005 FR-409; data-model §10 / 005 §9).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T013): ProjectIdRemoved, ConfigKeyRetired, PathNotInMap and AmbiguousSolutionFile added; ScopeConflict retired
' (one scope argument remains). The registry-side kinds leave in two steps as their callers do: KeyNotRegistered, KeyUnbound, KeyInactive,
' MapMissingSolution and PathNotRegistered at T021 with the resolver; RegistryAbsent at T024 with the archive. The count is 29 at T024.
' 2026-09-17 (T021): the five resolver kinds gone with TargetResolver's rewrite; 30 remain until RegistryAbsent leaves at T024.

''' <summary>
''' Every way a tool refuses. The wording of each lives in <see cref="BridgeRefusal"/>; facts assert each kind's distinguishing phrase.
''' </summary>
Public Enum BridgeRefusalKind
    ''' <summary>The configuration file or a required key is missing.</summary>
    Unconfigured
    ''' <summary>The configuration file carries a key the bridge no longer reads (storePath, feature 005).</summary>
    ConfigKeyRetired
    ''' <summary>No file at mapPath.</summary>
    MapAbsent
    ''' <summary>The file at mapPath is not SQLite or has no map identity.</summary>
    NotAMap
    ''' <summary>The map is at a schema version below the pin.</summary>
    VersionBelow
    ''' <summary>The map is at a schema version above the pin.</summary>
    VersionAbove
    ''' <summary>The map was not readable within the busy timeout.</summary>
    Busy
    ''' <summary>The map could not be opened for another reason.</summary>
    Unopenable
    ''' <summary>The store holds no code_map_solutions table (004; retired at T024 with the archive).</summary>
    RegistryAbsent
    ''' <summary>No solutionKey.</summary>
    ScopeMissing
    ''' <summary>The call carries projectId, which this bridge does not take (feature 005).</summary>
    ProjectIdRemoved
    ''' <summary>symbol_search with neither name nor kind.</summary>
    FilterMissing
    ''' <summary>A kind that is not one of the fourteen.</summary>
    KindUnknown
    ''' <summary>No symbolId.</summary>
    SymbolIdMissing
    ''' <summary>A kind orphans does not examine.</summary>
    KindNotExamined
    ''' <summary>No map solution with that key.</summary>
    SolutionKeyUnknown
    ''' <summary>No symbol with that id.</summary>
    SymbolNotFound
    ''' <summary>The symbol belongs to a solution outside the scope.</summary>
    SymbolOutOfScope
    ''' <summary>The symbol is retired.</summary>
    SymbolRetired
    ''' <summary>projectSymbolId is not a project row.</summary>
    NotAProjectRow
    ''' <summary>type_usages on a non-type.</summary>
    NotAType
    ''' <summary>extract refused by a gate.</summary>
    GateOff
    ''' <summary>No mapped solution's root contains the directory; the answer names the command that adds it (feature 005).</summary>
    PathNotInMap
    ''' <summary>The directory is not in the map and holds more than one solution file; no key is suggested (feature 005).</summary>
    AmbiguousSolutionFile
    ''' <summary>repoPath lies under a root shared by more than one mapped solution.</summary>
    AmbiguousRoot
    ''' <summary>An extraction launched by this bridge is still running.</summary>
    ExtractionRunning
    ''' <summary>The extractor was not found.</summary>
    ExtractorNotFound
    ''' <summary>extract with none of solutionKey, repoPath or stale, or solutionPath without solutionKey.</summary>
    TargetMissing
    ''' <summary>The hook's command names more than one target.</summary>
    AmbiguousTarget
    ''' <summary>The extractor exceeded its budget and was stopped.</summary>
    ChildTimedOut
End Enum

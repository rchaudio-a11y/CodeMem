' File: BridgeRefusalKind.vb
' Project: CodeMem.Bridging
' Description: The closed set of refusal kinds, one per distinct remedy (FR-306; data-model §10).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Every way a tool refuses. The wording of each lives in <see cref="BridgeRefusal"/>; facts assert each kind's distinguishing phrase.
''' </summary>
Public Enum BridgeRefusalKind
    ''' <summary>The configuration file or a required key is missing.</summary>
    Unconfigured
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
    ''' <summary>The store holds no code_map_solutions table.</summary>
    RegistryAbsent
    ''' <summary>Neither projectId nor solutionKey.</summary>
    ScopeMissing
    ''' <summary>Both projectId and solutionKey.</summary>
    ScopeConflict
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
    ''' <summary>No registered root contains repoPath.</summary>
    PathNotRegistered
    ''' <summary>repoPath lies under a root bound to more than one solution.</summary>
    AmbiguousRoot
    ''' <summary>No registry row has that key.</summary>
    KeyNotRegistered
    ''' <summary>The registry row has no codemem_solution_id.</summary>
    KeyUnbound
    ''' <summary>The registry row is not active.</summary>
    KeyInactive
    ''' <summary>The registry binds an id the map does not hold.</summary>
    MapMissingSolution
    ''' <summary>An extraction launched by this bridge is still running.</summary>
    ExtractionRunning
    ''' <summary>The extractor was not found.</summary>
    ExtractorNotFound
    ''' <summary>extract with none of solutionKey, repoPath or stale.</summary>
    TargetMissing
    ''' <summary>The hook's command names more than one target.</summary>
    AmbiguousTarget
    ''' <summary>The extractor exceeded its budget and was stopped.</summary>
    ChildTimedOut
End Enum

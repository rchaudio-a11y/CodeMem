# Contract: the eight tools of `CodeMem.Bridge`

**Feature**: `004-codemem-bridge` | **Date**: 2026-09-15 | **Spec**: [../spec.md](../spec.md) | **Model**: [../data-model.md](../data-model.md)

Server name `codemem` (stdio; `.mcp.json` in [cli-config-hook.md](cli-config-hook.md)); the model sees
`mcp__codemem__<tool>`. JSON is camelCase and **every documented property is always serialised, `null` included**
(058's rule). A refusal is a `CallToolResult` with `IsError = true` and one text block; nothing is thrown on the wire.
No audit row anywhere (Q7). The five ported tools keep the MemOS shapes by section reference; only differences are
spelled out.

## 1. Parameters

| Parameter | Type | solutions | symbol_search | symbol_detail | references | orphans | type_usages | map_status | extract |
|---|---|---|---|---|---|---|---|---|---|
| `projectId` | integer | — | exactly one of the two | 〃 | 〃 | 〃 | 〃 | — | — |
| `solutionKey` | string | — | 〃 | 〃 | 〃 | 〃 | 〃 | — | one of the three |
| `name` / `kind` | string | — | at least one | — | — | `kind` optional | — | — | — |
| `projectSymbolId` | integer | — | optional | — | — | optional | — | — | — |
| `symbolId` | integer | — | — | required | required | — | required (a type) | — | — |
| `repoPath` | string | — | — | — | — | — | — | — | one of the three |
| `stale` | boolean | — | — | — | — | — | — | — | one of the three (`true`) |

Whitespace-only strings count as not supplied. Every parameter is optional on the wire and missing required
values are refused by name (058 R12).

## 2. Descriptions (as registered; the phrases in bold are asserted by a fact, FR-312)

**`solutions`** — "Every solution in the CodeMem code map with its latest extraction run: key, name, repo root,
last-seen path; outcome, schema and SDK version, source digest, commit and dirty state (**unknown when there is no
commit**), start and finish, and the ten reconciliation counts. Read-only; the map's extractor is its only writer.
Refuses by name when the bridge is unconfigured or the map is absent, not a map, at another schema version, or busy
under an extraction. Call `map_status` to learn whether a run is current."

**`symbol_search`** — "Find symbols in the CodeMem code map by name and/or kind, scoped to a MemOS project
(projectId — resolved through the code_map_solutions registry to every solution bound to that project) or to one
map solution (solutionKey), optionally narrowed to one project row (projectSymbolId — the id of a project-kind row
inside the scope; the filter narrows the rows and never changes what the total counts). The name matches
case-insensitively anywhere in the symbol's name; kind is one of: namespace, class, module, structure, interface,
enum, enum_member, delegate, method, constructor, property, field, event, project. Returns active symbols only, **at
most 200** declarations, always with the total matched and whether the result was truncated — **narrow the filter
rather than page**. **A file linked into several projects declares one symbol per project; such twins are presented
as one declaration compiled into N projects, carrying every project's symbol id.** A projectId with no registry row
is not an error: the result says it searched nothing and why. Read-only."

**`symbol_detail`** — "One active symbol whole: its fields, every part that declares it (a partial class has
several; the one containing the symbol's name is flagged; a project has none), and its outbound and inbound edges
grouped under all **eight verbs** — part_of, calls, uses, implements, extends, imports, depends_on, handles — with an
empty list meaning none. An outbound edge to a symbol outside the solution (framework, package) carries its
doc-comment id and is **marked external**. When the declaration is compiled into several projects the header names
every twin's id; the parts and edges are the requested id's own. Requires symbolId and exactly one of projectId or
solutionKey; a symbol **outside that scope**, one that **does not exist**, and one that is **retired** are each
refused by name. Read-only."

**`references`** — "Every recorded occurrence whose target is the given active symbol — resolved by the compiler's
identity, not by matching text — with path, line, column, the verb, and the symbol it occurs in. **Containment
(part_of) is not a reference and is never included**; use symbol_detail's inbound part_of for what a type contains.
**Does not show AddHandler … AddressOf wiring sites**: the map records each one as the handler's own outbound handles
edge, not as an occurrence targeting it — they appear in the handler's symbol_detail, under outbound handles. **A
field or property read or written by its bare name, and a method passed by a bare AddressOf, are recorded as calls
occurrences and appear here (CodeMem 003). A bare-name use whose target lies outside the map (framework, package)
is not recorded.** A constructor call (`New X(…)`) targets the constructor, not the type: for everything the map
records against a type — its constructors and members included — use type_usages. Returns every occurrence,
uncapped. **Takes a mapped symbol id only**; external targets are not addressable here. Requires symbolId and exactly
one of projectId or solutionKey. Read-only."

**`orphans`** — the 060 text verbatim in substance, with `codemem_` prefixes dropped: "Active symbols in the CodeMem
code map that no recorded reference reaches, scoped to a MemOS project (projectId, resolved through the registry) or
to one map solution (solutionKey), optionally narrowed to one kind (kind) or to one project row within the scope
(projectSymbolId — the id the result's byProject lists); a filter narrows the rows, it never changes what counts as
an orphan, and the totals describe the narrowed set and state the filter. A reference is any edge except part_of
(containment) from outside the symbol, and a reference to a member counts as a use of the type or module that
contains it. A method wired by a Handles clause, and a member that implements an interface member, are not reported:
the map cannot see event or interface dispatch. Fields and properties are examined: since CodeMem 003 the map records
a field or property **used by its bare name**. Namespaces and projects are not examined, and every result names
them: the map holds **no namespace rows**, and project rows are roots, **unreferenced by construction**. An orphan is
a symbol with no recorded reference, **not a verdict that it is dead**. The result includes live code whose use the
map cannot see: entry points (**Main**); test classes and test methods, and anything else found by **reflection** or
attributes; **Overrides** members called through their base; **InitializeComponent**, called from a Designer form's
compiler-generated constructor; constructors that exist only to **prevent instantiation**; a bare-name use whose
target lies outside the map, which is not recorded; methods used only by **AddressOf** with a bare name; anything
consumed from **outside the solution**, such as a library's public surface; and code **generated outside the solution
tree**. It does not report a member **referenced only by a sibling**: a group that references only itself surfaces as
its outermost unreferenced container plus any member nothing references. It does not report a contract that is
**implemented but never called**. Returns every orphan, uncapped, with counts per declaring project and per kind.
Read-only; the map's extractor is its only writer."

**`type_usages`** — "Everything the CodeMem code map records against one type — class, module, structure,
interface, enum or delegate — in one call: references to the type itself, **calls to each of its constructors**
(`New X(…)` targets the constructor, so references on the type alone reads 0), calls and uses of each of its direct
members, and every implements or extends edge pointing at it. Grouped by verb with counts, then the flat occurrence
list; each occurrence names the symbol it actually targets (the type, a constructor or a member) and whether it
comes **from inside** the type (a member calling a sibling). **total** counts every occurrence; **fromOutside** is the
number to read before removing or renaming it. Resolved by the compiler's identity, uncapped, active symbols only;
containment (part_of) is not a use. A symbolId that is not a type is refused by name — use references. Requires
symbolId and exactly one of projectId or solutionKey. Read-only."

**`map_status`** — "For every solution the code_map_solutions registry binds to the CodeMem map: the commit and
dirty flag its latest completed run recorded, what HEAD is now in the registered repository root, whether the tree
is dirty, how many commits HEAD is past the recorded one, and **one verdict by name**: **current**, **behind** (by N,
HEAD named), **dirty** (the tree has uncommitted or untracked changes), **no_git** (the root cannot be read as a
repository, or the run recorded no commit), **map_missing_solution** (the registry binds an id the map does not
hold), or **diverged** (the recorded commit is not an ancestor of HEAD; no count is claimed). Nothing is guessed: a
repository the bridge cannot read says so. Registered solutions with no binding, and inactive rows, are listed
without a verdict. Call this at session start and before relying on references, orphans or type_usages; an
extraction is the remedy (the hook on a green build, or extract). Read-only."

**`extract`** — "Refreshes the CodeMem map by running the CodeMem extractor as a child process for one registered
solution — named by solutionKey, or by a repoPath that lies under a registered repository root — or, with stale set
to true, for every bound solution whose map_status is behind, dirty or diverged, in sequence, one run each. The
solution key is always taken from the code_map_solutions registry; a path under no registered root, and a key with
no active bound row, are refused by name and nothing runs. **Gated**: extract.enabled in bridge.config.json must be
true (default false) — the automatic green-build trigger additionally needs extract.onGreenBuild. A refusal names
the gate that stopped it. Returns the extractor's exit code, its summary or refusal line verbatim, and the
published run's ten counts. The bridge never writes the map itself; the extractor is its only writer. Normally
called by the green-build hook, not directly; a human runs extract with stale."

## 3. Result shapes

### 3.1 `solutions` — 056 §1.1 unchanged (`map`, `mapPath`, `readAtUtc`, `solutions[]` with `latestRun`). `latestRun` is
the latest run of any outcome, as 056 defines it (INC4); `map_status` compares the latest completed run.

### 3.2 `symbol_search` — 058 §3.2 with each element of `symbols[]` a Declaration:

```json
{ "readAtUtc": "…", "scope": { "by": "solutionKey", "projectId": null, "solutionKey": "MemOS",
    "solutions": [ { "solutionId": 3, "key": "MemOS" } ], "activeRegistryRows": null, "unboundRegistryRows": null,
    "inactiveRegistryRows": null, "danglingSolutionIds": null, "hadNothingToSearch": false },
  "filters": { "name": "CodeMemMapFixture", "kind": "class", "projectSymbolId": null },
  "total": 1, "truncated": false,
  "symbols": [
    { "solutionId": 3, "solutionKey": "MemOS", "id": 3556, "docCommentId": "T:MemOS.ContractTests.TestSupport.CodeMemMapFixture",
      "kind": "class", "name": "CodeMemMapFixture", "container": { "id": 3540, "name": "TestSupport" },
      "project": { "id": 2903, "name": "MemOS.ContractTests" },
      "path": "tests/contract/MemOS.ContractTests/TestSupport/CodeMemMapFixture.vb", "line": 43,
      "compiledInto": [
        { "projectSymbolId": 2903, "projectName": "MemOS.ContractTests", "symbolId": 3556, "docCommentId": "T:MemOS.ContractTests.TestSupport.CodeMemMapFixture" },
        { "projectSymbolId": 2906, "projectName": "MemOS.IntegrationTests", "symbolId": 4200, "docCommentId": "T:MemOS.IntegrationTests.TestSupport.CodeMemMapFixture" } ] } ] }
```

`id`, `docCommentId`, `container` and `project` at the top level are the first twin's (lowest id); `total` counts
declarations; `truncated` is true when more than 200 declarations matched. The `container` value above is
illustrative; the live acceptance records it.

### 3.3 `symbol_detail` — 058 §3.3 with `symbol.compiledInto` (the requested id first) added; `parts`, `outbound`,
`inbound` are the requested id's.

### 3.4 `references` — 058 §3.4 unchanged.

### 3.5 `orphans` — 060 §3.1 unchanged (`filters`, `notExamined`, `total`, `byProject`, `byKind`, `orphans`).

### 3.6 `type_usages`

```json
{ "readAtUtc": "…", "scope": { "…": "058 §3.1" },
  "symbol": { "solutionId": 3, "solutionKey": "MemOS", "id": 3023, "docCommentId": "T:MemOS.Business.Managers.CodeMemMapReader",
              "kind": "class", "name": "CodeMemMapReader", "path": "MemOS.Business/Managers/CodeMemMapReader.vb", "line": 29 },
  "byVerb": { "calls": 20, "uses": 0, "implements": 0, "extends": 0, "imports": 0, "depends_on": 0, "handles": 0 },
  "total": 20, "fromOutside": 16,
  "occurrences": [
    { "verb": "calls", "path": "tests/contract/MemOS.ContractTests/…/CodeMemMapReaderTests.vb", "line": 55, "column": 30,
      "source": { "id": 3601, "name": "ReadsTheLiveShape", "kind": "method" },
      "target": { "id": 6661, "docCommentId": "M:MemOS.Business.Managers.CodeMemMapReader.#ctor(…)", "name": "New", "kind": "constructor", "role": "constructor", "external": false },
      "fromInside": false },
    { "verb": "calls", "path": "MemOS.Business/Managers/CodeMemMapReader.vb", "line": 41, "column": 9,
      "source": { "id": 6661, "name": "New", "kind": "constructor" },
      "target": { "id": 6662, "docCommentId": "F:MemOS.Business.Managers.CodeMemMapReader._paths", "name": "_paths", "kind": "field", "role": "member", "external": false },
      "fromInside": true } ] }
```

Numbers are the live measurement of 2026-09-15 (research); paths and source ids in the two rows are illustrative
until the acceptance records them. Order: by verb (the seven in the fixed order), then path, line, column.

### 3.7 `map_status`

```json
{ "readAtUtc": "…", "mapPath": "C:\\_DB\\codemem.sqlite", "storePath": "C:\\_DB\\memos.sqlite",
  "bound": 3,
  "entries": [
    { "solutionKey": "MemOS", "projectId": 107243, "codememSolutionId": 3,
      "run": { "runId": 6, "commitSha": "806f5f3fe45d05628f0c8370e337c6d16e43578c", "isDirty": false, "finishedUtc": "2026-09-13T23:53:46.2840611Z" },
      "repoRoot": "C:\\Users\\rchau\\source\\repos\\rchaudio-a11y\\MemOS\\",
      "head": { "sha": "cbeb66158622236c38f9fd38b00cd20b789b1974", "treeDirty": false, "behindBy": 11, "note": null },
      "verdict": "behind", "reason": "HEAD cbeb6615 is 11 commits past the recorded 806f5f3f; the tree is clean." },
    { "solutionKey": "GameRoom", "projectId": 132040, "codememSolutionId": 1,
      "run": { "runId": 4, "commitSha": "e1bbe024d9a49acc5f8756c2ebc79bb38092bb13", "isDirty": false, "finishedUtc": "…" },
      "repoRoot": "C:\\Users\\rchau\\source\\repos\\GameRoom\\",
      "head": { "sha": "e1bbe024d9a49acc5f8756c2ebc79bb38092bb13", "treeDirty": true, "behindBy": 0, "note": null },
      "verdict": "dirty", "reason": "HEAD equals the recorded e1bbe024 but the working tree has uncommitted or untracked changes." },
    { "solutionKey": "CodeMem", "projectId": 131373, "codememSolutionId": 2, "run": { "runId": 5, "commitSha": "f6488469…", "isDirty": false, "finishedUtc": "…" },
      "repoRoot": "C:\\Users\\rchau\\source\\repos\\CodeMem\\", "head": { "sha": "b2e0168e…", "treeDirty": false, "behindBy": 1, "note": null },
      "verdict": "behind", "reason": "HEAD b2e0168e is 1 commit past the recorded f6488469; the tree is clean." } ],
  "unbound": [], "inactive": [] }
```

Live values of 2026-09-15 (the CodeMem tree will read `dirty` while this feature is uncommitted). `note` carries the
repository's own message when a fact could not be read ("root is not a repository working directory: found …",
"no commit", "recorded commit not in this repository").

### 3.8 `extract`

```json
{ "readAtUtc": "…", "origin": "tool",
  "target": { "asGiven": "solutionKey=MemOS", "resolvedKey": "MemOS", "solutionPath": "C:\\…\\MemOS\\MemOS.sln", "mapPath": "C:\\_DB\\codemem.sqlite" },
  "gate": "passed", "refusal": null, "launched": true, "exitCode": 0, "timedOut": false,
  "line": "solution=MemOS run_id=7 observed=… matched=… reactivated=0 new=… retired=… registry_before=14713 notes_orphaned=0 candidates=0 unaccounted_observed=0 unaccounted_registry=0 digest=… sha=cbeb66158622236c38f9fd38b00cd20b789b1974",
  "run": { "runId": 7, "symbolsObserved": 0, "symbolsMatched": 0, "symbolsReactivated": 0, "symbolsNew": 0, "symbolsRetired": 0,
           "registryActiveBefore": 0, "notesOrphaned": 0, "renameCandidates": 0, "unaccountedObserved": 0, "unaccountedRegistry": 0 },
  "elapsedMs": 31000, "logged": true, "logError": null }
```

A refused call returns the same object with `gate` naming the gate or `refusal` naming the kind, `launched: false`,
`exitCode: null`, `line: null`, `run: null` — and is **still** `IsError = true` on the wire, so the caller cannot
mistake it for a run. With `stale: true` the result is `{ "readAtUtc", "origin", "considered": [ { "solutionKey",
"verdict", "extracted", "result": <extract result or null> } ], "extractedCount" }`.

## 4. Scope resolution, identity, order of refusal

As 058 §6, with the registry replacing the `projects` table: arguments (`ScopeMissing`/`ScopeConflict` →
`FilterMissing` → `KindUnknown` / `SymbolIdMissing` / `KindNotExamined` / `TargetMissing`) → configuration
(`Unconfigured`) → store (`RegistryAbsent`) → map (`MapAbsent` → `NotAMap` → `VersionBelow` / `VersionAbove` →
`Busy` / `Unopenable`) → question (`SolutionKeyUnknown` → `SymbolNotFound` → `SymbolOutOfScope` → `SymbolRetired` →
`NotAProjectRow` / `NotAType`); for `extract`: `TargetMissing` (cardinality, before the configuration is read — INC2) → `Unconfigured` → `GateOff` →
`RegistryAbsent` → the map kinds → `KeyNotRegistered` / `KeyInactive` / `KeyUnbound` / `PathNotRegistered` /
`AmbiguousRoot` → `MapMissingSolution` → `ExtractionRunning` → `ExtractorNotFound`; a child that exceeds its budget is
`ChildTimedOut`. The gate is checked before resolution so a disabled verb reveals nothing about the registry.

## 5. The presentation rule (FR-340, FR-341)

Applied by one module both search and detail call: group key = (solution_id, kind, name, path, start_line);
fold only when the group's `project_symbol_id` values are all distinct and more than one; order twins by symbol id.
Retired rows are never in a group.

## 6. Refusal texts (proposed; facts assert the bold phrases)

| Kind | Text |
|---|---|
| `Unconfigured` | The bridge is **not configured**: '{key}' is missing from '{configPath}'. Set it and call again; nothing was opened. |
| `MapAbsent` | **No CodeMem map exists** at '{mapPath}'. Fix mapPath in '{configPath}', or run the CodeMem extractor to create the map. Nothing was created. |
| `NotAMap` | The file at '{mapPath}' is **not a CodeMem map** (no map identity). Point mapPath at a map the CodeMem extractor produced. |
| `VersionBelow` | The CodeMem map at '{mapPath}' is at schema version {found}; this bridge requires {required}. **Run the CodeMem extractor once: it upgrades the map in place**; the bridge is read-only and cannot. |
| `VersionAbove` | The CodeMem map at '{mapPath}' is at schema version {found}; this bridge requires {required}. **Revise the bridge against the newer contract**; nothing to do on the map's side. |
| `Busy` | **An extraction is in progress** on '{mapPath}'; the map was not readable within {n} seconds. Retry when it finishes; this is normal for a large solution. |
| `Unopenable` | The CodeMem map at '{mapPath}' **could not be opened**: {driver text}. Check the path and its permissions. |
| `RegistryAbsent` | The MemOS store at '{storePath}' holds **no code_map_solutions table**. Nothing is wrong with the map; the registry migration has not gone live on that store. |
| `ScopeMissing` | Supply **exactly one of projectId** (a MemOS project, resolved through the code_map_solutions registry) or solutionKey (one map solution). **Neither** was supplied. |
| `ScopeConflict` | Supply **exactly one of projectId** or solutionKey. **Both** were supplied; they can disagree, so neither is chosen for you. |
| `FilterMissing` | Supply a **name, a kind, or both**. A search with no filter is not run. |
| `KindUnknown` | '{given}' is **not a symbol kind** in the CodeMem map. Use one of: {the fourteen kinds, comma-separated}. |
| `SymbolIdMissing` | Supply **symbolId** — the map's symbol id, as symbol_search returns it. |
| `SolutionKeyUnknown` | The CodeMem map at '{mapPath}' **holds no solution with key** '{key}'. Keys are exact; solutions lists them. |
| `SymbolNotFound` | The CodeMem map at '{mapPath}' **holds no symbol with id** {symbolId}. Find the id with symbol_search. |
| `SymbolOutOfScope` | Symbol {id} ('{name}') belongs to solution '{solutionKey}', which is **not in the requested scope** ({scope description}). Ask with that solution's key, or with a project bound to it. |
| `SymbolRetired` | Symbol {id} ('{name}', {kind}, {path}) in solution '{solutionKey}' is **retired**; it was last seen in extract run {lastSeenRunId}. Search again for the current symbol, or consult the rename candidates whose retired symbol is {id}. |
| `KindNotExamined` | '{given}' is a symbol kind orphans **does not examine**: {for project: project rows are roots, unreferenced by construction; for namespace: the reason the description states under `no namespace rows`}. Filter by an examined kind, or omit kind. |
| `NotAProjectRow` | Symbol {id} ('{name}', {kind}, {path}:{line}) is **not a project row**; projectSymbolId takes the id of a project-kind symbol — the ids the result's byProject lists. |
| `NotAType` | Symbol {id} ('{name}', {kind}) is **not a type**; type_usages takes a class, module, structure, interface, enum or delegate. **Use references** for a member. |
| `GateOff` | extract is refused: **{gate} is false** in '{configPath}'. The Architect flips it; nothing ran and the map is unchanged. |
| `TargetMissing` | Supply **exactly one of solutionKey, repoPath or stale**. Nothing ran. |
| `PathNotRegistered` | **No registered solution's repository root contains** '{path}'. Registered roots: {roots}. Nothing ran. |
| `AmbiguousRoot` | '{path}' lies under a root bound to **more than one** registered solution ({keys}); name the solutionKey. Nothing ran. |
| `KeyNotRegistered` | **No code_map_solutions row** has solution_key '{key}'. Keys are exact; solutions lists the map's, map_status the registry's. Nothing ran. |
| `KeyUnbound` | Registry row '{key}' has **no codemem_solution_id**: it has never been published. Run the extractor by hand once with --solution-key {key} and bind the row in MemOS; the bridge does not extract an unbound solution. |
| `KeyInactive` | Registry row '{key}' is **{state}**, not active. Nothing ran. |
| `MapMissingSolution` | Registry row '{key}' binds map solution {id}, but the map at '{mapPath}' **holds no solution {id}**. Nothing ran. |
| `ExtractionRunning` | **An extraction launched by this bridge is still running** ({key}); wait for its result. Nothing ran. |
| `ExtractorNotFound` | The extractor was **not found** at '{extractorPath}'. Set extractorPath in '{configPath}' or build CodeMem.sln. Nothing ran. |
| `AmbiguousTarget` | The build names **more than one target** ({candidates}); the hook resolves none of them and never falls back to the working directory. Nothing ran. |
| `ChildTimedOut` | The extractor for '{key}' **exceeded 540 s** and was stopped; the map holds whatever it published before and nothing after. Run it by hand to see why. |

The eleven kinds from `ScopeMissing` to `NotAProjectRow` are transcribed from 058 §5 and 060 §4 (analyze pass 2,
U1, ruled 2026-09-15): the bridge's vocabulary lives here, not by pointer into MemOS documents. Tool names are
unprefixed (`symbol_search`, `solutions`, `orphans`); the registry replaces the `projects` table in
`ScopeMissing`; `ProjectUnknown` is not ported (Q7). `{scope description}` is `projectId {n}, resolving to: {keys
or "no solution"}` or `solutionKey '{key}'`. `BridgeRefusal.For` owns these texts (T019); facts assert the bold
phrases (T017 (7)).

## 7. What these tools never do

No write to either file beyond the extract log; no connection held across calls; no cache; no `LIKE` in
identity queries; no doc-comment-id input; no retired symbol in any result; no `part_of` in references; no
guessing in `map_status`; no extraction without the registry's key.

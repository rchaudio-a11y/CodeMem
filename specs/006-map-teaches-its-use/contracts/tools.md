# Contract: the nine tools after 006

**Date**: 2026-09-29 | **Spec**: [../spec.md](../spec.md) | Supersedes 005's `contracts/tools.md` where the two differ. A
section not restated here is 005's, unchanged.

## 1. Parameters (`rename_candidates` added)

| Parameter | Type | `rename_candidates` |
|---|---|---|
| `solutionKey` | string | required, exact |
| `runId` | integer | optional |
| `retiredSymbolId` | integer | optional |

`projectId` is refused `ProjectIdRemoved` as for every tool (005 FR-406). Any other unknown argument is ignored.
Registered with the read-only annotation (`ReadOnly = True`, as every tool but `extract`).

## 2. Descriptions

**symbol_search** gains one sentence, placed after the kind list (it is the sentence about the `constructor` kind),
verbatim from 191497 #3: *"VB constructors are named New; to find a class's constructions, use type_usages on the
class."* The registered text becomes:

"Find symbols in the CodeMem code map by name and/or kind within one map solution (solutionKey — exact; solutions lists
the keys), optionally narrowed to one project row (projectSymbolId — the id of a project-kind row inside the solution;
the filter narrows the rows and never changes what the total counts). The name matches case-insensitively anywhere in
the symbol's name; kind is one of: namespace, class, module, structure, interface, enum, enum_member, delegate, method,
constructor, property, field, event, project. **VB constructors are named New; to find a class's constructions, use
type_usages on the class.** Returns active symbols only, **at most 200** declarations, always with the total matched
and whether the result was truncated — **narrow the filter rather than page**. A file linked into several projects
declares one symbol per project; such twins are presented **as one declaration compiled into N projects, carrying every
project's symbol id.** This bridge scopes by solutionKey only. Read-only."

Every 004/005 caveat phrase stays verbatim (B07, B09 (4)). The new sentence joins B09 (4)'s asserted set.

**rename_candidates** (new; R81. **Ruled at STOP 1, 2026-09-29, with two edits**: the "Without runId or
retiredSymbolId…" sentence added, and "the number of candidates returned must equal" for "the candidates must equal".
The bold phrases are asserted):

"Rename evidence in the CodeMem code map for one map solution (solutionKey — exact; solutions lists the keys). Each
candidate pairs a symbol retired in an extract run with a new symbol of the same kind and container whose body hash is
equal, and gives each side's id, doc-comment id, kind, name, container, project, path, line and current active state (a
retired symbol can later be reactivated, a new one retired since), with samePath, offsetDistance (null when the two are
in different files) and rank (1 is nearest). A candidate is **a proposal, never applied**: this tool accepts, rejects
and resolves nothing — confirm one in the code. runId narrows to one run. Without runId or retiredSymbolId, every run
of the solution is listed, newest first. **retiredSymbolId** narrows to the candidates
naming that retired symbol — the id a retired-symbol refusal from symbol_detail, references or type_usages names — and
lists only the runs that wrote one, plus **the run that retired it**. An empty answer is common: **a changed signature
or body is not a rename** (the body hash differs), so search for the current symbol by name. A file linked into several
projects gives one candidate per project; they are **never folded**. For each completed run returned whole, the
number of candidates returned must equal the run's recorded count, or the call is **refused by name**; the result says
per run whether that check ran. Returns every candidate, **uncapped**. Read-only; the map's extractor is its only writer."

It contains none of B09 (4)'s forbidden phrases (`projectId`, `registry`, `code_map_solutions`, `MemOS project`), no
word either SQL gate matches in any case, and no case-sensitive `memos`.

## 3. Result shape

`rename_candidates`: data-model §2–§4, verbatim. The `scope` envelope is 005's §3.2.

## 4. Order of refusal (`rename_candidates`)

Arguments (`ProjectIdRemoved` → `ScopeMissing`) → configuration (`Unconfigured`, `ConfigKeyRetired`) → map (`MapAbsent`
→ `NotAMap` → `VersionBelow` / `VersionAbove` → `Busy` / `Unopenable`) → question (`SolutionKeyUnknown` → `RunNotFound`
→ `RunOutOfScope` → `SymbolNotFound` → `SymbolOutOfScope` → `CandidateCountMismatch`). The other eight tools' order is
005's, unchanged.

## 5. Server instructions

`options.ServerInstructions = BridgeServerInstructions.Text` in `BridgeServer.RunAsync`, beside `ServerInfo`
(research R74, R75). The text is 191490 §1's block. Its SHA-256 (LF, no trailing LF) is the spec's Q3 table,
`d8a173d3…a010ca`. It is advertised as `instructions` in the `initialize` result. It contains none of the SQL gates'
words and no `memos`. The two `"…"` quotations are doubled in the VB literal only.

## 6. Refusal texts (the bold phrases are asserted; 005 §6 and 004 §6 for the kinds not listed)

| Kind | Text |
|---|---|
| `SymbolRetired` (revised) | Symbol {id} ('{name}', {kind}, {path}) in solution '{solutionKey}' is **retired**; it was last seen in extract run {lastSeenRunId}. Search again for the current symbol, or **call rename_candidates with retiredSymbolId {id}**. |
| `RunNotFound` | The CodeMem map at '{mapPath}' **holds no extract run with id** {runId}. Omit runId to list every run of the solution; solutions names each solution's latest run. |
| `RunOutOfScope` | Extract run {runId} **belongs to solution** '{runKey}', not to '{solutionKey}'. Ask with solutionKey {runKey}, or pass a run of '{solutionKey}'. |
| `CandidateCountMismatch` | Extract run {runId} of '{solutionKey}' **recorded {recorded} rename candidates but the map holds {found}**: the run's evidence does not reconcile, and no answer is built on it. The bridge does not repair the map; a new extraction writes a new run and leaves this one as it is. |

`BridgeRefusalKind` gains the three after `NotAType`, the question-stage kinds kept together: thirty-two kinds.

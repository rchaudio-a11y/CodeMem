# Research: CodeMem 006 — The Map Teaches Its Own Use

**Date**: 2026-09-29 | **Spec**: [spec.md](spec.md) (clarified: five rulings) | Numbering continues 005's R60–R73.

Every structure fact about CodeMem below was read from the map after it was refreshed on 2026-09-29 (`extract
solutionKey=CodeMem` → run 111 at `806b524`, 1,922 observed, balanced), or read from source where the question was
behaviour or text (v1.5.0's Map-first rule; spec FR-530). The tree was dirty only with this feature's untracked
markdown, which the map does not hold; the map is judged current for code.

## R74. Environment facts (Verified 2026-09-29)

- **The SDK carries server instructions.** `ModelContextProtocol.Core` 1.4.0, the package `CodeMem.Bridging` references,
  documents `P:ModelContextProtocol.Server.McpServerOptions.ServerInstructions` ("optional server instructions to send
  to clients"). They travel as `P:ModelContextProtocol.Protocol.InitializeResult.Instructions`, which is `instructions`
  on the wire. Setting `options.ServerInstructions` in `RunAsync` beside `ServerInfo` is the whole wiring. No package
  moves. (Read from `%USERPROFILE%\.nuget\packages\modelcontextprotocol.core\1.4.0\lib\net8.0\ModelContextProtocol.Core.xml`.)
- **Versions**: `CodeMem.Bridge.vbproj` and `CodeMem.Bridging.vbproj` both carry `<Version>0.2.0</Version>`. B01 (5)
  reads `serverInfo.version` "0.2.0" (the assembly version, three parts).
- **The test client**: `BridgeProcess.Initialize()` returns only `result.serverInfo`. The instructions are a sibling
  field of the same result, so the helper must hand back the whole result (R79).
- **The suite baseline**: re-measured at plan (FR-529); see plan §Technical Context for the figure and duration.
- **The map**: CodeMem run 111 (refreshed at plan). MemOS run 110 is behind by 1. The live figures the acceptance
  depends on (run 13's one candidate; 6754 last seen in 52, retired in 53) are the spec's §Measured, read through
  the MemOS-side reader and the bridge.

## R75. The instructions constant and its line endings (Decided — FR-511, Q12)

**Decision**: a module `BridgeServerInstructions` in `src/CodeMem.Bridging/Mcp/BridgeServerInstructions.vb`, beside
`BridgeToolDescriptions`, as 191490's Implementor note places it. It holds one `Public Const Text As String`, a VB
multi-line string literal of 191490 §1's 25 lines, the two `"…"` quotations doubled as VB requires. The file is pinned
`text eol=lf` in `.gitattributes`, one added line beside `SchemaRepository.vb`'s, so the literal's line breaks are LF
on every checkout. `RunAsync` sets `options.ServerInstructions = BridgeServerInstructions.Text`.

**Rationale**:
- One constant is one door (Article XII): the server, B01 (5) and the usage-texts facts all read it.
- The multi-line literal keeps the text diffable against 191490, line for line.
- The `eol=lf` pin is 005 T001's precedent for exactly this trap. `SchemaRepository.vb` went CRLF under
  `core.autocrlf=true` at the 004 merge.
- A fact asserts no CR in the advertised text (FIRE: concatenate one `vbCrLf` into the constant). That catches a
  checkout that ignores the pin as well as an edit.

**Alternatives**:
- Build the text from 25 separate literals joined with `vbLf`: CR-proof without the pin, but the source no longer
  reads as the ratified block, so the review diff is lost.
- Strip CR at the use site (`Replace(vbCr, "")`): the constant and the advertised text would differ, and a second
  rule would stand at a second place.

## R76. The reader: one query per concern, the sides through the existing symbol read (Decided — FR-501–FR-509)

**Decision**: `RenameCandidatesReader.Read(map, scope, config, runId, retiredSymbolId)` in
`src/CodeMem.Bridging/Reading/Readers/`, walked through `BridgeTools.RunRead` like every read tool. Its steps, in the
refusal order Q7 fixes:

1. **Scope.** `ScopeResolver.Resolve(key)` answers `SolutionKeyUnknown`.
2. **Run.** When `runId` is given, `ExtractRunsRepository.ReadById` answers `RunNotFound` if it returns nothing, and
   `RunOutOfScope` if the run's `SolutionId` is not the scope's. The latter names the run's own key, read with
   `SolutionsRepository.ReadById`.
3. **Symbol.** When `retiredSymbolId` is given, `SymbolResolver.RequireInScope` (new) answers `SymbolNotFound` or
   `SymbolOutOfScope`. It is the first two-thirds of today's `RequireActive`, which is re-expressed as
   `RequireInScope` followed by the active check, so the two refusals keep one door (Article XII).
4. **Candidates.** `RenameCandidatesRepository.ReadCandidates(map, solutionId, runId, retiredSymbolId)` (new): one
   SELECT with two nullable filters, ordered `run_id DESC, retired_symbol_id, rank, new_symbol_id` (Q5).
5. **Runs.** `ExtractRunsRepository.ReadBySolution(map, solutionId)` (new, `id DESC`) when unfiltered. The single
   run when `runId` is given. Under `retiredSymbolId` alone, the distinct `run_id`s of step 4 plus
   `ExtractRunsRepository.ReadRetiringRun(map, solutionId, lastSeenRunId)` (new) when the symbol is retired now
   (R77).
6. **Sides.** Each distinct symbol id of step 4 is read once with `CodeSymbolsRepository.ReadById` and memoised per
   call. `SymbolRecord` already carries every field a side needs: container and project names, path, line, active
   state, last-seen run.
7. **Count check.** For each completed run returned whole (no `retiredSymbolId`), the candidates returned must equal
   `RunRecord`'s recorded `rename_candidates`, or the call is refused `CandidateCountMismatch` (R78). Otherwise each
   run records `countChecked` and the reason when it was not.
8. **Envelope.** data-model §2.

**Rationale**:
- Every read is a named `Read…` method in Core (Article XI). `BridgeSqlGateTests` (3) proves each SELECT-only by its
  prefix, and the tripwire's absence scan is unchanged.
- The sides reuse the one symbol read the other tools use (Article XII), rather than a second join that restates
  `SymbolRecord`'s mapping.
- Candidates are rare (one in 110 live MemOS runs), so a per-id read is cheap. The memo keeps a solution's worth
  bounded by its distinct ids.
- `RunRecord` is reused for runs, as `SolutionsReader` does.

**The one new Core record**: `RenameCandidateRecord` (`src/CodeMem.Core/Records/`): id, solution id, run id, retired
id, new id, same path, offset distance (nullable), rank. `RenameCandidate` (the reconciler's proposal) carries the new
side's *doc-comment id*, because the new id is not minted when the reconciler runs, so it cannot describe a stored
row. The two are the pre-publication and post-publication views of one concept, as `RegistryRow` and `SymbolRecord`
already are for `code_symbols` (Article XI's "one canonical model" is per shape, and this precedent stands).

**Alternatives**:
- One SELECT joining both sides, their containers and projects: fewer round trips, but a second mapping of
  `code_symbols` beside `ReadById`'s.
- Reading every candidate and filtering in memory: rejected; the filters belong in the query, which also keeps the
  unfiltered call's cost proportional to the answer.

## R77. The retiring run (Decided — Q2, FR-504)

**Decision**: `ExtractRunsRepository.ReadRetiringRun(db, solutionId, lastSeenRunId)`:
`SELECT <run columns> FROM extract_runs WHERE solution_id = @solution_id AND outcome = 'completed' AND id >
@last_seen_run_id ORDER BY id LIMIT 1`. Called only when the named symbol is not active. `Nothing` means the solution
has no completed run after the symbol's last sighting, which cannot happen for a retired row, so it is reported as
the answer's absence (`retiredInRunId: null`), never guessed.

**Rationale**: the first completed run after the last sighting is the run that did not observe the symbol (Article VI
step 4 retires every unobserved active row). A failed run publishes nothing (Article V). This is the one door Q2
names. Measured: 6754 → last seen 52 → 53.

**Alternatives**: store the retiring run on `code_symbols` (a schema change, out of scope, Q11); infer it from the
candidate rows (they exist only when a candidate was written, and 6754 has none).

## R78. The count check (Decided — FR-505, Q5)

**Decision**: when `retiredSymbolId` is absent, every listed run with `outcome = 'completed'` is checked:
`candidatesReturned(run) = run.RenameCandidates`. On the first mismatch, in listed order (newest first), the call is
refused `CandidateCountMismatch`, naming that run and both numbers. A failed run is listed with
`countChecked: false, countNotCheckedReason: "failed run"`. Under `retiredSymbolId` every run carries
`countChecked: false, countNotCheckedReason: "filtered by retiredSymbolId"`.

**Rationale**: the refusal answers the question "is this run's evidence whole?" at the one moment the reader has
both numbers. A mismatch means the map disagrees with its own run row, which no answer built on it should hide
(Article VIII's spirit on the read side).

**Fixture**: the extractor's real rename (I06's `Describe` → `Explain`), then one candidate row removed from the temp
map by a test-side statement in `MapQueries` (the pattern `SetStartLine` and `SetRepoRoot` already follow). That is
the only way to make a completed run disagree with itself, and it is the FIRE the spec names.

## R79. The test surface (Decided)

- **`BridgeProcess.InitializeResult()`** (new) returns the whole `initialize` result. `Initialize()` keeps returning
  `serverInfo` for its existing callers.
- **`BridgeProcess.ListToolAnnotations()`** (new) returns each tool's `annotations` from `tools/list`, for the
  read-only fact. `RenameScenario` exposes its `ConfigPath` for `BridgeProcess.Serve` (analyze U1).
- **`B12_RenameCandidatesTests`** (new, Bridge): nine facts on a `RenameScenario`, covering US1 scenarios 3–7 and the
  both-filters shape of Q5 (analyze G4).
- **`RenameScenario`** (new, Support) is a class fixture:
  - a `FixtureCopy` prepared with `TwinScenario`'s linked `Shared/Twin.vb`, whose `Prepare` becomes `Friend Shared`
    (the second caller; no third, so no abstraction);
  - run 1;
  - the I06 rename (`Sub Describe(` → `Sub Explain(`) and the twin rename (`Public Function Name()` →
    `Public Function Label()` in `Shared/Twin.vb`);
  - run 2, which writes three candidates: `Describe`→`Explain`, and one per twin, each within its project's
    container, because the twins' namespaces differ, `Sample.App.Shared` and `Sample.Lib.Shared` (verified in
    `TwinScenario.Prepare`);
  - run 3: one more rename (`Sub Explain(` → `Sub Narrate(`), extracted with `RunSeams.CorruptStagedCounts`. That is a
    real failed run that *records* one candidate and publishes none (as I08, B02 and X02 produce failed runs). A
    failed run over an unchanged tree would record zero and prove nothing about the check. The scenario asserts run
    3's recorded count of 1 and its zero rows from the map itself (analyze U2).
  - a second, unchanged copy extracted once as `Other`. Its run is the foreign run `RunOutOfScope` names, and it is
    the solution with no candidates at all.
- **`UsageTextsGateTests`** (new, Guards), the v1.5.0 gate as facts:
  - (1) the advertised instructions equal the constant, with no CR;
  - (2) every registered name is a whole word in the instructions;
  - (3) the Q4 phrases, whitespace collapsed;
  - (4) the README's tool-table rows equal the registered names as a set (both directions);
  - (5) the skill and the snippet carry "The map proves structure; runtime behaviour needs its own proof.",
    whitespace collapsed;
  - (6) no file under `docs/` contains "Rick" or "153204";
  - (7) the README states no tool count but nine.
- **Amended in place, each Red named in advance** (plan §Test design):
  - B01 (5): the literal nine names, the instructions, 0.3.0.
  - B09 (4): 9 descriptions, and the `symbol_search` sentence among the caveat phrases.
  - B09 (6): 32 kinds.
  - B05 (18): the placeholder path, no `C:/Users/`.
  - B08: two armed facts, (9) and (10), for the acceptance on a copy.

The README table is read between the heading `## The tools your assistant gets` and the next blank line after the
table's last row. A row's tool is the text between ``**` `` and `` `**`` in its first cell. Word boundaries for tool
names are identifier boundaries (`[A-Za-z0-9_]` on neither side), so `solutions` does not match inside
`solutionKey`.

## R80. Refusal texts (Decided — Q7; contracts/tools.md §6)

The three new kinds and the revised `SymbolRetired` are drafted in contracts/tools.md §6. Each distinguishing phrase
is bold there and asserted. No text contains a word either SQL gate matches: `SELECT`, `INSERT`, `UPDATE`, `DELETE`,
`CREATE`, `DROP`, `ALTER`, `ATTACH` and `PRAGMA` in any case, including "create table". "repair", "holds", "belongs"
and "recorded" are the verbs used.

## R81. The tool description (Decided — FR-510; for STOP 1)

Drafted in contracts/tools.md §2. It names every FR-510 point. It contains none of B09 (4)'s forbidden phrases
(`projectId`, `registry`, `code_map_solutions`, `MemOS project`), no SQL gate word, and no case-sensitive `memos`
(the standalone gate). It is the one text in this feature that has no ratified source, which is why it is a STOP 1
decision. **Ruled 2026-09-29 with two edits**: the unfiltered sentence ("Without runId or retiredSymbolId, every run of
the solution is listed, newest first.") and "the number of candidates returned must equal". Neither adds a gate word.

## R82. The kit files and the hashes (Decided — Q3; FR-516, FR-517, FR-520)

The six texts are extracted from 191490 exactly as the spec's Q3 table was made: read-only from `document_sections`,
blocks by the Q3 rule, LF, no trailing LF. The implementation writes them from the store. The scratch extraction of
2026-09-29, `scratchpad/191490/*.txt` (session-local), is not the source; the store is. Then:

- `SKILL.md` and `CLAUDE.snippet.md` are the block plus one final LF.
- README step 6 is inserted after step 5's paragraph, with one blank line on each side.
- Step 5's sentence becomes the paragraph's last line.
- The table row's Answers cell is the row sentence.
- The instructions constant is the §1 block.

The quickstart (§Verbatim) hashes each shipped text back and compares it with the spec's table; any difference stops
the work (Q3). The docs files are not `.vb` files, so no header block applies to them, and nothing scans them but
`UsageTextsGateTests`.

## R83. Line endings of the kit files (Decided)

`docs/**/*.md` and `README.md` check out CRLF on this machine. The Q3 comparison normalises to LF, so a CRLF checkout
still hashes equal, and the phrase facts collapse whitespace, so CR is irrelevant to them. No `.gitattributes` line is
added for markdown: the ratified text is the characters, not the line terminator.

## R84. Acceptance on a copy of the live map (Decided — Q13, FR-527)

B08's armed facts gain (9) `rename_candidates MemOS runId 13`, (10) `retiredSymbolId 6754` and (11) MemOS unfiltered
(analyze G3), run with
`CODEMEM_LIVE_MAP` pointing at a copy of `C:\_DB\codemem.sqlite`. The copy is taken with `Copy-Item` while no
extraction holds the map: the green-build hook is live, so no `dotnet build` runs in a mapped repository during the
copy. The live map's SHA-256 is recorded before the copy and after the suite, and must be unchanged. The facts assert
the spec's figures:

- (9): one candidate, 5339 → 23219, `samePath` true, `offsetDistance` 284, rank 1, `countChecked` true.
- (10): no candidates, runs exactly `[53]`, `retiredInRunId` 53.
- (11): answered, every completed run `countChecked`, `total` equal to the sum of recorded counts over completed runs.
  The Architect checked it live on 2026-09-29 through the MemOS-side reader: every completed MemOS run reconciles, and
  the total is 1, run 13's.

No timing is asserted; each fact's duration is recorded at T031 (analyze G3, ruled).

If MemOS gains runs before the close, (9) and (10) still hold: they name fixed runs and a fixed symbol.

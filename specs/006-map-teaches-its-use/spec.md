# Feature Specification: CodeMem 006 — The Map Teaches Its Own Use: Server Instructions, the Usage Kit, and `rename_candidates`

**Feature Branch**: `006-map-teaches-its-use` (from `main` at `806b524`, the constitution v1.5.0 amendment)

**Created**: 2026-09-29

**Status**: Specified 2026-09-29. The description leaves thirteen decisions open or implicit; each is proposed below
(Clarifications, session at specify) with its reasoning and its alternative. The three the Architect named before
the draft came back are Q3 (verbatim), Q2 (the run list under `retiredSymbolId`) and Q1 (twins). **Clarified the
same day (five rulings, session below):**
- Q1 ruled A: bare ids, each side names its project.
- Q8 (b) ruled A: "Nine, over stdio."
- Q8 (c) and (f) ruled with 191490 revised by the Documenter: the step 5 sentence added, the pointer line ruled out,
  and the six ratified hashes taken from the store (Q3's table).
- FR-521 ruled B: both directions for the README table.
- Q8 (e) ruled A: "v1.5.0, fifteen articles".

Q2–Q7 and Q9–Q13 stood as proposed into the plan. **Planned and tasked 2026-09-29**:
- **STOP 1 ruled the same day** (plan §STOP 1): eight decisions as proposed; version pin (4) retired to the archive;
  the tool description approved with two edits; the process document's two exclusions overruled. That extends Q9.
- **Analyzed the same day**: fourteen findings, ruled and applied. FR-513 is "the fact, with one FIRE"; four FIREs
  were added; B08 (11) and B12 (9) were added; the rest is wording.
- **Implemented 2026-09-29** on `006-map-teaches-its-use` (plan.md "Implementation record"): T001–T033.
  - Debug and Release are both 224 / 212 / 0 / 12.
  - Six verbatim hashes OK.
  - On a copy of the live map, B08 (9)–(11) are green and the live map is unchanged.
  - Open for the Architect: the connection-site gate's qualified-name gap, and the merge.

**Input**: User description: "CodeMem 006: the map teaches its own use. PM: 153204. Rulings: 191497. Parent: 005.
### Remove Nothing. ### Change `BridgeServer.RunAsync` … also sets the server instructions to a constant holding doc
191490 §1's text, verbatim … `BridgeToolDescriptions.SymbolSearch` gains one sentence … `SymbolRetired` refusal text …
→ '… or call rename_candidates with retiredSymbolId N.' … `settings.fragment.json` … → C:/path/to/CodeMem.Bridge.exe …
README.md — tool count eight → nine … step 6 from doc 191490 §4 verbatim … ### Add Tool `rename_candidates` … `docs/
claude-code/skills/codemem/SKILL.md` — doc 191490 §3's fenced text, verbatim … `docs/claude-code/CLAUDE.snippet.md`
— doc 191490 §2's public snippet only, verbatim … ### Invariants as tests …"

**Governing document**: `.specify/memory/constitution.md` **v1.5.0** (amended 2026-09-29, `806b524`). This is the
first feature reviewed against v1.5.0's two additions: the **Map first** Cross-Cutting Hard Rule, which governs how
this feature's own structure questions about CodeMem are answered, and the **usage-texts Review Gate**, which this
feature is the first to trigger. It adds a tool, so the server instructions, every registered tool description,
`docs/claude-code/` and the README's tool list and usage section change in this feature, and a test pins the phrases
they depend on. Where this specification and the constitution appear to differ, the constitution wins and the
difference is a defect in this document.

**Parent features**: `specs/005-bridge-stands-alone/` (shipped, merged to `main` 2026-09-17 by fast-forward to
`15a2d9b`), on `specs/001`–`004`. Requirements here are numbered FR-501 onward so they never collide with FR-001–FR-039,
FR-101–FR-123, FR-201–FR-219, FR-301–FR-351 or FR-401–FR-437. This feature changes the bridge (its tool set, its
advertised text and three of its texts) and adds three reads and one record to Core (Q11). It changes neither the
extractor nor the schema.

**PM**: task 153204 (RULED 2026-09-16: its own fixpack, landing after 005. The Implementor reaches for the map before
the filesystem). Decision **191497** (the Architect, 2026-09-29) makes eight rulings. The ones this feature carries
out are #1 (three surfaces plus the README), #2 (placement under `docs/claude-code/`), #3 (the `symbol_search`
clause), #4 (proof rule, revision 3), #7 (the bridge gains `rename_candidates` in the same feature) and #8 (the kit
before the 2026-09-17 review findings). #6, the constitution at v1.5.0, has landed. Doc **191490** (RATIFIED
2026-09-29 by 191497) is the source text of the four surfaces, §1–§4. All three were read from the PM store on
2026-09-29 and are quoted where they bind. 191497's open item on registration scope (task 152685) is not this
feature's.

**Ceremony**: STANDARD, no spike. What the design rests on is measured below (§Measured): the one candidate the live
map holds, the retiring run of symbol 6754, the gates the new texts must pass, and the line-ending trap a verbatim
constant meets on this machine. The one fact still to confirm is the SDK property that carries server instructions at
the package version the bridge references. The plan confirms it, and `initialize` is the test.

## Mission

An assistant connected to the bridge learns, from the bridge itself, when to ask the map before searching files and
how to prove a claim with it. Someone who downloads CodeMem gets the same guidance as three files they copy by hand.
And the bridge answers the question its own `SymbolRetired` refusal has pointed at since 004: *what became of this
retired id?*

**Out**:
- Accepting, rejecting or resolving a rename candidate. That is a human decision made outside the map.
- Any write to the map, any schema change, any change to the extractor.
- The MemOS repository, its constitution v6.2.0 (191497 #5) and its `codemem_*` tools.
- Placing the snippet or the skill into `~/.claude`. A person copies them; nothing installs itself.
- The private `CLAUDE.md` lines of 191490 §2.
- The registration-scope check (191497 §Open, task 152685) and the F10 caveat (punch 176110).
- The 2026-09-17 review findings, tasks 167677–167686. They follow this feature (191497 #8).
- Mapping C#.

## What this feature changes and adds (the description's headings, as a table)

| | Today (005, `806b524`) | After 006 |
|---|---|---|
| Server instructions | none; `RunAsync` sets `ServerInfo` only | **191490 §1, verbatim**, advertised at `initialize` |
| Tools | eight | **nine**: `rename_candidates` added, read-only |
| `symbol_search` description | no constructor route | gains *"VB constructors are named New; to find a class's constructions, use type_usages on the class."* |
| `SymbolRetired` refusal | "… or consult the rename candidates whose retired symbol is N." | "… or **call rename_candidates with retiredSymbolId N**." |
| Refusal vocabulary | 29 kinds | **32**: `RunNotFound`, `RunOutOfScope` and `CandidateCountMismatch` added (Q7) |
| `docs/claude-code/` | absent | `skills/codemem/SKILL.md` (§3) and `CLAUDE.snippet.md` (§2's public block), verbatim |
| README | "8"/"eight" tools in four places; no step 6; constitution "v1.4.0" | nine in all four; a `rename_candidates` row; step 5's closing sentence and Quick start step 6 (§4), verbatim; "v1.5.0" |
| `hooks/settings.fragment.json` | `C:/Users/rchau/…/CodeMem.Bridge.exe` | `C:/path/to/CodeMem.Bridge.exe` |
| Process document | eight tools, 29 kinds, "never returns a retired symbol" | nine, 32, that sentence amended (Q9) |
| Bridge version | 0.2.0 | 0.3.0 (Q10) |
| Extractor, schema | extractor 0.3.0, schema 3 | **unchanged** (Q11) |

## Clarifications

### Session 2026-09-29 (at specify; before clarify and plan)

The description settles the surfaces and the tool's contract. The decisions below are the ones it leaves open. Each
is answered with its reasoning and its alternative so nothing is decided silently, and every one is a proposal until
the Architect rules.

- **Q1 (review point 3): Twins.** When a candidate's side is a linked-file twin, does it fold as `symbol_search` does,
  or carry the bare id? → A: **Bare ids. One candidate per `rename_candidates` row, never folded. Each side also names
  its project (id and name).** *Ruled A at clarify, 2026-09-29.* Reasons:
  - (a) The extractor already proposes per twin. Its rule (B) matches a new symbol only against rows retired this run
    that have the same *resolved container id* (`Reconciler.vb`, step 5). A linked file's twins sit in different
    projects under different containers: their doc-comment ids differ by the project's root namespace, and equal ids
    would have stopped the run at the duplicate guard. So renaming a member of a file linked into two projects writes
    two rows, each pairing one project's retired id with the same project's new id, and never a cross-project pair.
  - (b) The count check reconciles rows against the run's recorded count. A folded answer would either fail the check
    or need a second counting rule.
  - (c) The question is asked about one id. `SymbolRetired` names one id, `retiredSymbolId` filters on one id, and an
    acceptance, when a person makes one, acts on one pair. A fold would answer for ids nobody asked about.
  - (d) `symbol_search` folds because its question is "where is this declared?", which has one answer per
    declaration. This tool's question is "which id became which?".

  The project on each side is the one field added to the description's list. Without it, twin candidates read as
  duplicates: same name, same path, same line, different ids. Alternatives, not taken: fold twin candidates into one
  entry carrying every pair (rejected for (b) and (c)); bare ids without the project (rejected: the rows cannot be
  told apart).
- **Q2 (review point 2): The runs under `retiredSymbolId`.** → A: **The runs that wrote a candidate naming that id on
  its retired side, plus, when the symbol is retired now, the run that retired it. Newest first, no duplicates, never
  every run of the solution.** The map records no retiring run on the symbol row: `code_symbols` carries only
  `first_seen_run_id` and `last_seen_run_id`, and this feature changes no schema. The retiring run is therefore
  derived, and this is the one place that says how. It is **the first run of the same solution, in id order, with
  outcome `completed`, after the symbol's `last_seen_run_id`**. A completed run that does not observe an active symbol
  retires it, and a failed run publishes nothing, so that run is the one. For a symbol retired, reactivated and
  retired again, the derivation names the latest retirement, and the candidate-writing runs of both retirements are
  listed. For a symbol active now, no retiring run is derivable: the runs are the candidate-writing runs only (possibly
  none), and the result says the symbol is active. The filter is on the retired side only; a symbol named only as the
  new side of a candidate returns no candidates. Measured: 6754 was last seen in run 52, and MemOS's next completed
  run is 53, so the answer is exactly one run, 53. The MemOS-side reader answers the same question with 97 run rows
  (§Measured), which is the shape this ruling excludes.
- **Q3 (review point 1): Verbatim.** What makes a shipped text "verbatim" against doc 191490? → A: **With line endings
  normalised to LF, its characters equal the fenced block of 191490 as ratified, and nothing else. The block is the
  lines strictly between the opening fence line and its closing fence line. A file adds one final LF.** The four
  blocks:
  - §1's `text` block → the instructions constant.
  - §2's **first** `markdown` block → `docs/claude-code/CLAUDE.snippet.md`. §2's second block, the private lines,
    never enters the repository.
  - §3's `markdown` block, front matter included → `docs/claude-code/skills/codemem/SKILL.md`.
  - §4's `markdown` block → README Quick start step 6, placed after step 5. **§4's block contains an inner
    ```` ```powershell ```` fence.** Its first bare fence line closes that inner fence, not the outer block, so the
    outer block ends at §4's *last* fence line. A Markdown renderer closes it early; the store's text is the source,
    not a rendering of it.

  Two single-line texts in §4 are verbatim too:
  - README step 5's closing line: §4's one quoted line with its `> ` marker stripped, appended as the last line of
    step 5. *"Replace `C:/path/to/CodeMem.Bridge.exe` in the fragment with the path your build produced."* (Q8 (f)).
  - The tool-table row for `rename_candidates`: *"Was this renamed? What became of a retired id? Ranked proposals;
    never applied."*

  **The ratified hashes, taken from the store at clarify** (at the Architect's direction, 2026-09-29). They were read
  read-only from 191490's `document_sections`, at the revision with `updated_at` 2026-09-29T18:58:53.676Z and
  `content_hash` `fc0ac8d037ea4802f19295c2e94d51b56eefca414a45007d1121ab5a59427837`. Each hash is the SHA-256 of the
  UTF-8 text, LF-joined, with no trailing LF:

  | Shipped text | Source in 191490 | Lines | SHA-256 |
  |---|---|---|---|
  | The instructions constant | §1 `text` block | 25 | `d8a173d3ca85e6449f474053cfa9de12a05cb52858ab4671782dcd7b99a010ca` |
  | `docs/claude-code/CLAUDE.snippet.md` | §2 first `markdown` block | 13 | `0cb688263573f0387bec07ddc24fe612da882b505e171930444679728338b970` |
  | `docs/claude-code/skills/codemem/SKILL.md` | §3 `markdown` block | 70 | `509557babd307faa9373844062e6565edcaec7199690dde9b8c3a66cbbf5dd26` |
  | README Quick start step 6 | §4 `markdown` block, to §4's last fence | 16 | `f73f70295130bb825fbe07712ce91cface5561a20b31563d1a137a5eb1d58f82` |
  | README step 5's closing line | §4 quoted line, `> ` stripped | 1 | `1d6946b3842a5fb4281278cc84af0f929645e7e03613db9f28031ac250814bf6` |
  | README `rename_candidates` row text | §4 "Also" paragraph | 1 | `3ec2c4deae08e1ec67c5e57f35f31b351f61ff89aab7c95c42682c1c34a4e174` |

  At the close, the quickstart records each shipped text's SHA-256 under the same rule (LF line endings, a file's
  final LF removed), equal to this table. If the store's text changes after these hashes, work stops for a ruling:
  the ratified text is the one hashed here, not the store's latest. No text is "tidied", re-wrapped or
  re-punctuated. Alternative, not taken: pin each shipped text by a SHA-256 in the suite. This was rejected because
  the gate asks for phrase pins that fire on a missing tool; a hash pin fires on every sanctioned edit and says
  nothing about what broke. The hashes live in this specification and the quickstart instead.
- **Q4: The "Not a replacement for reading the source" pin in the instructions.** The description pins *"The map
  proves structure"* in the instructions, the skill and the snippet, "or the short form's equivalent". §1 is the short
  form and does not contain that sentence. → A: **Instructions: the phrases "Use text search for what the map does not
  hold" and "runtime behaviour". Skill and snippet: the full sentence "The map proves structure; runtime behaviour
  needs its own proof."** 191497 #4 allows the instructions "a two-line short form of the same meaning". §1's last
  paragraph is where that meaning lives: runtime behaviour is named among what the map does not hold. Beside these,
  the instructions are pinned by the other phrases 191490's Implementor note names: "VB constructors are all named
  New" (the constructor line) and "Prove, don't grep" (the proof line), plus every registered tool name (FR-512).
  **Every phrase pin compares after collapsing each run of whitespace to one space**, which is TripwireTests'
  precedent (FR-121). The snippet breaks the sentence across a line with indentation ("… runtime behaviour needs its⏎
  own proof."), so a literal pin would never match the verbatim text. Alternative: pin only the tool names
  (rejected: the description asks for the meaning to be pinned).
- **Q5: Which runs and candidates each filter returns, and when the count check runs.** → A: The four argument shapes:

  | Arguments | Runs listed | Candidates | Count check |
  |---|---|---|---|
  | neither filter | every run of the solution | every candidate of the solution | on each completed run |
  | `runId` | that run | its candidates | if the run is completed |
  | `retiredSymbolId` | as Q2 | those naming it on the retired side | on no run: none is returned whole |
  | both | that run only | its candidates naming the symbol | on no run |

  Ordering: runs by id, descending. Candidates by run id descending, then retired id, then rank, then new id. A failed
  run is listed with its recorded counts and never checked: it published no candidate rows, while its recorded count
  may be non-zero, which is exactly why only completed runs are checked. The result states the filters as given,
  whether each listed run was checked and, when not, why ("failed run" or "filtered by retiredSymbolId"), and the
  total number of candidates returned. The unfiltered call on MemOS lists every run (97 today). That is uncapped by
  the description, and it is the caller's choice.
- **Q6: A `retiredSymbolId` naming a symbol that is active now.** → A: **Answered, not refused.** A retired symbol can
  later be reactivated, and its old candidates stay true evidence. The result names the symbol (id, doc-comment id,
  kind, name, container, project, path, line, active state, last-seen run), its candidates if any, and the runs of Q2.
  `SymbolRetired` is never raised by this tool, because a retired id is its input. Alternative: refuse an active id
  (rejected: it would hide the candidates of a symbol retired once and reactivated since).
- **Q7: The refusal vocabulary.** → A: **Three kinds added, and the existing ones reused where they fit.**
  - **Added:** `RunNotFound` (no run with that id; names the id). `RunOutOfScope` (the run belongs to another
    solution; names the run, the requested key and the run's own key). `CandidateCountMismatch` (a completed run
    returned whole, with a candidate count different from the one it recorded; names the run and both counts, and says
    the run's evidence does not reconcile and the bridge does not repair the map).
  - **Reused:** `ScopeMissing`, `ProjectIdRemoved`, the configuration and map kinds, `SolutionKeyUnknown`,
    `SymbolNotFound` (a `retiredSymbolId` the map does not hold) and `SymbolOutOfScope` (one in another solution).
  - **Order:** arguments → configuration → map → solution → run → symbol → count. Twenty-nine kinds become thirty-two.

  Each new kind has its text, a fact on its distinguishing phrase, a production-route test (Article XIII) and a
  FIRE line (Article II). Every new text and the tool description avoid the words the SQL gates match (§Measured).
  Alternative: fold the two run refusals into one "run not in scope" (rejected: the remedies differ; one run does not
  exist, the other exists under another key).
- **Q8: README items the description did not list.** → A, item by item:
  - **(a) Four tool counts, not three.** The badge (line 13), the sketch's "8 MCP tools" (32), "Eight, over stdio"
    (131), and the fourth, the Language-support sentence "the bridge and its eight tools" (248). All four become nine.
    "The eight edge rules" (250) and the eight verbs are not tool counts and do not change.
  - **(b) "Eight, over stdio, every one scoped to a `solutionKey`."** Under the v1.5.0 gate the tool list must agree
    with tool behaviour. `solutions` and `map_status` take no `solutionKey`, and `extract` takes one of three targets.
    **"Nine, over stdio."** *Ruled A at clarify, 2026-09-29.* The clause is dropped rather than rewritten, because no
    source text exists for a new clause.
  - **(c) The pointer line.** **Ruled out, not missing.** 191490 §4, revised at clarify, drops the "one-line pointer
    from *The tools your assistant gets*": step 6 sits directly below that section, so a pointer adds a line to
    maintain and finds nothing. *Ruled at clarify, 2026-09-29.*
  - **(d) Suite counts.** The badge, the Layout table and the Status line move with the suite at the close (191490
    §4), and the Status line's "Features 001–005 merged" moves at the merge.
  - **(e) The constitution version.** "v1.4.0, fifteen articles" (229) has been stale since `806b524`. It becomes
    **"v1.5.0, fifteen articles"** in the same edit. "Fifteen articles" stays: v1.5.0 added a hard rule and a review
    gate, not an article. *Ruled A at clarify, 2026-09-29.*
  - **(f) Step 5 has no path.** The absolute path is in the fragment alone (FR-522). Once the fragment carries the
    placeholder, a reader merging it must substitute their own path. **Step 5 therefore gains one sentence as its last
    line**, 191490 §4's quoted line with `> ` stripped: *"Replace `C:/path/to/CodeMem.Bridge.exe` in the fragment
    with the path your build produced."* The reason: the hook exits 0 on every outcome, so a fragment merged with the
    placeholder unchanged gives a hook that silently never fires. Step 5's other text is unchanged. *Ruled at
    clarify, 2026-09-29; 191490 revised by the Documenter.*
- **Q9: The process document** (`src/CodeMem.Bridge/README.md`, Implementor-authored, not verbatim-sourced). → A:
  - The tool list names nine.
  - "When each tool is called" gains a `rename_candidates` entry.
  - The refusal table gains the three kinds, its count moves to thirty-two, and `SymbolRetired`'s remedy becomes the
    new sentence.
  - The line "never returns a retired symbol" (line 216) becomes true of this feature: **"never returns a retired
    symbol as if it were live: `rename_candidates` names retired symbols as the retired side of a proposal, each side
    with its active state."** As written today, it would be false the moment the tool ships, which is a v1.5.0 gate
    finding.

  *Extended at STOP 1, 2026-09-29 (plan decision 11, overruled):* the sentence "Every tool is scoped by
  `solutionKey`…" is deleted (the Q8 (b) ruling), and install step 4's machine paths become
  `C:/path/to/CodeMem.Bridge.exe` (the fragment's fix). Deletions and a substitution only.
- **Q10: Pins that move, and the version.** → A:
  - **B01 (5)** asserts the listed names against a **literal list of the nine**. Today it compares against
    `RegisteredToolNames` itself, which cannot fire for a tool that was never registered. It also asserts that the
    `initialize` result carries the instructions equal to the constant, and that the version is **0.3.0**. A new
    tool is a new capability, as 0.1.0 → 0.2.0 was for 005.
  - **B09 (4)'s** literal count moves from 8 to 9, and the new description passes its forbidden-phrase scan.
  - **B05 (18)** additionally asserts that the fragment's command path is the placeholder, going red first against
    today's fragment.

  Each moved pin records its Red.
- **Q11: "Extractor and schema byte-identical."** → A: **No file under `src/CodeMem.Extraction/` or
  `src/CodeMem.Extractor/` changes, `SchemaRepository.vb` is unchanged, the extractor version stays 0.3.0 and the
  schema version stays 3.** The reads the tool needs are added to `CodeMem.Core`'s repositories as named `Read…`
  methods (Article XI; `BridgeSqlGateTests` (3) proves them SELECT-only). So Core's assembly changes, and the
  extractor's behaviour does not: the fixture's canonical fact set (I02) is unchanged. The close-out's `git diff`
  names no file in those three places.
- **Q12: The instructions' line endings.** → A: **The advertised text contains LF only, whatever the checkout.** This
  machine runs `core.autocrlf=true`. A multi-line VB string literal takes the checkout's line endings: 005 met this in
  `SchemaRepository.vb` (S02 (2) went red at the 004 merge) and pinned that file `eol=lf` in `.gitattributes`. The plan
  chooses between the same pin for the file carrying the constant and building the text with explicit LF. A fact
  asserts no CR in the advertised text.
- **Q13: Where acceptance runs.** → A: **On a copy of the live map, never on the live map.** The suite never opens
  the live map. The two live acceptance lines (FR-527) run against a copy taken at the close, with the live map's
  SHA-256 recorded before and after.

### Session 2026-09-29 — clarify (Architect)

- Q: When a rename in a file shared between two projects produces two candidates, should each side of a candidate
  also name its project? → A: **Yes: bare ids, one candidate per row, never folded, and each side names its project
  (id and name) (Q1 as proposed).**
- Q: What should the README's tools heading say, given that "every one scoped to a `solutionKey`" is untrue for
  `solutions`, `map_status` and `extract`? → A: **"Nine, over stdio." The clause is dropped (Q8 (b) as proposed).**
- Q: Two pieces of public README text have no ratified source: the pointer line to step 6, and a step 5 sentence
  telling the reader to replace the placeholder path. What happens to them? → A: **191490 is revised. §4 now carries
  the step 5 sentence as a quoted line after the step 6 block; strip the `> ` marker and append it as the last line
  of step 5. There is no pointer line: it is ruled out, not missing. §4 still has one fenced block, so "ends at §4's
  last fence" holds. The hashes are taken from the store now** (Q3's table; Q8 (c) and (f)).
- Q: Should the README-table test fail only when a registered tool has no row, or also when a row names a tool that
  doesn't exist? → A: **Both directions for the README table: its rows are exactly the registered tools. Not a
  reverse scan of the instructions: a snake_case scan over prose would misfire on words like `not_in_map`, so the
  instructions stay covered by the review gate** (FR-521).
- Q: Should 006's README edit also change "v1.4.0, fifteen articles" to v1.5.0, a line outside the description's list
  that has been stale since `806b524`? → A: **Yes: "v1.5.0" in the same edit. "Fifteen articles" stays, because no
  article was added** (Q8 (e)).

## User Scenarios & Testing *(mandatory)*

Actors:
- **Claude Code**, connected to the bridge, which reads the instructions and calls the tools.
- **A person who downloads CodeMem**, who reads the README and copies the kit by hand.
- **The Operator** (the Architect at this desk), who runs the acceptance on a copy of the live map.
- **The Architect**, who rules and reads the evidence.

MemOS is not an actor; its store is not a file the bridge knows.

### User Story 1 - The bridge answers "what became of this id?" (Priority: P1)

Claude Code calls `symbol_detail` with an id carried in last week's notes and is refused `SymbolRetired`. The text now
names the tool and the argument: *call rename_candidates with retiredSymbolId N*. It does. The answer names the
symbol, says it is retired, names the run that retired it, and lists the ranked proposals, often none. For a run
asked whole, it says the candidates reconcile with the run's recorded count. Nothing is resolved and nothing is
written.

**Why this priority**: 191497 #7, "drop it in right away". The README has promised "was this renamed?" since the
landing page, `SymbolRetired` has told callers to consult the rename candidates since 004, and until now only the
MemOS side could answer. The instructions (US2) name this tool, so the two ship together.

**Independent Test**: Against a fixture map carrying a rename, including one in a file linked into two projects; the
count-mismatch case on a fixture copy with one candidate row removed; the two acceptance lines on a copy of the live
map.

**Acceptance Scenarios**:

1. **Given** a copy of the live map, **When** `rename_candidates(solutionKey "MemOS", runId 13)` is called, **Then** it
   returns exactly one candidate:
   - retired side: 5339 `SupportedSchemaVersion`, field, not active;
   - new side: 23219 `MinimumSupportedSchemaVersion`, active;
   - `samePath` true, `offsetDistance` 284, rank 1.

   Run 13 is listed as completed with 9 symbols retired and 1 candidate recorded, and the result says the count check
   ran.
2. **Given** the same copy, **When** `rename_candidates(solutionKey "MemOS", retiredSymbolId 6754)` is called, **Then**
   it returns no candidates and says so. It names symbol 6754 (`New`, constructor, `PmWriteManager.vb`) as retired,
   last seen in run 52 and retired in run 53. It lists exactly one run, 53, not every run of the solution, and says
   the count check did not run because the result is filtered.
3. **Given** a retired symbol, **When** `symbol_detail` or `references` is called with its id, **Then** the refusal
   text contains "call rename_candidates with retiredSymbolId" followed by that id. **When** that id is passed to
   `rename_candidates` with the same key, **Then** it is answered, not refused.
4. **Given** a fixture copy from which one candidate row of a completed run has been removed, **When**
   `rename_candidates(runId N)` is called, **Then** it is refused `CandidateCountMismatch`, naming the run, the
   recorded count and the count found.
5. **Given** a run id the map does not hold, a run id of another solution, a `retiredSymbolId` the map does not hold,
   and one of another solution, **When** each is called, **Then** each is refused by name: `RunNotFound`,
   `RunOutOfScope` (both keys named), `SymbolNotFound`, `SymbolOutOfScope`. Nothing else is opened after the refusal.
6. **Given** the fixture map's SHA-256, **When** any `rename_candidates` call returns, **Then** the hash is unchanged,
   and `tools/list` shows `rename_candidates` annotated read-only.
7. **Given** a fixture rename of a member declared in a file linked into two projects, **When** the run is asked
   whole, **Then** two candidates are returned, one per twin. Each pairs one project's ids, each side names its
   project, and the count check passes (Q1).

---

### User Story 2 - A connected assistant is told how to use the map (Priority: P1)

Claude Code connects to the bridge in any repository. The `initialize` answer carries the server's instructions,
191490 §1 word for word:
- ask the map before searching files;
- call `map_status` first;
- which tool answers which question, with `rename_candidates` among them;
- prove a count or a "nothing" with `references` or `type_usages`, naming the tool and the count;
- use text search for what the map does not hold.

`symbol_search`'s description adds the constructor route, so a search for a class's constructions no longer goes
looking for a constructor named after the class.

**Why this priority**: 191497 #1. The instructions are the one layer that loads whenever the bridge is connected, and
the 062 slip was a claim being proved where a skill would not have fired.

**Independent Test**: The executable over stdio: `initialize`, `tools/list`, the constant compared with the
advertised text, and the phrase pins with their fires.

**Acceptance Scenarios**:

1. **Given** the bridge started with `serve`, **When** a client sends `initialize`, **Then** the result carries
   instructions equal to the constant, containing no CR, and the constant is 191490 §1 verbatim (Q3; the quickstart
   records its SHA-256 equal to the ratified one).
2. **Given** the registered tool names, **When** the instructions are scanned, **Then** each of the nine appears as a
   whole word. Deleting the `rename_candidates` line from the constant turns the fact red.
3. **Given** the instructions, **When** the phrase pins run, **Then** "VB constructors are all named New", "Prove,
   don't grep", "Use text search for what the map does not hold" and "runtime behaviour" are each present.
4. **Given** `symbol_search`'s registered description, **When** read, **Then** it contains *"VB constructors are named
   New; to find a class's constructions, use type_usages on the class."* verbatim, and every existing caveat phrase
   (B07, B09 (4)) is unchanged.
5. **Given** `RunAsync` without the instructions assignment, **When** B01 (5) runs, **Then** it is red (the FIRE).

---

### User Story 3 - Someone who downloads CodeMem gets the same kit (Priority: P2)

A person clones CodeMem and reads the README. It counts nine tools, lists `rename_candidates`, and its Quick start
step 6 explains the three layers and gives two commands that copy the skill and append the snippet. The files the
commands name exist under `docs/claude-code/`, word for word as ratified. The hook fragment no longer names a path
on the author's machine, and step 5 tells the reader to replace the placeholder with their own. Nothing private is in
the repository.

**Why this priority**: 191497 #1 and #2 ("it needs to be added to the readme fill for people that download the code").
It follows US1 and US2 because the README describes what they ship.

**Independent Test**: File facts over `docs/`, `README.md` and the fragment. The quickstart's six hashes against Q3's
table.

**Acceptance Scenarios**:

1. **Given** the repository, **When** each shipped text is hashed under Q3's rule, **Then** each equals its ratified
   hash:
   - `docs/claude-code/skills/codemem/SKILL.md` equals §3;
   - `docs/claude-code/CLAUDE.snippet.md` equals §2's public block;
   - README step 6 equals §4's block;
   - README step 5's last line equals §4's quoted sentence;
   - the `rename_candidates` row carries §4's row text.
2. **Given** the skill and the snippet, **When** the phrase pins run, **Then** each contains "The map proves
   structure; runtime behaviour needs its own proof."
3. **Given** every file under `docs/`, **When** scanned, **Then** none contains "Rick" or "153204". Pasting §2's
   private block into the snippet turns the fact red.
4. **Given** the README's tool table, **When** compared with the registered names, **Then** its rows are exactly the
   registered tools, and the `rename_candidates` row carries 191490's sentence. Deleting that row turns the fact red,
   and so does adding a row for an unregistered name.
5. **Given** the README, **When** read, **Then** the badge, the sketch, the tools heading and the Language-support
   sentence all say nine (Q8 (a)).
6. **Given** `src/CodeMem.Bridge/hooks/settings.fragment.json`, **When** parsed, **Then** the hook command is
   `"C:/path/to/CodeMem.Bridge.exe" hook` and contains no `C:/Users/`.

---

### Edge Cases

- **A failed run with a non-zero recorded candidate count**: listed, never checked, with zero candidates returned. It
  published no rows, which is why only completed runs are checked (Q5).
- **`retiredSymbolId` names a symbol active now**: answered with its candidates (if any), the runs that wrote them, no
  retiring run, and the statement that it is active (Q6).
- **A symbol retired, reactivated and retired again**: the candidate-writing runs of both retirements are listed, and
  the retiring run named is the latest (Q2).
- **A candidate whose new side was retired later**: both sides are shown not active. The tool does not follow the
  chain to a third symbol; it answers for the id asked.
- **Two candidates for one retired id in one run** (a tie under (B)): both are returned, ranked by the extractor's
  proximity evidence. The tool neither re-ranks nor picks one.
- **A twin pair where only one twin produced a candidate** (the projects compile the file differently): one row. The
  other twin gets none; nothing is inferred for it (Q1).
- **Both filters given, and the run wrote nothing naming the symbol**: that run listed, no candidates, not checked, and
  the result says none was found (Q5).
- **The solution has no candidates at all**: every run listed, every completed run checked (0 = 0), total 0, and the
  result says so. "Empty is an answer."
- **A signature or body change**: leaves no candidate by design, because the body hash differs. The description, the
  skill and the result's empty-case sentence say so (FR-506). 6754 is the live instance.
- **The map is busy under an extraction**: `Busy`, as for every read tool.
- **The instructions constant on a CRLF checkout**: the advertised text is still LF (Q12).
- **A future tool added without updating the instructions or the README table, or removed while its README row
  stays**: the v1.5.0 gate facts go red (FR-512, FR-521). That is the gate working, not a flaky test. A removed tool
  still named in the instructions is caught by the review gate, not a test (FR-521).
- **The store's 191490 text edited after the clarify hashes**: work stops for a ruling; the shipped text follows the
  ratified hash (Q3).

## Requirements *(mandatory)*

### Functional Requirements

**The `rename_candidates` tool (191497 #7)**

- **FR-501**: The bridge MUST register a ninth tool, `rename_candidates`, with the read-only annotation. It takes
  `solutionKey` (required, exact), `runId` (optional) and `retiredSymbolId` (optional). A call carrying `projectId` is
  refused as for every tool (005 FR-406). Refusals follow the order of Q7.
- **FR-502**: Each candidate MUST carry its run id, the retired side and the new side, and three proximity fields.
  - Each side: id, doc-comment id, kind, name, container (id and name), **project (id and name)** (Q1), path, line and
    current active state.
  - `samePath`; `offsetDistance`, null when the two sides are in different files; and `rank`, where 1 is nearest.
  - Candidates are never folded; one row is one candidate (Q1).
- **FR-503**: The result MUST list the runs examined, newest first. Each run carries `runId`, `outcome`,
  `finishedUtc`, the recorded `symbolsRetired` and `renameCandidatesRecorded`, `candidatesReturned`, and
  `countChecked` with `countNotCheckedReason` when it did not run (data-model §4). Which runs are listed follows Q5.
- **FR-504**: With `retiredSymbolId`, the result MUST name that symbol: id, doc-comment id, kind, name, container,
  project, path, line, active state, last-seen run, and the run that retired it when it is retired now. The runs
  listed MUST be exactly those of Q2: the candidate-writing runs plus the derived retiring run, never every run of
  the solution. The derivation of Q2 is stated once, in one place in the code, and asserted by a fact.
- **FR-505**: For each completed run returned whole, the candidates returned MUST equal the run's recorded
  `rename_candidates`, or the call MUST be refused `CandidateCountMismatch`, naming the run and both counts. The
  result states per run whether the check ran (Q5).
- **FR-506**: Zero candidates MUST be an answer, not a refusal. The result lists the runs examined and says no
  candidate was found. It also says that a changed signature or body leaves no candidate, by design (the body hash
  differs).
- **FR-507**: The tool MUST return every matching candidate, uncapped, and MUST resolve nothing: it accepts, rejects or
  applies no candidate and writes nothing. The map's SHA-256 is unchanged after any call, asserted on a fixture copy.
- **FR-508**: The vocabulary MUST gain `RunNotFound`, `RunOutOfScope` and `CandidateCountMismatch`, going from 29
  kinds to 32. `SymbolNotFound` and `SymbolOutOfScope` serve `retiredSymbolId`, and `SymbolRetired` is never raised by
  this tool (Q6, Q7). Each new kind:
  - names the run, key or counts it concerns;
  - avoids the SQL gates' words;
  - has a fact on its distinguishing phrase, a production-route test and a FIRE line.
- **FR-509**: The tool's reader MUST open the map through the existing map-access path. Its SQL lives in named `Read…`
  methods of `CodeMem.Core`'s repositories (Article XI), SELECT-only. Neither bridge project constructs a
  `SqliteConnection`, and the connection-site gate still names one production file, `MapDatabase.vb`. **FIRE**: a
  `New SqliteConnection` in the new reader turns the gates red.
- **FR-510**: The tool's registered description MUST say:
  - it is read-only;
  - candidates are proposals, never applied, and it resolves nothing;
  - `retiredSymbolId` is the id a `SymbolRetired` refusal names;
  - which runs a `retiredSymbolId` call lists (Q2);
  - the count check;
  - an empty answer is common, because a changed signature or body is not a rename;
  - twins are separate candidates;
  - it is uncapped.

  It MUST NOT mention `projectId`, a registry, `code_map_solutions` or a MemOS project (B09 (4)), nor any SQL gate
  word. The text is drafted in the plan's contract and reviewed at STOP 1.

**The server instructions (191497 #1; 191490 §1)**

- **FR-511**: `RunAsync` MUST set the server's instructions to a constant whose text is 191490 §1 verbatim (Q3), with
  LF line endings whatever the checkout (Q12). The instructions are advertised in the `initialize` result.
- **FR-512**: A fact MUST fail when any registered tool name is missing, as a whole word, from the instructions (the
  v1.5.0 gate). **FIRE**: delete the `rename_candidates` line from the constant.
- **FR-513**: A fact MUST pin the instructions' phrases of Q4: the fact, with one FIRE. One deliberate break proves
  the phrase loop (analyze G2, ruled).

**Descriptions and refusal text**

- **FR-514**: `symbol_search`'s description MUST gain *"VB constructors are named New; to find a class's constructions,
  use type_usages on the class."* verbatim. Every existing caveat phrase stays unchanged and still asserted (B07,
  B09 (4)).
- **FR-515**: `SymbolRetired`'s text MUST end "… Search again for the current symbol, or call rename_candidates with
  retiredSymbolId N." with N the retired id. No fact pins today's sentence, so a new fact pins this one. Its production
  route is the refusal's id passed to `rename_candidates`, which answers it (US1 scenario 3).

**The kit in the repository (191497 #1, #2; 191490 §2, §3)**

- **FR-516**: `docs/claude-code/skills/codemem/SKILL.md` MUST be 191490 §3's block verbatim, front matter included
  (Q3).
- **FR-517**: `docs/claude-code/CLAUDE.snippet.md` MUST be 191490 §2's public block verbatim. §2's private block, and
  its heading, MUST NOT enter the repository in any file.
- **FR-518**: Facts MUST pin "The map proves structure; runtime behaviour needs its own proof." in the skill and in the
  snippet, compared with whitespace collapsed (Q4): the snippet breaks the sentence across a line.
- **FR-519**: A fact MUST assert that no file under `docs/` contains "Rick" or "153204" (ordinal). **FIRE**: paste
  §2's private block.

**README, fragment and process document (191490 §4)**

- **FR-520**: `README.md` MUST:
  - say nine tools in all four places (Q8 (a));
  - add a `rename_candidates` row to the tools table carrying 191490's sentence (Q3);
  - insert Quick start step 6, 191490 §4's block verbatim, after step 5;
  - append step 5's closing line, 191490 §4's quoted sentence verbatim, as the last line of step 5 (Q8 (f));
  - add no pointer line (Q8 (c));
  - read "Nine, over stdio." (Q8 (b)) and "v1.5.0, fifteen articles" (Q8 (e));
  - move the suite counts and the Status line at the close (Q8 (d)).
- **FR-521**: A fact MUST assert that the README's tool table lists exactly the registered tools, in both directions
  (the v1.5.0 gate, which covers a tool added, removed or changed). It fails when a registered tool has no row
  (**FIRE**: delete the `rename_candidates` row), and when a row names a tool that is not registered (**FIRE**: add a
  row for an unregistered name). The instructions get no reverse scan: a snake_case scan over prose would misfire on
  words like `not_in_map`. They are covered in one direction by FR-512 and otherwise by the review gate. *Ruled B at
  clarify, 2026-09-29.*
- **FR-522**: `src/CodeMem.Bridge/hooks/settings.fragment.json` MUST name `C:/path/to/CodeMem.Bridge.exe`. B05 (18)
  MUST assert that path and the absence of `C:/Users/`, Red first against today's fragment (Q10).
- **FR-523**: The process document MUST change as Q9 states:
  - nine tools named;
  - a `rename_candidates` entry under "When each tool is called";
  - the refusal table at thirty-two, with `SymbolRetired`'s new remedy;
  - the retired-symbol sentence amended;
  - the "Every tool is scoped by `solutionKey`" sentence deleted;
  - install step 4's machine paths replaced by `C:/path/to/CodeMem.Bridge.exe` (STOP 1).

**Pins, version, invariants**

- **FR-524**: B01 (5) MUST assert the nine names as a literal list, the instructions equal to the constant, and version
  0.3.0. **FIRE**: drop the instructions assignment in `RunAsync`. B09 (4)'s count MUST move from 8 to 9, and B09
  (6)'s refusal-kind count from 29 to 32. Each moved pin records its Red (Q10).
- **FR-525**: The bridge's version MUST move from 0.2.0 to 0.3.0. The extractor's stays 0.3.0 and the schema's stays
  3.
- **FR-526**: The feature MUST change no file under `src/CodeMem.Extraction/` or `src/CodeMem.Extractor/`, and MUST
  leave `SchemaRepository.vb` unchanged (Q11). The fixture's canonical fact set (I02) is unchanged. The close-out
  records the `git diff` that shows it.
- **FR-527**: Acceptance on a copy of the live map (Q13) MUST record both results:
  - `rename_candidates solutionKey=MemOS runId=13` returns exactly one candidate and says the count check ran.
  - `retiredSymbolId=6754` returns no candidates and lists exactly one run, 53.

  The live map's SHA-256 is recorded before and after, and it is unchanged by the suite.
- **FR-528**: The feature MUST change no file outside this repository. The MemOS repository's `git status` is recorded
  clean before and after; `~/.claude` is not touched.
- **FR-529**: Every guard is Red-first where a Red is possible and carries its FIRE in its test header. A guard on
  absence that cannot go Red first records its fire instead (003 FR-219's rule). The suite baseline was re-measured at
  plan: 196 passed / 1 failed / 9 skipped at `806b524`. The one failure is the version pin that STOP 1 retires, and
  one further failure was seen once, unnamed. Task T003 names the baseline with a TRX logger before any change.
- **FR-530**: The v1.5.0 Map-first rule MUST govern this feature's own work. Structure questions about CodeMem's code
  are answered from a current map, or the answer says it came from a stale map or from text. The plan and the
  implementation record name the tool and the count behind every "nothing", "exactly N" or "only X" claim they make.

### Key Entities *(include if feature involves data)*

- **Rename candidate**: one `rename_candidates` row. The run that wrote it, the retired symbol, the new symbol, the
  equal body hash as (B) evidence, `same_path`, `offset_distance` and `rank`. Written by the extractor; read, never
  applied, by this tool. One row is one candidate, for twins too (Q1).
- **Candidate side**: a symbol as the tool presents it: id, doc-comment id, kind, name, container, project, path,
  line and current active state. A retired side can be active again; a new side can be retired since.
- **Examined run**: an `extract_runs` row listed by the tool, with its outcome, recorded `symbols_retired`, recorded
  `rename_candidates`, the candidates returned for it and whether the count check ran.
- **Retiring run**: for a symbol retired now, the first completed run of its solution after its `last_seen_run_id`
  (Q2). Derived, never stored.
- **Server instructions**: the text the bridge advertises at `initialize`: 191490 §1 verbatim, LF, one constant.
- **Usage kit**: the instructions, the `codemem` skill (`docs/claude-code/skills/codemem/SKILL.md`), the public
  `CLAUDE.md` snippet (`docs/claude-code/CLAUDE.snippet.md`) and README step 6. The shipped usage texts the v1.5.0
  gate governs, with every registered tool description and the README's tool list.
- **Refusal kind**: one of thirty-two after this feature, each with one distinct remedy.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-501**: On a copy of the live map:
  - `rename_candidates` with MemOS and run 13 returns exactly 1 candidate (5339 → 23219) and states that the count
    check ran;
  - with 6754 it returns 0 candidates and exactly 1 run (53), where the MemOS-side reader lists 97;
  - unfiltered, it lists every MemOS run, each completed one checked. The total returned equals the sum of recorded
    counts over completed runs.
- **SC-502**: 0 bytes of the fixture map change across every `rename_candidates` call in the suite. `rename_candidates`
  is listed read-only. 0 `SqliteConnection` constructions exist under the two bridge projects, and exactly 1
  production file constructs one.
- **SC-503**: The count-mismatch fixture is refused naming the run and both counts. Each of the three new kinds and the
  two reused symbol kinds fires on its production route. The vocabulary counts 32 kinds.
- **SC-504**: `initialize` returns instructions equal to the constant, with 0 CR characters. 9 of 9 registered names
  appear in them. Each of FR-512's and FR-524's fires is recorded.
- **SC-505**: 6 of 6 shipped texts in Q3's table hash to their ratified SHA-256 (LF line endings, final LF removed).
  The Q4 phrases are present: 4 in the instructions, and 1 in each of the skill and the snippet.
- **SC-506**: The README states a tool count other than nine 0 times. Its tool table's rows equal the 9 registered
  names as a set: 0 missing and 0 extra, each fire recorded. Step 6 directly follows step 5. The fragment's command path is the placeholder.
- **SC-507**: 0 files under `docs/` contain "Rick" or "153204"; the fire is recorded.
- **SC-508**: 0 changed files under `src/CodeMem.Extraction/` and `src/CodeMem.Extractor/`, and in
  `SchemaRepository.vb`. The extractor is at 0.3.0 and the schema at 3. The MemOS repository's `git status` is clean
  before and after. The live map's SHA-256 is unchanged by the suite.
- **SC-509**: The full suite is green on the Debug and the Release build, in under 5 minutes on this machine. Every
  new guard has its FIRE line, and every moved pin (B01 (5), B05 (18), B09 (4), B09 (6)) its Red.
- **SC-510**: The review gates hold:
  - header block and XML docs on every new or changed `.vb` file;
  - Option settings unchanged;
  - no SQL outside a named repository method;
  - the tripwire, SQL-location, bridge-SQL, standalone and connection-site gates green;
  - the v1.5.0 usage-texts gate satisfied, with every text it names updated in this feature.

## Constraints (the description's invariants, and the constitution's)

- **Not a MemOS feature**: one database file (P001 §3; Article IX). No reference, link, copy or vendored source from
  MemOS. The new description and every new text name no MemOS project, store or registry.
- **Rename candidates are proposed, never applied.** The tool reads and resolves nothing, and the bridge never writes
  the map.
- **Not a replacement for reading the source** (P001 §3). The instructions, the skill and the snippet carry it, pinned
  by phrase (Q4).
- **The publication boundary** (decision 142394). The what ships; the how does not. §2's private lines stay out of the
  repository.
- **Nothing installs itself.** The skill and the snippet are copied by a person, and the hook fragment is merged by
  hand.
- **Verbatim means verbatim** (Q3). No shipped kit text is edited to taste. A text the plan finds wrong goes back to
  the Documenter for a ruling; it is not corrected in the repository.
- **The extractor and the schema are untouched** (Q11).

## Measured on 2026-09-29, so the plan starts from facts

- **The map, through the bridge** (`map_status`, 18:16 UTC):
  - DSP_Processor is `current` (run 16).
  - GameRoom and RicksLife are `dirty`.
  - MemOS is `behind` by 1 (run 110).
  - **CodeMem is `behind` by 28 (run 5, `f6488469`, 2026-09-13).** None of the 004 or 005 bridge code is in the map.
    A refresh (`extract solutionKey=CodeMem`) was attempted twice and did not run: the session's permission check
    returned no verdict. So, under v1.5.0's rule, **the facts below about CodeMem's own code come from reading its
    source and from text search over strings, not from the map.** The plan refreshes first.
- **The live candidates, through the MemOS-side reader** (the only reader of `rename_candidates` today):
  - Run 13 (MemOS, completed, 9 retired, 1 recorded) holds exactly one candidate: 5339
    `F:MemOS.Core.Models.CodeMem.CodeMemMapContract.SupportedSchemaVersion` (not active) → 23219
    `…MinimumSupportedSchemaVersion` (active). Both are in `MemOS.Core/Models/CodeMem/CodeMemMapContract.vb`,
    lines 34 → 38, `same_path` 1, `offset_distance` 284, rank 1.
  - The same reader, asked with `retiredSymbolId` 6754, returned **97 run rows**, every MemOS run (6, 7, 13, 17–110),
    to say there is no candidate.
- **Symbol 6754, through the bridge** (`symbol_detail`, refused): `New`, constructor, in
  `MemOS.Business/Managers/PmWriteManager.vb`, retired, last seen in run 52. Run 53 is MemOS's next completed run
  (4 retired, 0 candidates), so it is the retiring run by Q2's derivation, as 191490 states.
- **The schema**: `rename_candidates` has `solution_id`, `run_id`, `retired_symbol_id`, `new_symbol_id`, `body_hash`,
  `same_path`, `offset_distance` and `rank`, with UNIQUE (`run_id`, `retired_symbol_id`, `new_symbol_id`).
  `code_symbols` has no retiring-run column (`SchemaRepository.vb`, 77–157). Rule (B) matches on the resolved
  container id (`Reconciler.vb`, 64–76).
- **The bridge today**:
  - `BridgeServer.vb:31` sets `ServerInfo` and no instructions.
  - Eight names are in `BridgeTools.RegisteredToolNames`.
  - The `SymbolRetired` text is at `BridgeRefusal.vb:80`, and **no test asserts its "consult the rename candidates"
    phrase**.
  - There are 29 refusal kinds (`BridgeRefusalKind.vb`).
- **The pins that move**:
  - B01 (5) compares listed names against `RegisteredToolNames` itself, so it is vacuous for a missing registration,
    and it pins version `0.2.0`.
  - B09 (4) pins `Assert.Equal(8, descriptions.Count)` and scans for forbidden phrases.
  - B05 (18) asserts only that the fragment's command ends with `CodeMem.Bridge.exe" hook`. It passes with the
    placeholder unchanged, so a new assertion is needed for the path.
  - B07 pins "compiled into N projects" in `symbol_search`'s description.
- **The gates the new texts must pass**:
  - `BridgeSqlGateTests` (1): case-sensitive `SELECT|INSERT|UPDATE|DELETE|CREATE|DROP|ALTER|ATTACH|PRAGMA` over
    string literals in both bridge projects.
  - `SqlLocationGateTests`: case-insensitive `SELECT|INSERT|UPDATE|DELETE|CREATE TABLE|PRAGMA` over the tree's
    literals.
  - 005's standalone gate: no case-sensitive `memos` in either bridge project.
  - 191490 §1's text, the `symbol_search` sentence and the new `SymbolRetired` sentence contain none of these.
- **Doc 191490, revised at clarify** (`updated_at` 2026-09-29T18:58:53.676Z): six active sections. §4's fence lines
  are `markdown`, `powershell`, bare, bare: one outer block with the inner fence, closed by the last bare fence. The
  step 5 sentence is §4's one quoted line. The six hashes are in Q3's table, taken read-only from
  `document_sections`.
- **Phrase positions in 191490**: in §1, each Q4 phrase sits on one line. In §2's public block, "The map proves
  structure; runtime behaviour needs its own proof." is split after "needs its" onto an indented line. In §3 it sits
  on one line. Hence the whitespace-collapsed comparison (Q4).
- **Line endings**: `core.autocrlf=true` on this machine. `.gitattributes` pins only `SchemaRepository.vb` to
  `eol=lf` (005 T001), so a new multi-line constant would check out CRLF (Q12).
- **The README** (`d0be120`, unchanged at `806b524`):
  - The tool count is at lines 13, 32, 131 and 248.
  - Step 5 (189) names the fragment and no path.
  - The tools table is at 133–142.
  - "v1.4.0" is at 229.
  - The suite count is 197 passing / 9 skipped (lines 14, 218, 261).
- **The process document**:
  - "eight MCP tools" (12).
  - "Twenty-nine kinds" (170).
  - `SymbolRetired`'s remedy (191).
  - "never returns a retired symbol" (216).
  - Machine paths in install step 4 (54, 64).
- **`docs/`** holds one file, `project-review-2026-09-17.md`, and contains neither "Rick" nor "153204": the privacy
  fact is green on today's tree. `docs/claude-code/` does not exist.
- **The fragment** (`hooks/settings.fragment.json:9`):
  `"C:/Users/rchau/source/repos/CodeMem/src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe" hook`.

## Assumptions

- **Claude Code loads an MCP server's instructions whenever the server is connected** (191490's premise for the first
  layer). The SDK version the bridge references can set them. The plan confirms the property; `initialize` is the
  test.
- **The fixture solution can carry a rename, and a rename inside a linked file**, or the plan adds the smallest
  fixture change that does. The extractor's existing rename-candidate facts are the starting point. If a fixture
  changes, its canonical fact set changes; that is recorded as a fixture change, not an extractor change.
- **Doc 191490 as hashed at clarify is the ratified text** (Q3's table). A later edit is a new ruling.
- **The private lines are placed by hand** in the Architect's own `~/.claude/CLAUDE.md` (191490 §2). Nothing in this
  feature reads or writes `~/.claude`.
- **Registration scope** (user or local) is unverified and does not bind this feature (191497 §Open, task 152685).
  Whether the kit reaches other repositories depends on it, and that is the Operator's check.
- **The suite baseline** was re-measured at plan: 196 passed / 1 failed / 9 skipped at `806b524`. The README's "197
  passing" is the 005 close's figure. T003 names it test by test before the first change.

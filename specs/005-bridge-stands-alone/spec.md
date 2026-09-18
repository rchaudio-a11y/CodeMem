# Feature Specification: CodeMem 005 — The Bridge Stands Alone: One Database, `.slnx` as an Equal Input, Warnings as Warnings

**Feature Branch**: `005-bridge-stands-alone` (from `main` at `90d62ca`, the 004 merge)

**Created**: 2026-09-16

**Status**: Specified 2026-09-16; **clarified the same day (five rulings, session below)**: Q2 ruled A (schema version
3; the Shell's reader is MemOS's concern, left out of the plan); Q1 amended (the extract log is the source, no
start-time filter, deduplicated by directory — "in memory for the process lifetime" withdrawn); Q3 amended (one
option, `--solution-key` with `--solution <path>` optional — `--key` withdrawn); Q8 ruled A with a counterpart NU1101
fixture; Q6 ruled A (one root rule, the extractor's). Q4, Q5, Q7, Q9 and Q10 stand as proposed until STOP 1. Planned
2026-09-16/17 ([plan.md](plan.md), research R60–R73); tasked 2026-09-17. **Analyzed 2026-09-17**: nineteen findings, all
accepted by the Architect and applied (FR-401's gate narrowed to the path-shaped, case-sensitive `memos` — I1; the `.slnx`
open time is measured, not asserted — A1; SC-406's 5 s is the one bound — I4). **STOP 1 ruled 2026-09-17: all ten plan decisions
as proposed** (Q4, Q5, Q7, Q9, Q10 thereby ruled as proposed). **Implemented 2026-09-17** (plan.md
"Implementation record"): T001–T044 done, every Red and FIRE recorded in the test headers, the suite 197 passed / 0 failed /
9 skipped on the Debug and the Release build; the live steps run read-only against the live map and in full on a copy of it
(the copy at schema 3, both `.slnx` re-points, `warnings=3` on RicksLife, `PathNotInMap` with its command on vbCalc).
**T042 and T043 done 2026-09-17**, at the Architect's request and after the T041 backup was re-verified byte-identical:
the live map upgraded to schema 3 by the extractor directly (run 10, DSP_Processor, 2,811 balanced) and RicksLife then
extracted through the bridge (run 11, `warnings=3`); the from-the-map checks run with `memos.sqlite` renamed and the Shell
closed — `DSP_Processor` resolved from the map alone, `vbCalc` refused `PathNotInMap` with the map byte-identical across the
refusal — and the store restored byte-identical, never opened. MemOS re-extracted at the same request (run 13). B08 armed
**8/8**: its `map_status` verdict fact was reworked to recompute each entry's inputs from git, because asserting `current`
was asserting a transient, and (7)'s absorbed-twin count re-pinned 277 → 279 with the arithmetic recorded. **T044 done 2026-09-17**: `DSP_Processor.sln` deleted and committed in its own repository (`98c81d0`), the two MemOS
registry rows re-pointed at their `.slnx` files by the Operator through the Shell, and `extract --repo-path` run once more at
that commit — run 16, launched from `DSP_Processor.slnx`, 2,811 balanced, the source digest unchanged from every run
before it; MemOS `git status --porcelain` empty before the live steps and empty again after. **T045 closed and merged 2026-09-17**:
`main` fast-forwarded 90d62ca → 15a2d9b at the Architect's word, sixteen commits in order. **The feature is complete.**

**Input**: User description: "CodeMem 005: the bridge stands alone. PM: 152687, 152688, 152705. Parent: 004. ### Remove
StoreDatabase, CodeMapSolutionsRepository, storePath, the projectId parameter on every tool, ScopeResolver's registry
branch, and the refusal kinds only the store could raise … Revert Article IX to its v1.2.1 sentence and the
connection-site gate to one file; stamp v1.4.0 with the reason. ### Change extract --repo-path and the hook resolve a
directory to a key through solutions.repo_root (ContainsDirectory), nothing else … ### Add extract --key <name>
--solution <path> … map_status gains verdict not_in_map … ### Extractor: .slnx is an equal input (152688, RULED)
… ### Extractor: warnings are warnings (152705) … Invariants as tests …"

**Governing document**: `.specify/memory/constitution.md` v1.3.0 today; **v1.4.0 after this feature's first task**,
which reverts Article IX to its v1.2.1 sentence and the connection-site gate to one file (FR-431). Where this
specification and the constitution appear to differ, the constitution wins and the difference is a defect in this
document. Article IX is again the article this feature is built around: CodeMem opens no database in any role other
than its own map.

**Parent features**: `specs/004-codemem-bridge/` (shipped, merged to `main` 2026-09-16 as `90d62ca`, live map runs
7–9), on `specs/001`–`003`. Requirements here are numbered FR-401 onward so they never collide with FR-001–FR-039,
FR-101–FR-123, FR-201–FR-219 or FR-301–FR-351. This feature changes the bridge's scope surface and its resolution
rule, and — unlike 004 — changes the extractor in two places: what it can open and what it lets abort a load.

**PM**: task 152687 (005 — cut the store read; filed under 131384 only because the binder refuses a null parent,
defect 152686; a sibling feature, not a child), 152688 (RULED 2026-09-16: `.sln` and `.slnx` as equals; no generated
`.sln` workarounds) and 152705 (defect: NU1701 warnings abort the workspace load), all read from the PM store on
2026-09-16 and quoted where they bind. **Superseded by 152687**, and therefore by this feature: decision 137077's
store-read clause, 142362's "a file, not a dependency", 152658 ruling 1's store clause, and 152672 item 1 (the v1.3.0
amendment). The Architect's sentence that rules all three tasks: "CodeMem is supposed to be a standalone project
completely independent of MemOS; MemOS is a project that can look into CodeMem if it's there."

**Ceremony**: STANDARD, no spike. The one fact the design rests on that nobody here has verified — which of the two
`.slnx` routes opens a solution end to end — is proven in the plan's research with a recorded artefact (152688: "the
Implementor proves one at plan"), not in a spike, because either route is a bounded change inside one loader and the
failing case is already reproduced (§Measured).

## Mission

Claude Code asks the bridge about the map, and the bridge answers from the map — one file, `codemem.sqlite`, and
nothing else on this machine. A green build in any repository the map holds refreshes that solution; a green build in
a repository the map does not hold gets one sentence naming the command that would add it, and nothing is added until
the Architect names the key. A solution whose file is a `.slnx` is mapped exactly as one whose file is a `.sln`. A
package-compatibility warning is recorded, not fatal.

**Out**: writing the map from the bridge (the extractor alone does that); any read of `memos.sqlite`, of MemOS's
`code_map_solutions`, or of a MemOS project id (MemOS keeps its own registry for its own project-to-key mapping — that
is MemOS looking into CodeMem, already built, 152687 §6); the MemOS repository and the Shell's `codemem_*` tools and
suites; a network host; the second-pass tools; adding a solution automatically; walking up or down from a directory to
find one; installing anything into the user's Claude Code settings.

## What this feature removes, changes and adds (the description's three headings, as a table)

| | Today (004) | After 005 |
|---|---|---|
| Databases the bridge opens | two: the map read-only, the store read-only (`code_map_solutions`) | **one**: the map, read-only |
| Scope of the five readers and `type_usages` | exactly one of `projectId` (through the registry) or `solutionKey` | **`solutionKey` only**, resolved against `solutions.key`; a call carrying `projectId` is refused naming the argument |
| `bridge.config.json` | `mapPath`, `storePath`, `extractorPath`, two gates | `mapPath`, `extractorPath`, two gates; a file carrying `storePath` is refused naming the key |
| `extract --repo-path` and the hook | resolve through the registry's bound rows to a map root | resolve through **`solutions.repo_root`** (`ContainsDirectory`), nothing else; a directory under no mapped root answers **not in the map** and names the command that adds it |
| First extraction of a new solution | the Operator's hand run of the extractor, then a bind in MemOS | **`extract --solution-key <name> --solution <path>`** through the same gate, door and log line; the extractor creates the `solutions` row as it does today |
| `map_status` | one entry per active, bound registry row; unbound and inactive rows listed | one entry per **map solution**; observed-but-unmapped directories listed with verdict **`not_in_map`** |
| Refusal vocabulary | 32 kinds | 29: seven store-side kinds retired, four added (FR-409) |
| Constitution | v1.3.0: Article IX carries the store exception; the connection-site gate names two files | **v1.4.0**: Article IX is v1.2.1's sentence; the gate names one file |
| Extractor: `.slnx` | accepted by the extension check, fails in the classic parser (exit 1) | **opened end to end**; `.sln` and `.slnx` yield identical fact sets |
| Extractor: NU1xxx | any workspace Failure aborts the load (exit 1) | an NU1xxx-coded warning is **recorded on the run** and the load continues |

## Clarifications

### Session 2026-09-16 (at specify; before clarify and plan)

The description settles most of the design. The decisions below are the ones it leaves open, each answered with its
reasoning and its alternative so nothing is decided silently; every one is a proposal until the Architect rules.

- Q1: `map_status` gains `not_in_map` for "any repo the hook has observed a green build in that holds no solutions
  row (in memory for the process lifetime; nothing written)". The hook is `CodeMem.Bridge hook`, a one-shot process
  (004 Q5, ruled); `map_status` is served by the `serve` process. The hook's memory dies with the hook. Where does
  the serve process learn what the hook observed, writing nothing new? → A: **From the extract log the hook already
  writes (004 FR-351), read by the serve process on each `map_status` call: every not-in-map line it holds, whatever
  its age, deduplicated by directory, listed while the directory still has no map solution whose root contains it.**
  *Ruled at clarify, 2026-09-16; the description's "in memory for the process lifetime" is withdrawn by the
  Architect — the log is history and the map is the only filter.* Every refused `extract` — the hook's included —
  already appends one line carrying the origin, the target as given (`repoPath=<dir>`) and the refusal by name; a
  not-in-map refusal is therefore already on disk with no new write anywhere. One entry per directory (compared as
  the one door normalises directories: full path, platform separators, trailing separator, case-insensitive),
  carrying the most recent line's time and origin. The suggestion (the key and the command) is recomputed from the
  directory at `map_status` time, never stored. Lines of every origin count — the serve process's own tool-origin
  refusals are in the same log — because a directory Claude Code named to `extract` directly is as observed as one
  the hook named; the description names the hook because it is the origin that observes builds. An entry disappears
  the moment a map solution's root contains the directory (recomputed per call), which is what "the row disappears
  after the solution is added" asks; nothing rewrites the log. Alternatives, not taken: lines newer than the serving
  process's start only (the proposal at specify; rejected by the ruling — a restarted server would forget a build
  it should still report); hook-origin lines only; literal in-process memory (the hook's observations would be
  lost); a sidecar file written by the hook (a new write, which the description forbids).
- Q2: "A workspace diagnostic whose message carries an NU1xxx code is **recorded on the run**." `extract_runs` has no
  column for it and Article X forbids a blob; 004 forbade a schema bump for 004. → A: **Schema version 3: a child
  table of `extract_runs` holding one typed row per warning (the code, the project file the message names, the
  message), written in the run's publication transaction, for completed and failed runs alike; a version-2 map is
  upgraded in place by the extractor on its next run exactly as 002 upgraded 1 → 2; the bridge's own pin moves from
  2 to 3 and its two version refusals keep their remedies.** Reasons: (a) 152705 says "recording them in the run's
  provenance", and provenance lives on the run row's side of the map, not in a log; (b) a count alone would say
  *that* something was tolerated and not *what*, and the next reader would re-derive it from MSBuild's paragraph; (c)
  004 FR-305 anticipated exactly this move ("a Core bump that moves the extractor to version 3 turns the bridge's
  refusal on rather than silently widening it"). *Ruled A at clarify, 2026-09-16.* The MemOS Shell's own reader pins
  the map's version too (137077: "duplicate the readers, pin the schema"); whether and when it moves is MemOS's
  concern, not CodeMem's, and the plan does not carry it. Nothing in the MemOS repository is changed by this
  feature; its suites run on its own fixtures and are unchanged (FR-436). Alternatives, not taken: (b) a single
  `workspace_warnings` count column on `extract_runs` (still a bump,
  less said); (c) no map change — the codes go to the extractor's summary line, the bridge's result and
  `extract.log` only, and "on the run" is read as "on the run's report" (no bump, no Shell consequence, nothing
  queryable; a reader who wants to know which project carried NU1701 reads a log).
- Q3: The description wrote `extract --key <name> --solution <path>` while the bridge already has `--solution-key
  <key>`; 152687 wrote the path as optional. Two spellings for one key? → A: **One option, one spelling:
  `--solution-key <name>`, with `--solution <path>` optional beside it (the tool: `solutionKey` with an optional
  `solutionPath`). The description's `--key` is withdrawn.** *Ruled at clarify, 2026-09-16.* Without the path, the
  key must be in the map and the path is the map's `last_seen_path` (today's behaviour). With the path, the map need
  not hold the key: the extractor is launched with `--solution-key <key> --solution <path>`, creates the `solutions`
  row if the key is new, and refreshes `last_seen_path` if it is not — which is also how the two `.slnx` solutions
  are re-pointed (FR-426). A key the map lacks, given without a path, is refused naming the key and the remedy is the
  command with the path. A path given without a key is a cardinality refusal: the bridge never defaults a key (004
  FR-327's rule stands; the launcher's whole job). Alternatives, not taken: a second spelling of the option
  (synonyms — the proposal at specify; rejected by the ruling as two names for one thing); a distinct option that
  requires `--solution` and refuses a known key (rejected: a second call with the same key must be "an ordinary
  re-extraction", per the description's own invariant); the path always from the map (rejected: it cannot add a
  solution).
- Q4: The not-in-map answer suggests a key "from the solution file name (.sln or .slnx, whichever the directory
  holds; both → refuse as ambiguous, name both)". Which directory, and what when it holds none, or two of one form?
  → A: **The directory the hook or the caller handed to `extract`, itself, never its parents or children.** Exactly
  one solution file of either form in it: the suggestion is its name without extension and the command names that
  file. More than one, in any mix of forms: refused as ambiguous, every file named, no key suggested — the Architect
  chooses. None: still not in the map; the answer names the directory, says no solution file was found in it, and
  gives the command with `<key>` and `<path>` placeholders. Two kinds, because the remedies differ (FR-409). Reasons:
  the hook already hands the directory of the `.sln`, `.slnx` or `.vbproj` the build named, so the usual case holds
  the file; searching upward would be the silent second guess 004 Q6 ruled out; searching downward would find a
  test project's solution. Alternative: walk up to the repository root and look there (rejected for the same reason
  the session directory is never tried).
- Q5: `map_status` and `extract --stale` without a registry: what is an entry, and which verdicts remain? → A: **One
  entry per map `solutions` row, ordered by key; the verdicts are 004's minus `map_missing_solution`, which cannot
  arise without a registry; the `not_in_map` entries are a separate list, keyed by observed directory, not by
  solution.** `bound`, `unbound`, `inactive`, `projectId`, `codememSolutionId` and `storePath` leave the envelope.
  `extract --stale` iterates map solutions whose verdict is `behind`, `dirty` or `diverged`; a `not_in_map` entry is
  listed and never extracted (no key to launch with). A solution whose run recorded no commit or whose root cannot
  be read stays `no_git` with its reason, as today.
- Q6: Resolution by directory uses `solutions.repo_root`; a solution extracted outside any repository has `repo_root`
  NULL. Is it unreachable by path? → A: **No: the root of such a solution is the directory of its `last_seen_path`,
  which is what `SolutionScope.Resolve` already answers when the repository root is absent (003 FR-201), and the
  bridge asks that one door.** *Ruled A at clarify, 2026-09-16: one root rule, the extractor's.* Reasons: the
  extractor scoped that run to exactly that directory; a second definition
  of "root" in the bridge would be a rule at two doors. The 004 resolver skipped such rows; 005 resolves them.
  Alternative: keep skipping them (rejected: a build in that directory would be answered "not in the map" while
  `solutions` lists it).
- Q7: 152688 names two routes for `.slnx`: (a) bump `Microsoft.Build.*` to ≥ 17.12 and verify `OpenSolutionAsync`;
  (b) parse the `.slnx` XML in the loader and open each project with `OpenProjectAsync` into one workspace. Which?
  → A: **The plan proves one and records the artefact; this specification binds the observable contract only
  (FR-421–FR-425): a `.slnx` opens end to end, yields the fact set its `.sln` twin yields, refuses a missing project
  path by name before any compilation, and the loader accepts exactly the extensions it can open.** Both routes
  satisfy it; route (b) additionally gives the extractor a place to refuse a malformed file in its own sentence, and
  moves no package (the Documenter's lean, 152688). If (a) is chosen, 002's F4/F5 transitive pins
  (`Microsoft.Build.Tasks.Core` 17.8.43 clears GHSA-h4j7) are re-verified at the new version and the SDK stamp is
  shown unchanged.
- Q8: "A workspace diagnostic whose message carries an NU1xxx code" — NU1xxx spans NuGet warnings (NU1701, NU1603)
  and NuGet errors (NU1101 "unable to find package"). Are the errors warnings too? → A: **A diagnostic is treated as
  a warning when its message carries an NU1xxx code and MSBuild's own severity word for it is `warning`; an NU1xxx
  `error` keeps aborting the load by name.** *Ruled A at clarify, 2026-09-16, with one addition: a counterpart
  fixture carrying a deliberate NU1101 (a package no configured source holds) asserts exit 1 by name with the
  compiler not invoked (FR-427).* Reason: Article V — green only; a restore error is not a green build, and letting
  it through would surface later as a wall of missing-reference compile errors that name the wrong cause.
  Alternatives, not taken: any NU1xxx code continues (the description's literal reading; rejected for the reason
  above, and it is one word in the rule); a fixed list of known compatibility warnings (rejected: a list is
  maintained, a severity word is read). Whether a suppression through the loader's property dictionary is also needed
  for the design-time build to produce a compilation is the plan's finding; the description forbids the environment
  variable as the mechanism, and a suppressed diagnostic cannot be recorded, so classification is the rule and
  suppression at most a supplement (FR-427).
- Q9 (not asked; decided): Where retired code goes. → A: **`_Archive/004-store/` at the repository root, per Article
  XIV — `StoreDatabase`, `CodeMapSolutionsRepository`, `RegistryTableMissingException`, `RegistryRecord`,
  `StoreAccess`, the registry-only envelopes, the tests' `RegistryFixture`, and the two facts of `BridgeSqlGateTests`
  that only the registry module could satisfy.** Outside `src/` and `tests/`, so no gate scans it and nothing
  compiles it. The description says "remove"; the constitution says archive; the constitution wins and the file is
  gone from the product either way.
- Q10 (not asked; decided): Live acceptance. → A: **As 003 and 004 did: on a copy of the live map first; on the live
  map only at the Architect's request, after a backup.** The live steps are the two `.slnx` re-points (FR-426) and
  the RicksLife extraction with no `NoWarn` in the environment (FR-428); their figures are recorded in the
  quickstart. Deleting DSP_Processor's committed `.sln` is a commit in that repository by the Operator's hand, after
  the re-point; re-pointing the two registry rows' `extraction_scope` at the `.slnx` is MemOS's own affair and
  changes nothing the bridge reads.

### Session 2026-09-16 — clarify (Architect)

- Q: Where should a run's NU1xxx warnings be recorded when the extractor tolerates them? → A: **Option A — schema
  version 3: a typed child table of `extract_runs`, one row per warning (code, project file, message); a version-2
  map upgraded in place on the extractor's next run; the bridge's pin moved to 3 (Q2 as proposed).** The Architect
  added: what the MemOS Shell's reader does with a version-3 map is not CodeMem's concern; it is left out of the
  plan.
- Q: How does the running bridge server learn which directories the hook has seen build without a map entry, so
  that `map_status` can list them as `not_in_map`? → A: **From `extract.log`, with no start-time filter: every
  not-in-map line whose directory still has no map solution whose root contains it, deduplicated by directory, any
  origin, nothing new written (Q1, option A amended).** The description's "in memory for the process lifetime" is
  withdrawn.
- Q: On the command line, should `--key` be a second spelling of the existing `--solution-key` option, with
  `--solution <path>` optional beside either, or a separate option that always requires `--solution`? → A: **One
  option, one spelling: `--solution-key <name>` with `--solution <path>` optional beside it — option A's semantics
  without the synonym (Q3 amended).** The description's `--key` is withdrawn.
- Q: Should every workspace diagnostic carrying an NU1xxx code be tolerated, or only those NuGet reports as a
  warning, with an NU1xxx error still stopping the load? → A: **Only those whose severity word is `warning`; an
  NU1xxx `error` aborts the load by name with exit 1 (Q8, option A).** The Architect added: a counterpart NU1101
  fixture asserts exit 1 by name with the compiler not invoked.
- Q: When a mapped solution was extracted outside any git repository, so the map holds no repository root for it,
  should a build directory still resolve to it through the directory of its last-seen solution file? → A: **Yes —
  one root rule, the extractor's: the repository root when recorded, otherwise the directory of `last_seen_path`
  (Q6, option A).**

## User Scenarios & Testing *(mandatory)*

Actors: **Claude Code**, which asks the bridge and whose builds the hook observes; the **Operator** (the Architect at
this desk), who removes `storePath` from the config beside the executable, names the key of a new solution, runs the
live steps and deletes the generated `.sln`; the **Architect**, who rules and reads the evidence. MemOS is not an
actor and its store is not a file the bridge knows.

### User Story 1 - The bridge answers from the map and nothing else (Priority: P1)

Claude Code, in a session with `memos.sqlite` absent from the machine, calls every read tool by `solutionKey` and gets
the answers 004 gave; `map_status` reports every solution the map holds; the config file beside the executable
carries no `storePath`. A call that still passes `projectId` is refused naming the argument and pointing at
`solutionKey`; a config file that still carries `storePath` is refused naming the key. No connection to any file but
the map is constructed anywhere in the two bridge projects.

**Why this priority**: this is 152687's ruling — the dependency direction in 004 was inverted, and today the bridge
cannot answer a question about its own map without MemOS's store present. Everything else in this feature stands on
the bridge being standalone.

**Independent Test**: In the suite, every tool called against a fixture map with the store fixture removed from the
tree and no `storePath` in the config; the gate over the two bridge projects; the live session in `repos\CodeMem`
with `memos.sqlite` renamed for the duration of the check (the Shell closed) and restored after.

**Acceptance Scenarios**:

1. **Given** a configured map at schema version 3 and no `memos.sqlite` anywhere on disk, **When** `solutions`,
   `symbol_search(solutionKey "GameRoom", name "Draw")`, `symbol_detail`, `references`, `orphans`, `type_usages` and
   `map_status` are called, **Then** each answers in the 004 shape (minus the registry fields, Q5) and none refuses.
2. **Given** any read tool, **When** it is called with `projectId` — alone or beside `solutionKey` — **Then** it is
   refused naming `projectId` as an argument this bridge does not take, with the remedy `solutionKey` and the
   remark that MemOS's own tools take a project id; nothing is opened.
3. **Given** `bridge.config.json` carrying `storePath`, **When** any tool is called, **Then** it is refused naming
   `storePath` as a key the bridge no longer reads; nothing is opened.
4. **Given** the source tree, **When** the gate test runs over `src/CodeMem.Bridge/` and `src/CodeMem.Bridging/`,
   **Then** it finds zero `SqliteConnection` constructions and zero occurrences of the case-sensitive text `memos`
   (a path, never the prose `MemOS`), and it fires when one `New SqliteConnection` is inserted into the library.
5. **Given** the whole of `src/`, **When** the connection-site gate runs, **Then** exactly one production file,
   `MapDatabase.vb`, constructs a `SqliteConnection`.

---

### User Story 2 - A green build refreshes a mapped solution, or says how to map it (Priority: P1)

Claude Code runs `dotnet build` in `repos\DSP_Processor` and it succeeds. The hook hands the build's directory to the
bridge; the bridge finds the one map solution whose root contains it — `DSP_Processor`, from `solutions.repo_root`
alone — and runs the extractor with that key and the map's last-seen path. Claude Code then builds a repository the
map has never seen; the hook's line in the session says the directory is not in the map and gives the exact command
that adds it, with the key suggested from the `.sln` or `.slnx` the directory holds. Nothing was launched; the map is
byte-identical; nothing was added.

**Why this priority**: the trigger is the path 004 was judged on, and its resolution rule is the one place the store
was load-bearing. It must work from the map alone before the store code can go.

**Independent Test**: In the suite, a fixture map holding two solutions with roots in a temp tree: a path under one
root, under none, under two nested roots, under one root shared by two solutions; a directory holding one `.sln`,
one `.slnx`, both, two `.sln`, none. The live half: `extract --repo-path C:\Users\rchau\source\repos\DSP_Processor`
on a copy, resolving to `DSP_Processor`.

**Acceptance Scenarios**:

1. **Given** the live map's five solutions, **When** `extract --repo-path <root of DSP_Processor>` is called with the
   gates on, **Then** the resolved key is `DSP_Processor`, the child is launched with `--solution-key DSP_Processor`
   and `--solution` equal to the map's `last_seen_path`, and the log line names the key.
2. **Given** a directory under no mapped root that holds exactly one `Fresh.slnx`, **When** `extract --repo-path` is
   called by any origin, **Then** the answer is a refusal of kind not-in-map whose text contains `extract --solution-key Fresh
   --solution <that file's full path>`, no process is started, and the map's hash is unchanged.
3. **Given** a directory under no mapped root that holds `A.sln` and `A.slnx` (or two `.sln`), **When** `extract
   --repo-path` is called, **Then** the refusal names every solution file found and suggests no key.
4. **Given** a directory under no mapped root that holds no solution file, **When** `extract --repo-path` is called,
   **Then** the refusal says so, names the directory, and gives the command with `<key>` and `<path>` placeholders;
   no parent or child directory is examined.
5. **Given** a `.slnx` named in the build command (`dotnet build C:\…\RicksLife\RicksLife.slnx`), **When** the hook
   parses the payload, **Then** the target directory is that file's directory, exactly as for a `.sln`.
6. **Given** the hook's answer for an unmapped directory, **When** it reaches the session, **Then** it is one line of
   `additionalContext` containing the command, the tool call that ran the build is not failed, and the hook exits 0.
7. **Given** a mapped solution whose run recorded no repository (`repo_root` NULL), **When** a directory under the
   directory of its `last_seen_path` is given, **Then** it resolves to that solution (Q6).

---

### User Story 3 - The Architect adds a solution through the bridge (Priority: P2)

The Architect reads the hook's line, decides the key, and runs `CodeMem.Bridge extract --solution-key Fresh --solution
C:\…\Fresh.slnx`. The same gate is checked, the same door walks the same steps, one line is appended to the same log;
the extractor creates the `solutions` row as it always has and publishes run 1 of `Fresh`. `map_status` now lists
`Fresh` with a verdict and no longer lists the directory as `not_in_map`. A second identical call is an ordinary
re-extraction: matched > 0, new = 0.

**Why this priority**: 152687 §3 — "this is the onboarding — no registry, no bind page, nothing to do in MemOS". It
replaces the Operator's hand run plus the MemOS bind that 004 Q1 left in place.

**Independent Test**: On a copy of a fixture map: `extract --solution-key Fresh --solution <fixture copy>` twice; `map_status`
before (after one observed refusal for that directory) and after.

**Acceptance Scenarios**:

1. **Given** `extract.enabled` true and a key the map lacks, **When** `extract --solution-key Fresh --solution <path>` is
   called, **Then** the child exits 0, a new `solutions` row with key `Fresh` and `last_seen_path` equal to the path
   exists, the run's ten counts balance (observed = matched + reactivated + new; registry_before − matched − retired
   = 0), and the log carries one line with the key.
2. **Given** the same call repeated, **When** it completes, **Then** the run reports matched > 0 and new = 0 and no
   second `solutions` row exists.
3. **Given** `extract --solution-key Fresh` without `--solution` and no map solution `Fresh`, **When** called, **Then** it is
   refused naming the key and the remedy is the command with `--solution <path>`; nothing runs.
4. **Given** `extract --solution <path>` without a key, **When** called, **Then** it is refused for cardinality
   (the four target shapes named); the bridge never defaults a key.
5. **Given** `extract.enabled` false, **When** `extract --solution-key … --solution …` is called by any origin, **Then** it is
   refused naming `extract.enabled`, exactly as every other target shape.
6. **Given** `extract --repo-path <dir>` has at any time been refused as not in the map (one line in `extract.log`,
   by any origin, by any bridge process), **When** `map_status` is called, **Then** `dir` is listed once with verdict
   `not_in_map`, the suggested key or the ambiguity, and the command, even after the server has been restarted;
   **When** an `extract --solution-key … --solution …` has since published a solution whose root contains `dir`, **Then** the entry is gone and
   the solution has an entry of its own; the log line stays.

---

### User Story 4 - `.slnx` is an equal input (Priority: P2)

The Operator points the extractor at `RicksLife.slnx` and it maps the five projects it names, exactly as the generated
`RicksLife.sln` did; the same for `DSP_Processor.slnx`. The committed fixture, in both forms, extracts to the same
fact set. A `.slnx` that names a project file which does not exist is refused naming that path before anything is
compiled. The extractor's usage names `.slnx`, and the loader's extension check promises only what the code beneath
it delivers.

**Why this priority**: 152688, ruled — every new Visual Studio solution is `.slnx`-native; the two solutions added on
2026-09-16 were mapped only through generated `.sln` files, one of which is now a committed workaround.

**Independent Test**: `Sample.sln` and a committed `Sample.slnx` naming the same two projects, each extracted into a
fresh map, the two fact sets compared (I02 across formats); a `.slnx` naming `Missing/Missing.vbproj`; the live
re-points on a copy, then on the live map at the Architect's request.

**Acceptance Scenarios**:

1. **Given** the fixture in both forms, **When** each is extracted into a fresh map, **Then** the canonical fact sets
   (symbols by doc id, parts, edges, ten counts) are identical; `solutions.key` defaults to `Sample` for both; and
   `last_seen_path` records the file that was passed, extension included.
2. **Given** a `.slnx` naming a project path that does not exist, **When** the extractor runs, **Then** it exits 1
   with one line naming that path, before any compilation is attempted and with nothing written.
3. **Given** a `.slnx` that is not well-formed XML or has no `Solution` root, **When** the extractor runs, **Then** it
   exits 1 naming the file, in the extractor's own sentence.
4. **Given** `RicksLife.slnx` (five projects) and `DSP_Processor.slnx` (two), **When** each is extracted with its key
   and the `.slnx` path on the live map (Q10), **Then** each publishes a completed run whose counts balance, and
   `solutions` shows `last_seen_path` ending in `.slnx` for both.
5. **Given** the extractor's usage text, **When** printed, **Then** it names `.sln`, `.slnx` and `.vbproj` and nothing
   else as inputs, and a path with any other extension is refused naming the extension before a workspace is
   created.

---

### User Story 5 - Warnings are warnings (Priority: P3)

The Operator extracts RicksLife with nothing set in the environment. NuGet's NU1701 ("restored using .NETFramework
instead of net10.0") arrives as a workspace diagnostic; the loader records it against the run and continues; the
compile is checked as always; the run publishes with exit 0 and the warning readable on the run. A fixture with a
deliberate NU1701 does the same in the suite; the same fixture with a real compile error still exits 2.

**Why this priority**: 152705 — run 9 of RicksLife succeeded only with `NoWarn=NU1701` in the process environment,
"Operator memory, not a tool rule". Lowest priority because the map is right once the run completes; the defect is
that it did not complete.

**Independent Test**: A fixture project that references a package built for .NET Framework only, from a local package
source checked in beside it so the suite stays offline; the same fixture with a `.vb` file carrying a compile error;
a counterpart fixture referencing a package that local source does not hold (NU1101).

**Acceptance Scenarios**:

1. **Given** the NU1701 fixture, **When** extracted with no `NoWarn` in the environment (asserted), **Then** the exit
   code is 0 and the run carries at least one warning row with code `NU1701` and the project file named.
2. **Given** the same fixture with a compile error added, **When** extracted, **Then** the exit code is 2 and nothing
   is written, as today.
3. **Given** the NU1101 fixture (a package reference the fixture's local source cannot resolve), **When** extracted,
   **Then** the exit code is 1, one line names `NU1101` and the project, nothing is written, and no compilation was
   requested — the loader stopped before the compiler (Q8 as ruled).
4. **Given** a completed run with warnings, **When** `solutions` or `extract` reports it, **Then** the warning count
   is present, and the `extract` result lists the rows.
5. **Given** a version-2 map, **When** the extractor next runs, **Then** it upgrades the map in place to version 3
   and publishes; **When** the bridge (pinned at 3) opens a version-2 map before that, **Then** it refuses naming the
   version with the remedy "run the extractor once".

---

### Edge Cases

- **Two mapped roots nest** (one repository checked out inside another's tree): the longest root that contains the
  directory wins, as in 004; unchanged.
- **One root shared by two map solutions** (a `.sln` and a `.vbproj` of the same repository extracted under two keys):
  ambiguous; refused naming both keys and asking for `--solution-key`. Now observable without a registry; stated so
  it is not guessed.
- **A directory under a mapped root that also holds an unrelated `.slnx`**: it resolves to the mapped solution; the
  file in the directory is never consulted when the map answers. The suggestion is made only for a directory no
  root contains.
- **The observed directory no longer exists** when `map_status` runs (a temp build): the entry is still listed with
  what the log recorded; the suggestion says the directory could not be read now.
- **The log holds not-in-map lines from days ago, or from another bridge process** (yesterday's builds, a hook that
  ran while no server was up): listed, as long as the directory still has no map solution whose root contains it
  (Q1 as ruled); the log is history and the map is the only filter. Two lines for one directory are one entry.
- **The extract log cannot be read** by `map_status`: the entries list is empty and the envelope says the log could
  not be read and why; the solution entries are unaffected.
- **A hook fires while the store fixture or `memos.sqlite` is present on disk**: irrelevant; the bridge has no path
  to it and no key that names it.
- **A `.slnx` with folders** (`<Folder Name="/src/"><Project Path="…"/></Folder>`): every `Project` element at any
  depth is a project; folders are organisation, not scope.
- **A `.slnx` naming a project type the workspace cannot open** (a `.csproj` where no C# support is loaded, a
  `.vcxproj`): whatever the `.sln` twin does today; parity is the contract, and a load failure is exit 1 naming the
  project.
- **A `.slnx` naming no project**: refused naming the file (nothing to compile); nothing written.
- **`.slnx` project paths with forward slashes** (the format's own convention) and with `..` segments: resolved
  against the `.slnx` directory, normalised to the platform, never checked beyond existence before the workspace
  opens them.
- **A solution whose `.sln` and `.slnx` both exist and are both mapped under different keys**: two map solutions, two
  roots equal; a directory under them is ambiguous (above). The remedy on this desk is to delete the generated `.sln`
  after the re-point (FR-426), not to map both.
- **NU1xxx appears in the same diagnostic as a non-NuGet failure**: the diagnostic is one message with one severity
  word; `warning` continues, `error` aborts; the plan records the observed message shape.
- **A warning row on a failed run** (exit 4, residual mismatch): written beside the failed run row, because the load
  did happen; a run that never reached a run row (exit 1, exit 2) records nothing, as today.
- **A retired refusal kind in an old `extract.log` line** (`PathNotRegistered`, `KeyUnbound`): the log is append-only
  history; `map_status` reads only the current not-in-map kind; nothing rewrites the log.
- **`projectId` arrives as an unknown argument the protocol layer would ignore**: the bridge inspects the raw
  arguments of every tool call so that the refusal by name is possible (FR-406); an unknown argument other than
  `projectId` is ignored as today.

## Requirements *(mandatory)*

### Functional Requirements

**One database (152687 §1, §5; Article IX at v1.4.0)**

- **FR-401**: The bridge MUST open exactly one database file, the map, read-only per call as 004 FR-303 states. Neither
  bridge project may construct a `SqliteConnection`, and neither may name the store's file: no occurrence of the
  case-sensitive text `memos` (the file is `memos.sqlite` and a path segment is lowercase; prose that says `MemOS`, as
  the projectId refusal does, is not a path and is allowed — analyze I1, 2026-09-17); a gate test over
  `src/CodeMem.Bridge/` and `src/CodeMem.Bridging/` asserts both and carries its FIRE: one `New SqliteConnection`
  inserted into the library turns it red.
- **FR-402**: `StoreDatabase`, `CodeMapSolutionsRepository`, `RegistryTableMissingException`, `RegistryRecord`,
  `StoreAccess`, `ScopeResolver`'s registry branch, the registry envelopes, the tests' `RegistryFixture` and the two
  registry-only facts of `BridgeSqlGateTests` MUST leave the product and the suite, moved to `_Archive/004-store/`
  (Article XIV, Q9). After this feature no file under `src/` or `tests/` references any of those names; a gate
  asserts it.
- **FR-403**: `bridge.config.json` MUST carry `mapPath`, `extractorPath` (optional) and the two gates only. A file
  carrying `storePath` MUST be refused by name, naming the key, before anything is opened (a kind of its own, FR-409).
  The shipped sample and the process document drop the key; the Operator removes it from the file beside the
  Release executable (Q10).
- **FR-404**: The `.mcp.json`, the settings fragment and the hook entry are unchanged in shape; the hook's answer
  line and the `extract` command line change only as FR-411–FR-420 state.

**Scope by key (152687 §1)**

- **FR-405**: `symbol_search`, `symbol_detail`, `references`, `orphans` and `type_usages` MUST take `solutionKey` as
  their only scope argument, resolved against `solutions.key` exactly; a missing key is refused by name
  (`ScopeMissing`'s text becomes "supply solutionKey"); an unknown key is `SolutionKeyUnknown` as today, with the
  remedy extended by the add command. The result's `scope` envelope carries `by: "solutionKey"`, the key and the one
  solution; the four registry counts, `dangling` and `hadNothingToSearch` leave it.
- **FR-406**: No tool MUST accept `projectId`. A call carrying `projectId` — whatever else it carries — MUST be refused
  naming the argument, with the remedy `solutionKey` and the remark that MemOS's own `codemem_*` tools take a
  project id; nothing is opened. The bridge inspects the call's raw arguments to make that possible.
- **FR-407**: Every registered tool description MUST be revised so that none mentions `projectId`, a registry,
  `code_map_solutions` or a MemOS project; a fact asserts the absence of those four phrases across all eight
  descriptions and the presence of every 004 FR-312 caveat phrase, unchanged.
- **FR-408**: The refusal order of 004 FR-315 becomes arguments → configuration → map → question: there is no store
  stage.
- **FR-409**: The vocabulary (004 FR-306) MUST retire seven kinds — `RegistryAbsent`, `ScopeConflict`,
  `KeyNotRegistered`, `KeyUnbound`, `KeyInactive`, `MapMissingSolution`, `PathNotRegistered` — and the reason text
  "no registry row names project N"; and MUST add four — `PathNotInMap` (the directory is under no mapped root; the
  text names the directory, the mapped roots, and the add command with the suggested key when exactly one solution
  file is found or with placeholders when none is), `AmbiguousSolutionFile` (more than one solution file found; every
  file named, no key suggested), `ProjectIdRemoved` (FR-406) and `ConfigKeyRetired` (FR-403, naming `storePath`).
  `Unopenable` loses its store variant. Twenty-nine kinds; each text names the file, key or argument it concerns;
  each new kind's distinguishing phrase is asserted by a fact and its production route demonstrated (Articles II,
  XIII).

**Resolution from the map (152687 §2; 004 FR-326 amended)**

- **FR-410**: `extract` with `repoPath`, by every origin, MUST resolve the directory through the map alone: the one
  `solutions` row whose root — `repo_root`, or the directory of `last_seen_path` when `repo_root` is NULL (Q6),
  through `SolutionScope.Resolve` and `ContainsDirectory`, the one door — contains it or is it, the longest such root
  winning, a tie refused as `AmbiguousRoot` naming the keys. The map's `last_seen_path` is the `--solution` passed
  to the child, as today.
- **FR-411**: A directory under no root MUST be answered `PathNotInMap` (Q4): the answer names the directory and the
  mapped roots; inspects that directory — never a parent or child — for `*.sln` and `*.slnx`; and when exactly one is
  found suggests the key (the file name without extension) and states the command `extract --solution-key <key> --solution
  <full path>` verbatim in the text; when none is found, states the command with `<key>` and `<path>` placeholders and
  says no solution file was found there. More than one is `AmbiguousSolutionFile`. No process is started; the map is
  byte-identical (asserted by hash); the bridge never adds a solution itself.
- **FR-412**: The hook's target parse (004 FR-335) MUST treat a `.slnx` token exactly as a `.sln` token: the
  directory of the file named, authoritative by shape.
- **FR-413**: The hook's line for an unmapped directory MUST carry the refusal text, command included, as one line of
  `additionalContext`; the hook exits 0 and never fails the observed tool call, as today.

**Adding a solution through the bridge (152687 §3; Q3)**

- **FR-414**: The by-key target shape MUST accept an optional explicit solution path — on the command line
  `--solution-key <name> [--solution <path>]`, one option and one spelling (Q3 as ruled), on the tool `solutionKey`
  with optional `solutionPath`. Cardinality is unchanged: exactly one of `solutionKey`, `repoPath`, `stale`;
  `solutionPath` only beside `solutionKey`; a path without a key is `TargetMissing` whose text names the three shapes
  and the optional path. The bridge never defaults a key.
- **FR-415**: With a path, the map need not hold the key: the child is launched with `--solution-key <key>
  --solution <path> --db <map>`; the extractor creates the `solutions` row when the key is new and refreshes
  `last_seen_path` when it is not (Stage A FR-032, unchanged). Without a path, the key must be in the map
  (`SolutionKeyUnknown` otherwise, with the add command as the remedy) and the path is the map's.
- **FR-416**: The fourth shape walks the same door — cardinality, configuration, the gates (`extract.enabled` for
  every origin; `extract.onGreenBuild` additionally for the green-build origin), the map read once and closed before
  the launch, one extraction per process, the launch under the 540 s budget, the run read back, the log line — with
  no step added or skipped; a fact asserts the gate matrix of 004 SC-305 for it and one line per invocation in the
  log with the target as given (`solutionKey=<name> solution=<path>`).
- **FR-417**: A second call with the same key and path MUST be an ordinary re-extraction: the run reports matched > 0
  and new = 0 for an unchanged tree, and no second `solutions` row exists.

**`map_status` (152687 §4; Q1, Q5)**

- **FR-418**: `map_status` MUST return one entry per map `solutions` row, ordered by key, with the facts and the
  verdict of 004 FR-320–FR-322 minus `map_missing_solution`: `current`, `behind`, `dirty`, `diverged`, `no_git`, in
  004's order. `bound`, `unbound`, `inactive`, `projectId`, `codememSolutionId` and `storePath` leave the envelope.
- **FR-419**: `map_status` MUST additionally list, under `notInMap`, every distinct directory that any `extract`, by
  any origin and any bridge process, has ever refused as `PathNotInMap` or `AmbiguousSolutionFile` — learned from
  every such line in `extract.log`, whatever its age, with no start-time filter, nothing written (Q1 as ruled) —
  deduplicated by directory (compared as the one door normalises directories), each with verdict `not_in_map`, the
  directory, the most recent line's time and origin, the solution files found in it now, the suggested key or the
  ambiguity, and the command (recomputed per call, never stored). A directory that a map root now contains is not
  listed; nothing rewrites the log. A log that cannot be read is reported as such in the envelope; the solution
  entries are unaffected.
- **FR-420**: `extract --stale` MUST iterate map solutions whose verdict is `behind`, `dirty` or `diverged`, one run
  each, skipping and listing the rest; `not_in_map` entries are listed and never extracted.

**The extractor: `.slnx` as an equal input (152688, ruled; Q7)**

- **FR-421**: The extractor MUST open a `.slnx` end to end — every `Project` element at any depth, its `Path`
  resolved against the `.slnx` directory with the format's forward slashes normalised — and continue exactly as for
  a `.sln`: one workspace, every project compiled, the same staging, reconciliation and publication.
- **FR-422**: Format parity: the committed fixture in both forms (`Sample.sln`, `Sample.slnx`, the same two projects)
  MUST extract into two fresh maps whose canonical fact sets are identical (I02's comparison, across formats);
  `solutions.key` defaults to the file name without extension for both; `last_seen_path` records whichever file was
  passed.
- **FR-423**: A `.slnx` naming a project path that does not exist MUST be refused naming that path, exit 1, before
  any compilation and with nothing written; a `.slnx` that is not well-formed XML, has no `Solution` root, or names no
  project MUST be refused naming the file, exit 1, in the extractor's own sentence.
- **FR-424**: The loader's extension check MUST accept exactly `.sln`, `.slnx` and `.vbproj` and refuse any other
  extension by name before a workspace is created; the extractor's usage text names the three.
- **FR-425**: If route (a) is chosen, the transitive pins of 002 (F4/F5) MUST be re-verified at the new
  `Microsoft.Build.*` version and the recorded SDK stamp shown unchanged; if route (b), no package moves. Either way
  the plan records the artefact that opened a `.slnx` end to end before the first task.
- **FR-426**: On the live map, at the Architect's request and after a backup (Q10), `DSP_Processor` and `RicksLife`
  MUST be re-extracted through `extract --solution-key <key> --solution <the .slnx>`, so that each `last_seen_path` ends in
  `.slnx`; the quickstart records the two runs' counts; then the Operator deletes DSP_Processor's committed `.sln`
  (a commit in that repository, by hand) and the next `extract --repo-path` of that root runs from the `.slnx`.

**The extractor: warnings are warnings (152705; Q2, Q8)**

- **FR-427**: A workspace diagnostic whose message carries an NU1xxx code with severity `warning` MUST NOT abort the
  load; it MUST be recorded on the run (FR-429) and the load MUST continue to compilation, where errors are judged
  exactly as today (exit 2 on any). An NU1xxx `error`, and any other Failure diagnostic, aborts by name as today,
  before any compilation is requested; a counterpart fixture with a deliberate NU1101 asserts exit 1 naming the code
  and the project, nothing written, the compiler not invoked (Q8 as ruled; the plan names the observable that proves
  no compilation was requested). The extractor MUST NOT read or require any environment variable for this (a fact asserts the child runs with
  `NoWarn` absent from its environment); if the plan finds a suppression is also needed for the design-time build to
  yield a compilation, it is passed through the loader's property dictionary and the diagnostic is still recorded.
- **FR-428**: The extractor's summary line MUST carry the warning count (`warnings=N`), and its usage text MUST name
  the failure class ("workspace load failed" versus "compile errors") so a refusal is the tool's sentence, not
  MSBuild's paragraph (152705).
- **FR-429**: Schema version 3 (Q2): a typed child table of `extract_runs` holds one row per warning — the code, the
  project file the message names (nullable), the message — written in the publication transaction beside completed
  and failed run rows alike, never as a blob (Article X). A version-2 map is upgraded in place on the extractor's
  next run as 002 upgraded 1 → 2: the DDL text of versions 1 and 2 stays frozen; the migration is one added text;
  the fresh path and the upgrade path yield byte-identical schemas; the abort seam covers the new step.
- **FR-430**: The bridge's pin (004 FR-305) MUST move to exactly 3 with both remedies unchanged; `solutions` reports
  each latest run's warning count; the `extract` result lists the published run's warning rows; every 002 fact that
  pins "current = 2" is revised to 3 and its Red recorded.

**Governance (152687 §5; the description's "stamp v1.4.0 with the reason")**

- **FR-431**: The constitution MUST be amended to v1.4.0 in this feature's first task: Article IX's text returns to
  v1.2.1's, exactly — the ruled-exception paragraph removed and nothing else touched — and the Review Gate names one
  production file, `MapDatabase.vb`; the Amendment Record gains a dated entry carrying the reason (152687's ruling
  and its drift record: 137077 → 142362 → 152658 → 152672, each step small, the sum a bridge that could not answer
  without MemOS's store) and the migration path (the archive of FR-402; the gate test amended in the task in which
  `StoreDatabase.vb` leaves `src/`, after its Red is recorded, and re-fired). A fact asserts Article IX's text equals
  a verbatim transcription of v1.2.1's and that the connection-site gate passes with one file.
- **FR-432**: The four superseded decisions are named as superseded in the amendment entry and in the process
  document; nothing else in MemOS's PM is written by this feature.

**Invariants as tests (Articles II, III, XIII; the description's list)**

- **FR-433**: Every tool call in the suite MUST succeed against a fixture map with no `memos.sqlite` on disk and no
  store fixture in the tree; the live check renames `memos.sqlite` for its duration with the Shell closed and
  restores it (recorded in the quickstart).
- **FR-434**: Facts MUST assert, through the launcher seam and by hash: `extract --repo-path <root of
  DSP_Processor>` resolves to `DSP_Processor` from the map alone (on a copy of the live map); `extract --repo-path
  <unmapped dir>` returns not-in-map with the command in the text, zero launches, map byte-identical; `extract --solution-key
  Fresh --solution <fixture>` on a copy exits 0 with a new row and ten balanced counts, and its repeat is a
  re-extraction; `map_status` lists and then drops the `not_in_map` entry.
- **FR-435**: Facts MUST assert format parity (FR-422), the missing-path refusal (FR-423), the NU1701 fixture's exit 0
  with its warning row, the same fixture's exit 2 with a compile error, the NU1101 fixture's exit 1 by name with the
  compiler not invoked (FR-427), and Article IX's text (FR-431).
- **FR-436**: The feature MUST change no file in the MemOS repository; the quickstart records that repository's `git
  status` clean before and after, and the 004 project-file gate stays green: the Shell's `codemem_*` suites are
  untouched.
- **FR-437**: Every guard is Red-first where a Red is possible and carries its FIRE in the test header; a guard on
  absence that cannot go Red first records its fire instead (003 FR-219's rule). The 004 facts that exercised
  `projectId`, the registry fixture and the store-arming variable `CODEMEM_LIVE_STORE` are revised or archived with
  the code they tested; the suite's baseline (145 passed / 8 skipped / 2 m 33 s on 2026-09-16) is the figure the
  close-out compares against.

### Key Entities *(include if feature involves data)*

- **Mapped solution**: one `solutions` row — `key` (exact; the scope of every read tool), `repo_root` (the root the
  extractor scoped the last run to; NULL when there was no repository), `last_seen_path` (the `.sln`, `.slnx` or
  `.vbproj` the last run was pointed at; the `--solution` of the next run through the bridge). The unit of
  `map_status` and of `extract --stale`. Created by the extractor on a solution's first run; never by the bridge.
- **Mapped root**: `repo_root`, or the directory of `last_seen_path` when `repo_root` is NULL (Q6); the directory a
  `repoPath` is resolved against, through `SolutionScope`, the one door.
- **Observed directory**: a directory an `extract` refused as `PathNotInMap` or `AmbiguousSolutionFile`, known from
  every such line in `extract.log`, whatever its age, one entry per directory (Q1 as ruled); listed by `map_status`
  with verdict `not_in_map` until a mapped root contains it. Never stored beyond the log line 004 already writes.
- **Suggested key**: the name without extension of the one `.sln` or `.slnx` an observed directory holds; absent when
  it holds none or more than one. Proposed in text; never applied.
- **Target shape**: how an `extract` names what to run — by key (with the path from the map, or with an explicit
  `solutionPath`), by `repoPath`, or `stale`. Exactly one per call.
- **Run warning**: a typed row keyed by an `extract_runs` row — code (`NU1701`), the project file named (nullable), the
  message — written when a load continued past an NU1xxx warning (Q2). Schema version 3.
- **Verdict**: for a mapped solution, one of `current`, `behind`, `dirty`, `diverged`, `no_git`; for an observed
  directory, `not_in_map`. Never stored; computed per call.
- **Bridge configuration**: `bridge.config.json` beside the executable — `mapPath`, `extractorPath` (optional),
  `extract.enabled`, `extract.onGreenBuild`; never defaulted, never cached; `storePath` refused by name.
- **Extract log line**: unchanged in shape (004 FR-351); the target-as-given column gains the fourth shape's
  `solutionKey=<name> solution=<path>`; now also the serving process's source for observed directories.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-401**: With `memos.sqlite` absent from disk, every one of the eight tools answers in the suite and in a live
  session in `repos\CodeMem`; 0 `SqliteConnection` constructions and 0 occurrences of the case-sensitive text `memos`
  under the two bridge projects; exactly 1 production file under `src/` constructs a `SqliteConnection`; the gate
  fires on one inserted construction.
- **SC-402**: A call to any tool carrying `projectId` is refused naming `projectId` in 100% of the eight; a config
  carrying `storePath` is refused naming `storePath` by every tool; 0 registered descriptions mention `projectId`,
  a registry, `code_map_solutions` or a MemOS project; every 004 caveat phrase is still present.
- **SC-403**: `extract --repo-path C:\Users\rchau\source\repos\DSP_Processor` resolves to `DSP_Processor` on a copy of
  the live map with no store present; `extract --repo-path <unmapped dir holding Fresh.slnx>` returns text containing
  `extract --solution-key Fresh --solution` and that file's path, starts 0 processes and leaves the map's SHA-256 unchanged;
  the two-file and no-file cases return their own texts with 0 processes.
- **SC-404**: `extract --solution-key Fresh --solution <fixture copy>` exits 0, creates exactly 1 `solutions` row, and its run's
  ten counts balance; the repeated call reports matched > 0 and new = 0 and the row count stays 1; the log holds
  exactly 2 lines for the two calls.
- **SC-405**: `map_status` lists an observed unmapped directory with verdict `not_in_map`, the suggested key and the
  command within one call of the refusal, and lists it no longer within one call of the `extract --solution-key … --solution …` that mapped
  it; a restarted server lists the same directories (the log, not memory, is the source); two refusals for one
  directory produce exactly 1 entry.
- **SC-406**: The fixture in both forms yields identical fact sets (0 differing lines; ≥ 1 symbol, part and edge line
  present); `key` defaults to `Sample` for both; `last_seen_path` ends in `.sln` and `.slnx` respectively; a `.slnx`
  naming a missing project exits 1 naming the path in under 5 s with 0 rows written. On the live map after the
  re-points, `DSP_Processor` and `RicksLife` show `last_seen_path` ending in `.slnx`, each with a completed run whose
  counts balance, and `DSP_Processor.sln` no longer exists in that repository.
- **SC-407**: The NU1701 fixture extracts with exit 0 and ≥ 1 warning row with code `NU1701` and its project named,
  with `NoWarn` absent from the child's environment; the same fixture with a compile error exits 2 with 0 rows
  written; the NU1101 fixture exits 1 naming `NU1101` with 0 compilations requested and 0 rows written; RicksLife
  extracts live with exit 0 and its NU1701 rows recorded, with nothing set in the environment.
- **SC-408**: Article IX's text equals v1.2.1's verbatim (0 differing characters in the article's body); the
  constitution's version line reads 1.4.0 with a dated entry naming the four superseded decisions; the connection-site
  gate names one file.
- **SC-409**: The MemOS repository's `git status` is clean before and after; 0 project files reference
  `rchaudio-a11y` or `MemOS`; 0 files under `src/` or `tests/` reference the archived names.
- **SC-410**: The full suite green, under 5 minutes on this machine; every new fact green; every new guard with its
  FIRE line; the 002 and 003 suites green at schema version 3 with their revised pins and their Reds recorded.
- **SC-411**: Review gates: header block and XML docs on every new or changed file; Option settings unchanged in every
  touched project file; no SQL outside a named repository method; the tripwire, SQL-location, MemOS-reference and
  the new memos-free gates green.

## Constraints (the description's "Do not", and the constitution's)

- No reference, link, copy or vendored source from MemOS (004 FR-302 stands); no path, key or literal naming its store.
- The bridge never writes the map, and now never opens anything but the map (Article IX at v1.4.0).
- The bridge never adds a solution, never chooses a key, never walks up or down from a directory (Q4).
- The extractor reads no environment variable for warnings (FR-427); test seams are the only variables it reads.
- No `rename_candidates`, `unregistered`, `repo_drift`, `external_usage` or `spec_conformance`; second pass, as before.
- No auto-install of the hook; no edit of the user's `.claude/settings.json`; the trigger refreshes the built solution
  only.
- Nothing in the MemOS repository changes; the registry rows in its store are its own and are not written by this
  feature.

## Measured on 2026-09-16 (local; 2026-09-17 UTC), so the plan starts from facts

- **The map** (`C:\_DB\codemem.sqlite`, schema 2, five solutions): CodeMem run 5; DSP_Processor run 8
  (`last_seen_path` `…\DSP_Processor\DSP_Processor.sln`, `repo_root` `…\DSP_Processor\`); GameRoom run 4; MemOS run 7;
  RicksLife run 9 (`last_seen_path` `…\RicksLife\RicksLife.sln`, `repo_root` `…\RicksLife\`). `map_status` through the
  004 bridge: CodeMem `behind` by 6 (HEAD `90d62ca9`), DSP_Processor `behind` by 1 (HEAD `d07d341b`), GameRoom `dirty`,
  MemOS `behind` by 2 (HEAD `91db2d72`), RicksLife `dirty`; the registry's five rows all active and bound — so
  removing the registry loses nothing reachable today.
- **The `.slnx` files**: `DSP_Processor.slnx` names two projects; `RicksLife.slnx` names five; both are the two-element
  shape `<Solution><Project Path="…"/></Solution>`. `DSP_Processor.sln` (2 898 bytes) is **committed** in that
  repository as `d07d341` ("add initial Visual Studio solution file"), the commit `map_status` reports it behind by;
  `RicksLife.sln` was never committed and no longer exists, so the map's `last_seen_path` for RicksLife already
  points at a file that is gone.
- **The defect, reproduced**: `CodeMem.Extractor --solution …\RicksLife.slnx --db <scratch>` → exit 1,
  `InvalidProjectFileException: No file format header found.`, 0.42 s; the resolved `Microsoft.Build` is 17.7.2
  with `Microsoft.Build.Tasks.Core` pinned at 17.8.43 (002 F4/F5), Roslyn workspaces 4.14.0.
- **The suite baseline**: 145 passed / 8 skipped / 0 failed, 2 m 33 s, on `main` at `90d62ca` the same evening.

## Assumptions

- **Every solution the bridge will ever be asked about is in the map or will be added by the Architect naming its
  key** (152687: "the Architect names the key"). The bridge suggests; it never decides.
- **The five live solutions keep their `repo_root`** through this feature; a repository that moves heals on its next
  hand run, as 004 assumed.
- **The extract log is readable by the serving process** (same directory as the executable, the file it appends to);
  Q1 depends on it and FR-419 says what happens when it is not.
- **The extractor's contract is Stage A's as amended by 002–004**, plus what this feature adds: `warnings=N` on the
  summary line, `.slnx` as an input, schema version 3. The bridge relies on nothing else about it.
- **A design-time build of RicksLife and DSP_Processor completes on this machine** without environment help once
  FR-427 lands; if a suppression proves necessary in the property dictionary, the plan says which and why.
- **The NU1701 and NU1101 fixtures stay offline**: the .NET Framework-only package the first references ships as a
  local package source checked in beside the fixtures, and a `NuGet.config` beside them names that source alone —
  so the second's missing package is NU1101 ("unable to find") from the local source, never NU1301 (a network
  failure) — and the suite needs no network (Article III's real fixture, Article XV's no foreign runtime).
- **What the MemOS Shell's reader does with a version-3 map is MemOS's concern** (ruled at clarify, 2026-09-16):
  this feature's plan does not carry it, and nothing in MemOS's repository is changed (FR-436).
- **`git` and the LibGit2Sharp the bridge already carries keep answering** for the repository facts `map_status`
  reports; nothing about that mechanism changes.
- **The Operator's steps outside this repository** — deleting DSP_Processor's committed `.sln`, re-pointing the two
  registry rows' `extraction_scope` in MemOS, removing `storePath` from the config beside the Release executable —
  are named in the process document and done by hand; none is performed by this feature's code.

# Feature Specification: CodeMem 004 — The Bridge: Claude Code's Source for the Map, and the Green-Compile Trigger

**Feature Branch**: `004-codemem-bridge`

**Created**: 2026-09-15

**Status**: Specified and clarified 2026-09-15 (five rulings, session below); spike run the same day, both facts
proven ([spike.md](spike.md)); plan written ([plan.md](plan.md)); **STOP 1 ruled 2026-09-15** (all eleven as
proposed; constitution amended to v1.3.0). Next: `/speckit-tasks`, then implement.

**Input**: User description: "CodeMem 004: the bridge. PM: task 131384 and children 152646–152648 — read their
details. Decisions 142362, 137077, 132115, 152658 are binding. Parent: 003, shipped. Build one process,
CodeMem.Bridge … Serve eight tools … Wire the green-compile trigger … Presentation rule … Invariants as tests … Do
not … Ceremony: STANDARD, one spike."

**Governing document**: `.specify/memory/constitution.md` v1.3.0 (amended at STOP 1, 2026-09-15: Article IX
gains the ruled exception for the registry read; the connection-site gate names two files). Where this
specification and the constitution appear to differ, the constitution wins and the difference is a defect in
this document. Article IX is the article this feature is built around: the bridge never writes the map; the
extractor is the sole writer.

**Parent features**: `specs/001-extractor-codemem-sqlite/` (Stage A), `specs/002-stage-a-fixpack/` and
`specs/003-extractor-fixpack/` (shipped, commit `f648846`, live map runs 4–6). Requirements here are numbered
FR-301 onward so they never collide with FR-001–FR-039, FR-101–FR-123 or FR-201–FR-219. This feature changes no
extractor behaviour; the extractor is invoked as a black box.

**PM**: task 131384 (Stage B — the CodeMem bridge; parent project CodeMem 131373) and its three children, all read
from the PM store on 2026-09-15 and quoted where they bind: 152646 (finding 1, `map_status`), 152647 (finding 2,
`type_usages`), 152648 (finding 3, twin symbols from linked source). Binding decisions, read the same day:
142362 (the bridge is Claude Code's source; green compile is its trigger; §3a two readers, no dependency),
137077 (serve and extract in one surface; the extract verb is permissive-gated; duplicate the readers, pin the
schema), 132115 (rule of five and the permissive gate; the ceremony gauge; every spec opens with a Mission), and
152658 (the four open calls ruled: stdio; a Claude Code PostToolUse hook; two gates, both off, dependent; the
trigger refreshes the built solution only, `extract --stale` is manual).

**Ceremony**: STANDARD, one spike (description). The silent failure this feature guards against is a map that is
behind the tree and answers confidently (137077 §Staleness); the loud ones — a refused open, a failed child process
— need no elevation. The spike covers the two facts the design rests on that nobody in this repository has
verified (§Spike).

## Mission

Claude Code, from any session on this machine and with the MemOS Shell closed, asks one process for what the
map knows — where a symbol is declared, what it is made of, everywhere it is used, who uses a type, and whether
the map is current — and a green build refreshes the map of the solution that just compiled, through that same
process, without anyone noticing or remembering.

**Out**: writing the map (the extractor alone does that); a second reader shared with MemOS; a network host; the
second-pass tools (`rename_candidates`, `unregistered`, `repo_drift`, `external_usage`, `spec_conformance`); a
schema change; installing anything into the user's Claude Code settings; refreshing more than the solution that
was built.

## Clarifications

### Session 2026-09-15 (before planning)

The description settles most of the design. The decisions below are the ones it leaves open, each answered with
its reasoning and its alternative so nothing is decided silently; every one is a proposal until the Architect
rules at STOP 1. Q5 and Q6 are also the subject of the spike, which may replace them with what was observed.

- Q1: Where does the bridge get the **registered repo root** and the **solution path** it needs, given that it
  reads `memos.sqlite` for `code_map_solutions` only, and that table has no path column but `extraction_scope`
  (anchored: `{repo}/MemOS.sln` on the MemOS row; absolute on the other two by the 2026-09-13 carve-out)? → A:
  **From the map, keyed by the registry's binding.** *Ruled A at clarify, 2026-09-15 (session below).* A
  registry row whose `codemem_solution_id` is set names one
  `solutions` row in the map, and that row carries `repo_root` (the working directory the extractor scoped the
  last run to, 003 FR-201) and `last_seen_path` (the `.sln` or `.vbproj` it was pointed at). The registry supplies
  identity and binding (`solution_key`, `project_id`, `codemem_solution_id`, `state`); the map supplies the two
  paths. Reasons: (a) the registry deliberately has no repo-root column — migration 029 records that the map's
  `repo_root` "is something to COMPARE AGAINST", which is exactly `map_status`'s question; (b) an anchored
  `extraction_scope` cannot be resolved without MemOS's anchor configuration, and the repo-role rows of
  `project_paths` are a second store table plus a vocabulary domain, both outside "the registry only"; (c) the
  extractor refreshes both map columns on every run, so a repository that moves heals itself on the next hand run.
  Consequences, stated: a bound row whose solution the map lacks is reported as **map missing solution** and
  refused by `extract`; a registry row with no binding (`codemem_solution_id` NULL) is listed by `map_status` as
  **unbound**, not given a verdict, and is **not extractable through the bridge in this pass** — the first
  extraction of a new solution stays the Operator's hand run with an explicit `--solution-key`, and the binding is
  MemOS's write (061's bind door), as today. On the live registry every row is bound (MemOS 3, GameRoom 1,
  CodeMem 2), so nothing reachable today is lost. Alternatives: `project_paths` role `repo` (a second table and
  the `project_path_role` domain; task 131411 will repoint the anchors under it); `extraction_scope` when
  absolute (two sources for one path, and the MemOS row is anchored).
- Q2: How does the twin-symbol presentation (152648) apply to `symbol_detail`, whose input is one id? → A: **The
  requested id's own parts and edges, under a header that names every twin.** Each twin is its own compilation
  and has its own edges (the contract-tests copy's outbound edges point at contract-tests symbols); a union would
  double-count and could not be attributed. So `symbol_detail(3556)` presents the declaration "compiled into 2
  projects" with both ids and both declaring projects, then 3556's parts and edges, and the caller may ask for
  4200 by id. In `symbol_search`, one presented declaration per (kind, name, path, line) within a solution,
  carrying its per-project ids; `total` counts presented declarations; the 200-row cap applies to presented
  declarations. The fold never crosses solutions (paths are solution-relative). Alternative: union the twins'
  edges in detail (rejected: double counting, unattributable rows).
- Q3: `type_usages` — which symbols may it be asked about, what exactly is unioned, and are uses from inside the
  type counted? → A: **Type kinds only; the union the finding names, one containment level deep; inside uses
  flagged and counted separately.** *Ruled A (the inside/outside split) at clarify, 2026-09-15; the kind
  restriction and the one-level rule stand as proposed.* The tool accepts an active symbol of kind `class`,
  `module`, `structure`, `interface`, `enum` or `delegate` and refuses any other kind by name with the remedy
  "use `references`" (a
  namespace or project would union its whole subtree; a member has no members). The union is: every non-`part_of`
  edge whose target is the type; every `calls` edge whose target is one of its constructors; every `calls` or
  `uses` edge whose target is one of its direct members (the active symbols whose container is the type, which is
  what inbound `part_of` lists — nested types count as members, their members do not); every `implements` or
  `extends` edge whose target is the type. Each occurrence names the symbol it actually targets (the type, a
  constructor or a member — by id, kind and name), because the identity rule forbids presenting a constructor
  call as a reference to the type. An occurrence whose source lies inside the type (the source is the type, a
  member, or contained by one) is flagged `fromInside`; the result reports `total` and `fromOutside`, so "is this
  class used anywhere" is one number that a member calling a sibling does not inflate. Measured on the live map
  2026-09-15 for `CodeMemMapReader` (MemOS symbol 3023): 0 references to the type, 16 constructor calls (15 from
  `MemOS.ContractTests`, 1 from `MemOS.Modules.Mcp`), 4 member calls, all 4 from inside (`New` and `Read` reading
  the two fields), 0 implements/extends — `total` 20, `fromOutside` 16. Alternative: count everything with no
  inside/outside split (rejected: it answers a different question from the one the finding asks).
- Q4: `map_status` — how are the verdicts ordered when more than one fact holds, and what is said when the
  recorded commit is not an ancestor of HEAD? → A: **One verdict per solution, chosen in this order: map missing
  solution → no git → dirty tree → behind by N → current; every fact is reported regardless.** A dirty tree is
  reported as `dirty` even when HEAD has also moved (the commit count is still reported beside it), because
  `extract --stale` treats both as stale and the tree on disk is what the next run would map. "No git" covers
  both the repository the bridge cannot read now (no `.git`, no `git` answer, an unreadable root) and a run that
  recorded no commit (`commit_sha` NULL — the extractor found no repository with a commit at extraction time);
  the reason is named in each case. When the recorded commit exists in the repository but is not an ancestor of
  HEAD (a branch switch, a rebase), the count cannot be taken without guessing: the verdict is **`diverged`**, a
  sixth name, with both shas named. *Ruled A at clarify, 2026-09-15.* Alternative: report it as `behind` with
  the count absent (rejected: "behind" claims a direction the facts do not support; the description says never
  guess).
- Q5: How does the hook "call the bridge", and how does the bridge tell a hook-triggered `extract` from a direct
  one so that the second gate applies only to the trigger? → A: **The bridge executable has a one-shot
  command-line entry that walks the same extract door as the MCP tool, declaring its origin.** Claude Code
  launches the bridge as the stdio server (`serve`, the default); the hook runs the same executable once with
  `extract --repo-path <dir> --on-green-build`, which resolves, gates and launches exactly as the tool does and
  prints the same result; a human runs `extract --stale` from the same entry (origin manual). The three origins —
  tool, green-build, manual — reach one door (Article XII); only green-build is subject to `extract.onGreenBuild`.
  The hook therefore never launches the extractor, never opens either file, and needs no session-side MCP call.
  Alternatives: (b) a hook of the documented type `mcp_tool`, which calls a registered server's tool directly
  with templated input and no subprocess — then the bridge's `extract` would receive the build command and its
  outcome as arguments and do the matching itself, and a `trigger` argument on the public tool would let a direct
  caller claim the hook's origin; (c) the hook opens a second stdio session to the bridge and sends a
  `tools/call` (heavier, same objection as (b)). Two documented facts bear on all three (read 2026-09-15 from
  the Claude Code hooks reference): `PostToolUse` fires only after a tool succeeds and a separate event fires on
  failure, so "exited 0" may be implied by the event firing at all rather than read from a field; and the
  contents of `tool_response` for Bash are not documented. **The spike decides between (a) and (b) on evidence
  (§Spike).**
- Q6: What is "the build's working directory" the hook passes as `repoPath`? → A: **The directory of the `.sln`
  or `.vbproj` named in the build command when one is named; otherwise the directory the command ran in.** A
  session in one repository can build another by path (`dotnet build C:\…\GameRoom\GameRoom.sln`), and the built
  solution — the only thing the trigger may refresh (152658 ruling 4) — is the one the path names. *Ruled A at
  clarify, 2026-09-15, with the addition that a named path resolving to no registered root is refused naming
  that path; the session directory is never a second guess.* Alternative: the session directory only (rejected:
  it would refresh the wrong solution for a build by path, or refuse it).
- Q7 (not asked; decided): What does "the registry only" do to the ported refusal vocabulary? → A: **Two kinds are
  not ported and one reason changes.** MemOS's `ProjectUnknown` consults the `projects` table, which the bridge
  does not read: a `projectId` with no registry row is not an error — the result says it searched nothing and
  why, and the reason is "no registry row names project N" (the bridge cannot tell an unknown project from an
  unregistered one, and says so). MemOS's audit row (`sql_log`, `source = mcp-codemem`) is a write to the store,
  so it is not ported: the bridge writes nothing anywhere, and this is the one deliberate difference in the
  shapes. `RegistryAbsent` (the table does not exist) is ported, naming the store path.
- Q8 (not asked; decided): Configuration. → A: **One file, `bridge.config.json`, beside the executable, read on
  every call and never cached** (056's rule for the map path: a value changed between two calls is seen by the
  second). Keys: `mapPath` (the map file), `storePath` (`memos.sqlite`), `extractorPath` (optional; default the
  extractor executable built beside the bridge), `extract.enabled` and `extract.onGreenBuild` (both default
  `false`; absent means off). A missing file, or a missing `mapPath` or `storePath`, is refused by name naming
  the key, on every tool that needs it; a missing `extractorPath` is refused by `extract` only. No default path
  is ever invented and nothing is created.
- Q9 (not asked; decided): The schema-version pin and what "another version" refuses. → A: **Pinned at exactly 2,
  with two remedies.** Below (a Stage A map at 1): refused naming the version and the remedy — run the extractor
  once, it upgrades in place; the bridge, read-only, cannot. Above: refused naming the version and the remedy —
  revise the bridge against the newer contract. A file that is absent, not SQLite, or SQLite without a map
  identity is refused by its own name; nothing is created (a read-only open of a missing path fails, it does not
  create a 0-byte file, and that is asserted).
- Q10 (not asked; decided): Where the live-map acceptance runs. → A: **As 003 did: on a copy first, then on the
  live map only at the Architect's request, after a backup.** The invariant "after a green extract MemOS it
  reports current" is a run of the extractor against MemOS at its current HEAD (`cbeb661`, 11 commits past run
  6's `806f5f3`); it writes run 7 to whichever map it is pointed at. The bridge tests in the suite use fixture
  maps and a copy; the live figures are recorded in the quickstart.
- Q11 (not asked; decided): What "documented, not auto-installed" ships, and where. → A: **Three files in this
  repository and nothing outside it.** `.mcp.json` at the repository root (the ruling's "one `.mcp.json` entry";
  Claude Code reads it for sessions in this repository and asks the user before using a project server, which is
  the approval step, not an installation); a settings fragment under the bridge project (`hooks/` beside a short
  document) holding the PostToolUse entry with a placeholder for the bridge's path, to be merged by hand into the
  user's own `settings.json` — user scope, because a build of GameRoom or MemOS happens in that repository's
  session, where this repository's project settings are not loaded; and the process document, which names the
  install steps (the `.mcp.json` approval, the user-scope server registration for other repositories, the
  fragment, the config file and its two gates) and **when each tool is called**: `map_status` at session start
  and before any `references`, `orphans` or `type_usages` answer is relied on; `type_usages` before any delete or
  rename; `extract` by the hook, never by Claude Code directly; `extract --stale` by a human (152658 §What
  specify starts from; 137077 §Governance).

### Session 2026-09-15 — clarify (Architect)

- Q: Where should the bridge get the registered repo root and the solution path it needs for `map_status` and
  `extract`? → A: **From the map — `solutions.repo_root` and `solutions.last_seen_path` of the solution the
  registry row binds to (Q1, option A).** The map's `repo_root` is the root the extractor actually scoped, so
  `map_status` compares HEAD against the tree the run came from, and the bridge's store read stays confined to
  `code_map_solutions`. A per-solution path list in the bridge's own configuration would put a second
  hand-maintained copy of paths next to the registry — the anchor problem task 112650 exists to remove, not add
  to; the `project_paths` and `extraction_scope` routes both drag anchor resolution into the bridge, and the
  MemOS row's `{repo}` scope shows that is not solvable there today. Consequence stands: an unbound registry row
  is listed, not extractable through the bridge.
- Q: When the commit a run recorded exists in the repository but is not an ancestor of the current HEAD, what
  should `map_status` say? → A: **A sixth verdict, `diverged`, naming both shas and claiming no count; `extract
  --stale` treats it as stale (Q4, option A).** The five names were the Architect's and missed the branch-switch
  case; a name that states the fact is the honest fix. Calling it `behind` — with or without a merge-base count —
  claims a direction the history does not support; calling it `no git` hides the one signal wanted after a
  rebase, since the repository is readable and merely disagrees with the run.
- Q: Should `type_usages` count a use that comes from inside the type itself separately from uses that come from
  outside it? → A: **Return every occurrence, flag each with `fromInside`, report both `total` and `fromOutside`
  (Q3, option A).** The tool exists so an agent gets the right number before a delete or rename, and that number
  is `fromOutside`; hiding the inside uses throws away evidence the same agent needs a minute later when editing
  the class, and no split makes it re-derive the answer from source paths — the two-call problem the tool was
  filed to remove. It lines up with how `orphans` already reasons about reach.
- Q: When the hook fires on a green build, which directory should it pass to `extract` as `repoPath`: the
  directory of the `.sln` or `.vbproj` named in the build command, or the directory the command ran in? → A:
  **The directory of the file named in the command when one is named; otherwise the directory the command ran
  in (Q6, option A).** "The built solution only" means the one that compiled, and when the command names a
  path that is the answer regardless of where the shell sat; the working directory is what `dotnet build` itself
  falls back to when nothing is named, so the rule mirrors the tool's own. The session directory alone would
  refresh the wrong map on a by-path build (MemOS's tree building CodeMem would re-extract MemOS). Trying the
  session directory after a named path fails is a silent second guess: a named path that resolves to no
  registered root is a refusal naming that path, never a reason to extract whatever the session happens to be in.
- Q: Should the bridge keep any record on disk of its `extract` invocations, given that FR-308 said it writes
  nothing at all? → A: **Yes: an append-only text log beside the executable, always on, for the extract path
  only, one line per invocation; reads are never logged (option B).** "Writes nothing" was about the two
  databases — Article IX and the two-readers rule — not about a log beside the executable, and the trigger is the
  path this feature is judged on. The valve outage in 132133 is the precedent: five weeks silent because nothing
  recorded the failures; a default-off log reproduces that exactly — the first time it is needed, it was not on.
  FR-308 now says "writes neither database"; the log is FR-351.

### Session 2026-09-15 — STOP 1 rulings (Architect)

- **All eleven plan decisions as proposed** (plan §STOP 1). (1) Article IX amended, v1.3.0, with research R55's
  wording as written; the connection-site gate becomes exactly two named production files, `MapDatabase.vb` and
  `StoreDatabase.vb`, with its fire; store code may proceed once the amendment is in — it is in
  (`.specify/memory/constitution.md` v1.3.0, 2026-09-15). (2) Gates evaluated before target resolution; a
  disabled verb reveals nothing about the registry. (3) The hook fragment as proposed — `PostToolUse`, matcher
  `Bash`, the bridge executable as the command, the `hook` entry reading stdin, exit 0 always, no
  `PostToolUseFailure`; the README records that the `mcp_tool` hook type produced no observable effect in the
  spike, so it is not retried. (4–11) Repo-relative `.mcp.json`; `--config` on every entry; the Extractor project
  reference; LibGit2Sharp with the tree-wide dirty check; the twin fold with the header naming the twins and the
  edges belonging to the requested id; `type_usages` on type kinds, one containment level; the registry-only
  vocabulary; live acceptance on a copy first. Q2, Q5, Q7–Q11 are thereby ruled as proposed.
- **Accepted, not ruled**: research R46's dirty definition differs from the run's `is_dirty` — whole working
  tree versus compiled inputs — so a README edit reads `dirty` in `map_status` and clean in the run. Accepted for
  this feature as stated. Narrowing to source files stays available as a later ruling; it is not built now.

### Session 2026-09-15 — review rulings after analyze (Architect)

- **CON1** — two projects, one process: `CodeMem.Bridge` (exe: `Program.vb`, the command line, the MCP host wiring)
  and `CodeMem.Bridging` (class library: readers, status, scope, twin fold, configuration, refusals, the extract
  door), the Extractor/Extraction shape; Article I passes without a justification (FR-301).
- **CON2** — no implementation slice lands before its behaviour test exists and is observed Red; the shared Core
  reads and the bridge foundation land behind B02's Red; a skeleton that fails to compile is a Red only once the
  fact it will satisfy is written (tasks, Phases 2–3).
- **CON3** — B01 calls all seven readers through the executable over stdio, and `extract` the same way for one gate
  refusal and one real launch; the in-process host serves fast facts, never the production-route evidence (FR-342).
- **CON4** — one open, one read transaction per tool call, ended after serialisation, with the commit-straddling
  fact (FR-303).
- **COR1** — `SolutionScope.ContainsDirectory` (equality or prefix, both normalised as directories) for the bridge's
  target resolution; `Contains` keeps its file semantics for the extractor; exact-root facts in both places (FR-326).
- **COR2** — positional targets parsed after options; more than one candidate is ambiguous, never a fallback;
  option-before-path, quoted and cross-repository paths tested (FR-335).
- **INC1** — the registry is queried directly and "no such table" becomes `RegistryAbsent`; no `sqlite_master`
  (FR-304). **INC2** — cardinality before configuration (FR-315). **INC3** — the log beside the executable; `--stale`
  writes one line per solution launched, no header (FR-329, FR-351). **INC4** — `solutions` reports the latest run of
  any outcome, `map_status` the latest completed run (FR-309).
- **DUP1** — `type_usages` deduplicates by edge id and a fact asserts every fixture edge appears once (FR-316).
- **COR3** — raw rows preserved; `TwinFolder` folds only groups whose declaring projects are all distinct; the
  same-project shape is seeded and asserted as two declarations (FR-340).
- **TIM1** — a 540 s child budget inside the 600 s hook, with a scripted timeout fact (FR-328).
- **Branch** — `004-codemem-bridge` from `b2e0168`; the v1.3.0 amendment and the 004 artifacts committed there
  before the first Red.

## User Scenarios & Testing *(mandatory)*

Actors: **Claude Code**, the agent that registers the bridge and asks it questions; the **Operator** (the
Architect at this desk), who flips the two gates, installs the fragment and runs the extractor by hand when the
bridge refuses; the **Architect**, who rules at STOP 1 and reads the evidence. The MemOS Shell is not an actor:
the bridge is available while the Shell is closed, and the Shell's own `codemem_*` tools are untouched.

### User Story 1 - Claude Code asks the map while the Shell is closed (Priority: P1)

Claude Code, in a session in this repository, has the bridge listed as a server from `.mcp.json`. It calls
`solutions` and sees the three mapped solutions with their latest runs; `symbol_search` for a name; `symbol_detail`
for an id; `references` for everywhere a symbol is used, by identity; `orphans` for what nothing reaches. Each
answer has the shape the MemOS-hosted tool of the same name returns today, carries the same caveats in its
description, and refuses by name in the same situations — an unconfigured or absent map, a map at another schema
version, a symbol that is retired or out of scope. The Shell is closed the whole time. The map file is byte-for-byte
what it was before the first call.

**Why this priority**: this is the ruling of 142362 — the bridge is Claude Code's source — and the reason the
topology exists: four incidents of work stopping to close or reopen the Shell because the tools were hosted by the
process that holds the build output (task 131396). Every other story builds on the bridge being there.

**Independent Test**: Build the bridge; point its config at a copy of the live map and the live store; start a
Claude Code session in `repos\CodeMem`; approve the project server; call each of the five tools once against
`GameRoom` and once through `projectId 132040`; hash the map before and after. Refusal cases are driven in the
suite against fixture maps (absent, foreign, version 1, a hand-bumped version 3, a retired symbol).

**Acceptance Scenarios**:

1. **Given** a configured map at schema version 2 and the live registry, **When** Claude Code calls `solutions`,
   **Then** it receives the map identity and every solution ordered by key, each with its latest run's outcome,
   commit, dirty state (unknown exactly when there is no commit), SDK version and ten counts, in the 056 shape.
2. **Given** the same, **When** `symbol_search(solutionKey "GameRoom", name "Draw")` is called, **Then** active
   symbols only are returned with id, doc-comment id, kind, name, container, declaring project, path and line, at
   most 200 rows, with `total` and `truncated` always present; and the same call with `projectId 132040` resolves
   through the registry to solution 1 and returns the same rows with the registry's four counts in `scope`.
3. **Given** a symbol id, **When** `symbol_detail` is called with exactly one of `projectId` or `solutionKey`,
   **Then** the result carries the symbol, every declaring part (the one containing the name flagged), and its
   outbound and inbound edges under all eight verbs, an external target marked external with its doc-comment id.
4. **Given** a symbol id, **When** `references` is called, **Then** every occurrence whose compiler-resolved target
   is that symbol is returned uncapped with path, line, column, verb and source symbol; `part_of` is never among
   them; the description states the `AddHandler … AddressOf` gap, the bare-name rule of 003, and the external
   bare-name limit, by mechanism.
5. **Given** a scope, **When** `orphans` is called, **Then** the 059/060 result is returned: `notExamined`,
   `total`, `byProject` for every active project in scope, `byKind` for every examined kind with zeros, and every
   orphan uncapped; the description carries every mechanism phrase 060 registered.
6. **Given** a map path that is absent, not a map, at version 1, or at a version above 2, **When** any tool is
   called, **Then** it is refused by name with the remedy that fits, nothing is created, and the path is not
   touched.
7. **Given** a `symbolId` that is retired, or that belongs to a solution outside the requested scope, or that does
   not exist, **When** `symbol_detail`, `references` or `type_usages` is called, **Then** each is refused by its
   own name and remedy, as 058 §5 does.
8. **Given** any sequence of calls to the seven read tools, **When** the sequence completes, **Then** the SHA-256
   of the map file equals its value before the first call, and no `-journal` or `-wal` file has appeared beside
   it.

---

### User Story 2 - "Who uses this class" is one call (Priority: P2)

Claude Code is about to delete or rename a class. It asks `type_usages` for the class's id and gets the union
of everything the map records against it: references to the type itself, calls to each constructor, calls and
uses of each member, and inbound implements and extends — grouped by verb with counts, then the flat list of
occurrences, each naming what it actually targets and whether it comes from inside the type. `references` still
answers the narrow question it always did.

**Why this priority**: finding 152647 — `references(CodeMemMapReader)` returns 0 while its constructor has 16
callers. The model is right (`New X(…)` targets the constructor) and the agent is wrong-footed; 152658 predicts
this is the most-called tool from VS Code. It serves the mission's "who uses a type" directly.

**Independent Test**: On the live MemOS map, `type_usages(3023)` against `references(3023)`; on the fixture map, a
type with a constructor call, a member call from another type, a member call from inside, and an `Implements`,
each counted where expected.

**Acceptance Scenarios**:

1. **Given** MemOS symbol 3023 (`CodeMemMapReader`, class), **When** `type_usages(3023)` is called, **Then** at
   least 16 occurrences are `calls` whose target is its constructor (6661), `references(3023)` on the same map
   returns 0, and each of the 16 names the constructor as its target, not the type.
2. **Given** a type whose member is called from another type and from a sibling member, **When** `type_usages` is
   called, **Then** both occurrences appear, the sibling's is flagged `fromInside`, and `fromOutside` counts only
   the first.
3. **Given** a type that another type inherits or implements, **When** `type_usages` is called, **Then** the
   `extends` or `implements` occurrence appears under its verb with the source named.
4. **Given** a symbol of kind method, field, property, namespace or project, **When** `type_usages` is called,
   **Then** it is refused by name with the remedy "use `references`".
5. **Given** the verb groups, **When** any result is returned, **Then** all seven non-`part_of` verbs are listed
   with their counts, zeros included, and `total` equals the length of the occurrence list.

---

### User Story 3 - The map says whether it is current (Priority: P2)

At session start, Claude Code calls `map_status` and reads, for every bound solution, the commit the latest
completed run recorded, what HEAD is now in that repository, whether the tree is dirty, and one verdict by name:
current, behind by N (HEAD named), dirty tree, no git, or map missing solution. On this desk, today: MemOS is
behind by 11 with HEAD `cbeb661`; GameRoom's HEAD equals its run's commit but the tree is dirty; CodeMem is
behind by 1. Nothing is guessed: a root the bridge cannot read is reported as such.

**Why this priority**: finding 152646 — the map went stale at the 061 merge and nothing said so; `solutions`
reports the dirty state *at extraction*, and no reader compared the recorded commit with the live HEAD. This is the
detection half of 131384's staleness paragraph; the extract verb is the remedy half. The mission's "whether the map
is current" is this tool.

**Independent Test**: On the live map and the three repositories as they stand; then on a fixture: a copy of the
fixture solution inside a repository with one commit, extracted, then one more commit (behind by 1), then a
modified file (dirty), then the `.git` directory removed (no git), then a registry row bound to a solution id the
fixture map lacks (map missing solution).

**Acceptance Scenarios**:

1. **Given** the live map and registry on 2026-09-15, **When** `map_status()` is called, **Then** the MemOS entry
   reports run 6, recorded commit `806f5f3f…`, HEAD `cbeb6615…`, verdict `behind` with the count of commits between
   them (11 at the time of writing; ≥ 1 at test time), and the GameRoom entry reports verdict `dirty` with HEAD
   equal to its recorded commit.
2. **Given** a green extraction of MemOS at HEAD has completed (US4), **When** `map_status()` is called again,
   **Then** the MemOS entry reports run 7, recorded commit equal to HEAD, and verdict `current`.
3. **Given** a bound solution whose repository root has no `.git` directory, or whose root does not exist, **When**
   `map_status()` is called, **Then** the entry's verdict is `no git` with the reason stated, and no other entry is
   affected.
4. **Given** a registry row bound to a solution id the map does not hold, **When** `map_status()` is called,
   **Then** the entry's verdict is `map missing solution`, naming the key and the id.
5. **Given** a registry row with no binding, or with state other than active, **When** `map_status()` is called,
   **Then** it is listed under unbound or inactive rows with no verdict, and the counts of bound, unbound and
   inactive rows are present.
6. **Given** a recorded commit that is not an ancestor of HEAD, **When** `map_status()` is called, **Then** the
   verdict is `diverged` with both shas named and no count claimed (Q4, ruled 2026-09-15).

---

### User Story 4 - A green build refreshes the built solution, through the bridge, under two gates (Priority: P3)

The Operator has merged the shipped fragment into their Claude Code settings and turned both gates on. Claude Code
runs `dotnet build` in a registered repository and it succeeds. The hook fires, resolves the build's directory to a
registered root through the bridge, and the bridge runs the extractor as a child process with the registry's key;
the run's exit code, its one summary line and the new run's ten counts come back into the session. A build in an
unregistered directory is refused by name, naming the path, and nothing is launched. With either gate off, the
refusal names the gate that stopped it. A human who wants every stale solution refreshed runs `extract --stale`.

**Why this priority**: the second ruling of 142362 and the whole of 152658. It closes the loop 137077 named: a
tool that finds its map behind the tree can now refresh it inside the surface instead of a line in a runbook. It is
P3 because it is the only path that changes state and is gated off by default — the read surface stands without
it.

**Independent Test**: The gate matrix in the suite against a fixture map and a fixture registry (a throwaway
store holding only `code_map_solutions`): both off; first on only, hook origin and tool origin; both on. The
resolution cases: a `repoPath` under a registered root, under no root, under two nested roots. The child-process
contract: exit codes 0, 1, 2 and 4 from the real extractor, each with what the bridge returns. The hook end to end
is the spike's fact 2, repeated as the live acceptance after implementation.

**Acceptance Scenarios**:

1. **Given** `bridge.config.json` absent, or both gates absent or `false`, **When** `extract` is called by any
   origin, **Then** it is refused naming `extract.enabled`, no process is started, and the map is byte-identical.
2. **Given** `extract.enabled` `true` and `extract.onGreenBuild` `false`, **When** the hook path calls `extract`,
   **Then** it is refused naming `extract.onGreenBuild`; **When** the tool is called directly with a `solutionKey`,
   **Then** the extractor runs and the result carries its exit code, its summary line and the run's ten counts.
3. **Given** `extract.enabled` `false` and `extract.onGreenBuild` `true`, **When** the hook path calls `extract`,
   **Then** it is refused naming `extract.enabled` (the second gate is inert while the first is off).
4. **Given** both gates `true`, **When** the hook observes a `dotnet build` or `dotnet test` that exited 0 in a
   directory under a registered root, **Then** `extract` runs for that one solution with `--solution-key` equal to
   the registry's key, `--solution` equal to the map's last-seen path and `--db` equal to the configured map, and
   the outcome is visible in the session that ran the build.
5. **Given** a `repoPath` under no registered root, **When** `extract` is called, **Then** it is refused naming the
   path, no process is started, and the map is byte-identical.
6. **Given** the extractor exits 2 (compile errors), 3 (lock held) or 4 (residual mismatch), **When** `extract`
   returns, **Then** the exit code and the one stderr or summary line are returned verbatim, the ten counts are
   present only when a completed run was published, and nothing is retried.
7. **Given** two bound solutions reported `behind` or `dirty` and one `current`, **When** `extract(stale: true)` is
   called with `extract.enabled` on, **Then** the two are extracted in sequence, one run each, the current one is
   skipped and listed with its verdict, and the result lists all three.
8. **Given** an `extract` already running through the bridge, **When** a second `extract` is called, **Then** the
   second is refused by name and the first completes; the extractor's own lock (exit 3) is never the first line of
   defence.
9. **Given** the shipped fragment and `.mcp.json`, **When** the process document is followed on a clean machine
   state, **Then** nothing in `~/.claude/settings.json` has been written by this feature, and the user has made
   every edit named.

---

### User Story 5 - One declaration, compiled into N projects (Priority: P4)

Claude Code searches for `CodeMemMapFixture` in MemOS. The map holds two active class symbols for it — one under
`MemOS.ContractTests` (3556) and one under `MemOS.IntegrationTests` (4200) — with one path and one line, because
the file is linked into both projects. The bridge presents one declaration, "compiled into 2 projects", carrying
both ids and both project names. The map is untouched: both rows stand, both ids are addressable.

**Why this priority**: finding 152648 — a tool resolving "the symbol at this file and line" from an editor gets
two ids for one declaration and cannot tell which to use. Presentation only; nothing in the extractor or the schema
changes. Lowest priority because every other story works without it.

**Independent Test**: `symbol_search(solutionKey "MemOS", name "CodeMemMapFixture", kind "class")` on the live
map; on the fixture map, a file linked into both fixture projects (the shape 003's tests already build).

**Acceptance Scenarios**:

1. **Given** MemOS symbols 3556 and 4200, **When** `symbol_search` matches them, **Then** one presented
   declaration is returned with `compiledInto` listing both project names and ids, `total` counts it once, and
   the symbol's kind, name, path and line are those the rows share.
2. **Given** either id, **When** `symbol_detail` is called, **Then** the header names the declaration as compiled
   into 2 projects with both ids, and the parts and edges are the requested id's own (Q2).
3. **Given** two active symbols that share kind, name, path and line under the same declaring project (a partial
   type's parts are one symbol; this shape does not arise from the extractor), or under different solutions,
   **When** searched, **Then** they are not folded.
4. **Given** the live MemOS map, **When** every active symbol is grouped by kind, name, path, line and solution,
   **Then** 222 groups span more than one declaring project (measured 2026-09-15: 6 classes, 6 constructors, 68
   fields, 65 methods, 77 properties), and every one of them presents as one declaration.

---

### Edge Cases

- **The map is under an extraction when a read tool is called**: the extractor holds `BEGIN IMMEDIATE`; a read in
  rollback-journal mode may wait or fail with busy. The bridge waits a bounded time and then refuses by name
  ("an extraction is in progress; retry"), as 056's `Busy` does; it never reports a partial read.
- **A read tool is called while the bridge's own `extract` is running**: reads are per call, open and closed
  inside the call, so the extractor's lock is not blocked by the bridge; a read during the run may see the previous
  completed run or be refused busy, never an intermediate state (Article V's publication is one transaction).
- **The registry table does not exist in the store**: refused by name (`RegistryAbsent`'s shape), naming the store
  path — the window between a MemOS merge and its migration going live; nothing is wrong with the map and the
  refusal says so.
- **The registry is empty**: a legitimate state, not a refusal: `solutions` still lists the map; a `projectId`
  scope searches nothing and says why; `map_status` lists no entries and zero counts; `extract` by `repoPath`
  refuses naming the path.
- **Two registered roots nest** (one repository checked out inside another's tree): `repoPath` resolves to the
  longest root that contains it.
- **The same root is bound twice** (two active registry rows binding solutions whose map rows share a
  `repo_root`, as a `.sln` and a `.vbproj` extracted separately would): `repoPath` is ambiguous; `extract` refuses
  naming both keys and asks for `solutionKey`. Not observable on the live registry today; stated so it is not
  guessed.
- **A registry row's state is not `active`**: the row is not registered for any purpose here; `extract` refuses
  naming the key and its state; `map_status` lists it under inactive.
- **`extract` by `solutionKey` for a key the registry lacks, or the map lacks**: refused naming the key and which
  side lacks it (the registry, or the map — "map missing solution").
- **The extractor executable is missing, or exits without a summary line**: `extract` returns the exit code and
  stderr as observed and names the path it ran; counts absent; nothing inferred.
- **A hook fires for a build that is not `dotnet build` or `dotnet test`** (`dotnet run`, `msbuild`, a build
  inside a script): the hook does nothing and says nothing; only the two verbs, matched at the start of the
  command, trigger.
- **A hook fires for a green build in an unregistered directory** (any `dotnet build` on this machine): the bridge
  refuses naming the path; the hook reports it and exits successfully — the build's session is never failed by the
  trigger.
- **The hook's timeout**: an extraction of MemOS took 31 s on 2026-09-13 (load and compile included); the fragment
  sets a per-hook timeout large enough for the largest registered solution (the documented default is 600 s), and
  the spike measures it. A timeout is reported as such, not as a refusal.
- **A red build**: `PostToolUse` is documented to fire only after a tool succeeds; a `dotnet build` that exits
  non-zero is expected to fire the failure event instead, which the fragment does not subscribe to. The spike
  confirms it; the hook additionally checks the status it can see, so a payload that arrives for a failed build
  never triggers an extraction.
- **`extract(stale: true)` when nothing is stale**: no run; the result lists every bound solution with its verdict
  and says nothing was extracted.
- **`extract(stale: true)` when one run fails**: the sequence continues with the next solution; the result carries
  each outcome; nothing is retried.
- **`type_usages` on a type with hundreds of members** (a large form): the union is uncapped, like `references`;
  the by-verb counts come first so a caller can stop reading.
- **A twin whose partner is retired**: only active symbols are presented; a declaration compiled into one live
  project is presented as one, with the retired id absent (`symbol_detail` of the retired id refuses as 058 does).
- **Symbols that share path and line but differ in kind or name** (a method and its property on one line, `Sub
  New` and the class): never folded — all four fields must match.
- **`git` is not on the path, or the root is a worktree or a submodule**: `map_status` reports what the repository
  answers or that it could not be asked; it never falls back to a guess. Whether the repository is read through the
  `git` executable or the library the extractor already uses is the plan's decision; the observable behaviour is
  the same.
- **A map at version 2 whose `sdk_version` is NULL on an old run**: not an error (056's rule); `solutions` shows
  `null`.
- **The configuration file changes between two calls**: the second call sees it; a gate flipped off mid-session
  refuses the next `extract` by name.
- **The extract log cannot be written** (a read-only install directory, a locked file): the extraction proceeds
  or is refused exactly as it would have been, and the result says the line could not be logged; a log the bridge
  cannot write never turns into a silent skip.

## Requirements *(mandatory)*

### Functional Requirements

**The process and its boundaries (task 131384; 142362; 152658 ruling 1)**

- **FR-301**: Two projects MUST exist in `CodeMem.sln` (CON1, the Extractor/Extraction shape): `CodeMem.Bridge`, a
  console executable under `src/CodeMem.Bridge/` holding the entry point, the command line and the MCP host wiring
  only, speaking the Model Context Protocol over standard input and output and nothing else — no port, no HTTP
  host, no service; and `CodeMem.Bridging`, a class library under `src/CodeMem.Bridging/` holding the readers, the
  status and scope logic, the twin fold, configuration, the refusal vocabulary and the extract door. One runtime
  process; the executable only wires (Article I).
- **FR-302**: The bridge MAY reference `CodeMem.Core` and other CodeMem-internal projects. It MUST NOT carry a
  `ProjectReference`, `PackageReference` or linked `Compile` item that points at anything under `rchaudio-a11y\MemOS`,
  and it MUST NOT contain source copied from that repository (142362 §3a: two readers, two hand edits per schema
  change).
- **FR-303**: The bridge MUST open the map in read-only mode on every call (`Mode=ReadOnly`, the driver's
  read-only open that refuses writes and never creates a file), for the duration of that call only, and MUST
  never hold a map connection across calls or while an extraction it launched is running. Every SQL statement the
  bridge issues against the map lives in a named `CodeMem.Core` repository method (Article XI); neither bridge
  project holds a SQL literal, and a gate test asserts both that and that no INSERT, UPDATE, DELETE, DROP, CREATE,
  ALTER, ATTACH or PRAGMA that writes appears in those methods. A read tool opens the map once per call and reads
  inside one read transaction that ends after the response is serialised, so a publication between two of a
  tool's reads never yields a mixed response (CON4).
- **FR-304**: The bridge MUST open `memos.sqlite` read-only and read `code_map_solutions` only: no other table,
  view or PRAGMA of the store, and no write of any kind — no audit row (Q7). The absence of the table is learned
  from the query itself ("no such table" becomes the registry-absent refusal), never from `sqlite_master` (INC1). A
  gate test asserts every store statement names `code_map_solutions` and no other table.
- **FR-305**: The bridge MUST pin the map schema version at exactly 2 and refuse a map at any other version at
  open, by name, with the remedy that fits the direction (Q9). The pin is the bridge's own constant, not
  `CodeMem.Core`'s current version by reference — so a Core bump that moves the extractor to version 3 turns the
  bridge's refusal on rather than silently widening it (137077 §The condition that makes duplication safe).
- **FR-306**: The bridge MUST have its own refusal vocabulary: one named kind per distinct remedy, text only, never
  a thrown exception on the wire, each text naming the file or key it concerns. The kinds cover at least:
  unconfigured (which key), map absent, not a map, version below, version above, busy, unopenable, registry
  absent, scope missing, scope conflict, filter missing, kind unknown, symbol id missing, solution key unknown,
  symbol not found, symbol out of scope, symbol retired, kind not examined, not a project row, not a type (for
  `type_usages`), gate off (which gate), path not registered (which path), key not registered, map missing
  solution, ambiguous root, ambiguous target (a build command naming more than one solution or project), extraction
  already running, extractor not found, child timed out.
- **FR-307**: The bridge MUST read `bridge.config.json` beside its executable on every call (Q8) and MUST NOT
  invent a default for `mapPath` or `storePath`; a missing key is a refusal naming the key.
- **FR-308**: The bridge MUST write neither database in any mode — no fact row, no audit row, no lock, no
  upgrade — and no cache or lock file of its own. Its only writes anywhere are the extract log of FR-351 and the
  extractor process it launches under FR-325 (ruled at clarify, 2026-09-15).

**The five ported readers (056–060 shapes; 142362 §3a "written fresh")**

- **FR-309**: The bridge MUST serve `solutions`, `symbol_search`, `symbol_detail`, `references` and `orphans`,
  written fresh, whose parameters and result shapes equal those the MemOS-hosted `codemem_*` tools return today:
  056 §1.1 for `solutions`; 058 §3.1–3.4 for search, detail and references; 059 §3.1 as amended by 060 §3.1 for
  orphans. Every documented property is always present, `null` included. The two differences are Q7's: no audit
  row (FR-304) and no `ProjectUnknown`. `solutions` reports each solution's latest run of any outcome, as 056
  does; `map_status` and `extract` read the latest completed run (INC4).
- **FR-310**: Scope rules are the same: `symbol_search`, `symbol_detail`, `references` and `orphans` require exactly
  one of `projectId` (resolved through the registry to every active, bound solution of that project) or
  `solutionKey` (one map solution, exact); neither or both is refused by name. A `projectId` that resolves to no
  solution is not an error: the result says it searched nothing and why (Q7).
- **FR-311**: The identity rule is the same: every tool takes a mapped symbol id only; a doc-comment id is a label
  in results and never an input; an occurrence's target is the compiler's resolution, never a text match.
- **FR-312**: Each ported tool's description MUST carry the caveats the MemOS description carries, by mechanism:
  for `references`, that `part_of` is never included, the `AddHandler … AddressOf` gap, the bare-name occurrences
  of CodeMem 003, and the unrecorded external bare name; for `orphans`, every mechanism phrase 060 registered
  (`not a verdict that it is dead`, `Main`, `reflection`, `Overrides`, `InitializeComponent`, `prevent
  instantiation`, `AddressOf`, `outside the solution`, `generated outside the solution tree`, `implemented but
  never called`, `referenced only by a sibling`, `no namespace rows`, `unreferenced by construction`, `used by its
  bare name`); for `symbol_search`, the 200-row cap and "narrow the filter rather than page"; for `symbol_detail`,
  the external marking and the three named refusals. A test asserts each phrase is present in the registered
  description.
- **FR-313**: Filters and narrowing are the same: `symbol_search` takes `name` and/or `kind` and optionally
  `projectSymbolId`; `orphans` takes optional `kind` and `projectSymbolId` with 060's refusals for an unexamined
  kind and a non-project row; `references` takes no filter and no cap.
- **FR-314**: Only active symbols appear in any result; a retired symbol is refused by name where it is asked for
  by id, and never listed.
- **FR-315**: The order of refusal is arguments → configuration → store → map → question, as 058 §6, so that a
  malformed call never opens a file. For `extract`, the cardinality of `solutionKey`, `repoPath` and `stale` is
  validated before the configuration is read (INC2).

**`type_usages` (task 152647)**

- **FR-316**: The bridge MUST serve `type_usages(symbolId, projectId | solutionKey)` for an active symbol of a
  type kind (Q3), returning the union, deduplicated by edge id (DUP1), of: every non-`part_of` edge whose target is the type; every `calls` edge
  whose target is one of its constructors; every `calls` or `uses` edge whose target is one of its direct members
  (the symbols whose container is the type); and every `implements` or `extends` edge whose target is the type.
- **FR-317**: The result MUST present, in this order: the symbol; the by-verb counts for all seven non-`part_of`
  verbs, zeros included; `total` and `fromOutside`; then the flat occurrence list, each occurrence carrying verb,
  path, line, column, the source symbol (id, kind, name), the actual target (id, kind, name — the type, a
  constructor or a member) and `fromInside`.
- **FR-318**: `references` keeps its exact-identity contract unchanged: `type_usages` is a second tool, not a
  widening (152647; the reasoning of 142175).
- **FR-319**: A `symbolId` of a non-type kind is refused by name with the remedy "use `references`"; the scope,
  existence and retirement refusals are those of `references`.

**`map_status` (task 152646)**

- **FR-320**: The bridge MUST serve `map_status()` with no parameters, returning one entry per active, bound
  registry row (Q1): `solutionKey`, `projectId`, `codememSolutionId`, the latest completed run's id, `commit_sha`,
  `is_dirty` and finish time from the map, the map's `repo_root`, and the repository's answers now — HEAD's sha,
  whether the tree is dirty, and the count of commits from the recorded commit to HEAD — each `null` where it could
  not be obtained, with the reason named.
- **FR-321**: Each entry MUST carry exactly one verdict by name from: `current`, `behind` (with the count and HEAD
  named), `dirty`, `no_git`, `map_missing_solution` and `diverged` (both shas named, no count; Q4, ruled
  2026-09-15), chosen in the order Q4 states.
- **FR-322**: `map_status` MUST never guess: a root that cannot be read, a repository that gives no answer, a
  recorded commit unknown to the repository, and a run that recorded no commit are each reported as what they are.
- **FR-323**: The result MUST also carry the counts of bound, unbound and inactive registry rows and list the
  unbound and inactive keys, so an operator can see what the bridge is not watching.
- **FR-324**: `map_status` MUST use only `extract_runs`, `solutions` and `code_map_solutions` plus the
  repository's own answers; no schema change and no stored "stale" (Article V: the word is never stored).

**`extract` (137077; 132115; 152658 rulings 2–4)**

- **FR-325**: The bridge MUST serve `extract(solutionKey | repoPath, stale?)` — exactly one of the three ways of
  naming the target — and it MUST cause a write only by running `CodeMem.Extractor` as a child process; the bridge
  itself never opens the map for write, not even to record what it launched (Article IX).
- **FR-326**: A `repoPath` MUST resolve through the registry to the one active, bound solution whose registered
  root (Q1) contains it or is it (directory containment, both sides normalised as directories — COR1) — the longest
  such root — and MUST be refused by name, naming the path, when no root
  contains it or when two roots tie; a `solutionKey` MUST match an active registry row exactly and its bound map
  solution must exist.
- **FR-327**: The child process MUST be run with `--solution-key` equal to the registry's `solution_key` (never
  defaulted — the launcher's whole job, 132094 as absorbed), `--solution` equal to the map's `last_seen_path` for
  that solution, and `--db` equal to the configured map path; nothing else is passed unless the plan names it.
- **FR-328**: The result MUST carry the extractor's exit code, its one summary line (or its one stderr line on a
  refusal) verbatim, and — when a completed run was published — that run's ten counts read from the map after the
  process exits. The bridge MUST NOT retry, and MUST NOT interpret an exit code other than by reporting it. A child that exceeds a
  540 s budget — inside the hook's 600 s — is killed and reported as timed out, never as a success (TIM1).
- **FR-329**: `stale: true` MUST evaluate `map_status` and extract, in sequence and one run each, every bound
  solution whose verdict is `behind`, `dirty` or `diverged`, skipping the rest and listing every solution with what
  happened, one log line per solution launched and no header line (INC3); it is the manual verb (152658 ruling 4) and is never invoked by the trigger.
- **FR-330**: Two gates, read from `bridge.config.json` on every call, both defaulting to off: `extract.enabled`
  gates the verb for every origin; `extract.onGreenBuild` gates the green-build origin only and is inert while
  `extract.enabled` is off. A refusal MUST name the gate that stopped it.
- **FR-331**: The bridge MUST distinguish three origins of an `extract` — tool (a call on the MCP surface),
  green-build (the hook) and manual (a human at the command line) — through one door (Article XII), with the
  mechanism Q5 proposes or the spike replaces it with.
- **FR-332**: The bridge MUST run at most one extraction at a time; a second call while one runs is refused by
  name. It MUST wait for the child to exit and MUST NOT leave it running.
- **FR-333**: A refused `extract` — any gate, any resolution failure — MUST start no process and leave the map
  byte-identical (asserted by hash).
- **FR-334**: `extract` MUST hold no map connection while the child runs, so the extractor's `BEGIN IMMEDIATE`
  is never blocked by the process that launched it.

**The trigger and what ships (152658 rulings 2 and 4; Q11)**

- **FR-335**: The feature MUST ship a settings fragment holding one PostToolUse hook on Bash that, when the
  command is a `dotnet build` or `dotnet test` and the build exited 0, invokes the bridge's green-build origin
  with the build's directory as `repoPath` (Q6, COR2: the `.sln` or `.vbproj` token anywhere after the verb, or a
  directory token immediately after it — authoritative by shape, whether or not the path exists, and parsed after
  any options such as `-c Debug`; more than one candidate is refused as ambiguous, never resolved by fallback; the
  directory the command ran in only when it names nothing); a named path under no registered root is refused
  naming that path, and the session directory is never tried in its place. The hook MUST never invoke the extractor executable, and it MUST
  never fail the tool call it observed.
- **FR-336**: The fragment MUST NOT be installed by this feature: no file under the user's `.claude` directory is
  written or edited; the process document names where to merge it and why user scope.
- **FR-337**: The feature MUST ship `.mcp.json` at the repository root registering the bridge as a stdio server,
  and the process document MUST name the user-scope registration for sessions in other repositories.
- **FR-338**: The trigger MUST refresh the built solution only: one `--solution-key`, one run; it never iterates the
  registry.
- **FR-339**: The process document MUST name when each tool is called (Q11) and MUST record the two gates, their
  defaults and who flips them (the Architect, 132115).

**Presentation (task 152648)**

- **FR-340**: Where several active symbols of one solution share kind, name, path and line under different
  declaring projects, `symbol_search` MUST present one declaration carrying `compiledInto` — the list of
  (declaring project id and name, symbol id) — and `symbol_detail` MUST present the same header for any of the
  ids (Q2). The map is not changed; every id stays addressable. Raw rows are preserved: a group of rows sharing the four
  fields under one declaring project stays one declaration per row (COR3).
- **FR-341**: Symbols differing in any of the four fields, or belonging to different solutions, are never folded.

**Invariants as tests (Articles II, III, XIII; description)**

- **FR-342**: A contract test MUST assert the map file is byte-identical (SHA-256) after every tool call except
  `extract`, driven through the spawned executable over stdio for every read tool, with `extract` driven the same
  way for one gate refusal and one launch (CON3), on a copy of a real map, and that the bridge's connection door carries `Mode=ReadOnly`; the write
  attempt through that door MUST fail with the driver's read-only error, demonstrated once (the FIRE record).
- **FR-343**: A gate test of the same shape as `ProjectFileGateTests` MUST assert that no project file under
  `src/` or `tests/` (fixtures excluded) contains a `ProjectReference`, `PackageReference` or `Compile Include`
  whose path contains `rchaudio-a11y` or `MemOS`, counting a positive number of project references first so the
  scan cannot pass vacuously.
- **FR-344**: A fact MUST assert, on the MemOS map, `type_usages(3023)` ≥ 16 constructor calls and
  `references(3023)` = 0; on the live map it is Skip-armed like the acceptance runner and recorded in the
  quickstart; on the fixture map an equivalent shape runs unconditionally.
- **FR-345**: Facts MUST assert `map_status` on the current MemOS map reports `behind` with HEAD named, and, after
  a green extract of MemOS, `current` (Q10: the live half is the operator step; the fixture half creates the
  repository states).
- **FR-346**: Facts MUST assert the gate matrix of US4 scenarios 1–3 by name of the gate in the refusal text.
- **FR-347**: A fact MUST assert `extract` with a `repoPath` under no registered root refuses naming the path,
  starts no process and leaves the map byte-identical.
- **FR-348**: A fact MUST assert, over every occurrence any tool returns in a full run of the suite, that it
  carries a non-NULL mapped symbol id or is marked external with its doc-comment id, never neither.
- **FR-349**: The feature MUST change no file in the MemOS repository; a fact or the quickstart records `git
  status` of that repository clean before and after, so the Shell's own `codemem_*` suites are untouched.
- **FR-350**: Every guard is Red-first where a Red is possible and carries its FIRE demonstration in the test
  header; a guard that cannot go Red first (a refusal that exists before the test) records its fire instead
  (003 FR-219's rule).

**The extract log (ruled at clarify, 2026-09-15)**

- **FR-351**: The bridge MUST append one line to `extract.log` beside its executable for every `extract`
  invocation, whatever its outcome — always on, no configuration key: the UTC time, the origin (tool,
  green-build, manual), the target as given (`solutionKey`, `repoPath` or `stale`), the resolution (the key it
  resolved to, or the refusal by name), the gate outcome (which gate refused, or passed), and, when a child ran,
  its exit code and its summary or refusal line verbatim (`timeout` for a killed child); for `stale`, one line per
  solution launched and no header line (INC3); the file lives beside the executable whatever `--config` names. Reads
  are never logged. A line that cannot be written never stops the extraction; the result names the failure to
  log. A fact asserts one line per invocation across the gate matrix of FR-346 and the refusal of FR-347.

### Key Entities *(include if feature involves data)*

- **Registry row**: one `code_map_solutions` row — `solution_key` (the value passed as `--solution-key`, never
  defaulted), `project_id`, `codemem_solution_id` (the map's `solutions.id`, NULL until the first run publishes),
  `extraction_scope` (not used by the bridge, Q1), `state` (`active` | `inactive`). Read-only here.
- **Bound solution**: an active registry row with a non-NULL `codemem_solution_id` whose map `solutions` row
  exists. The unit of `map_status` and of `extract(stale)`.
- **Registered root**: the map's `solutions.repo_root` of a bound solution (Q1); the directory `repoPath` is
  resolved against and `map_status` asks the repository at.
- **Verdict**: one of `current`, `behind`, `dirty`, `no_git`, `map_missing_solution`, `diverged` (Q4); never
  stored, computed per call from `extract_runs.commit_sha`, `is_dirty` and the repository's answers.
- **Gate**: a boolean in `bridge.config.json` — `extract.enabled`, `extract.onGreenBuild` — read per call,
  default off, flipped by the Architect; a refusal names it.
- **Origin**: which door an `extract` came through — tool, green-build, manual; only green-build meets the second
  gate.
- **Occurrence**: a `code_edges` row presented with verb, path, line, column, source and target; the target is a
  mapped symbol id or external with its doc-comment id (never neither); in `type_usages` it also carries
  `fromInside`.
- **Presented declaration**: one or more active symbols of one solution sharing kind, name, path and line under
  different declaring projects, presented once with `compiledInto`.
- **Bridge configuration**: `bridge.config.json` beside the executable — `mapPath`, `storePath`, `extractorPath`
  (optional), the two gates; never defaulted, never cached.
- **Extract result**: exit code; the summary or refusal line verbatim; the ten counts of the published run when
  there is one; for `stale`, one such result or a skip verdict per bound solution.
- **Extract log line**: one appended line per `extract` invocation in `extract.log` beside the executable —
  time, origin, target, resolution, gate outcome, child exit code and line; never for a read (FR-351).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-301**: A Claude Code session started in `repos\CodeMem` with the Shell closed lists exactly eight tools
  from the bridge — `solutions`, `symbol_search`, `symbol_detail`, `references`, `orphans`, `type_usages`,
  `map_status`, `extract` — and each of the seven read tools answers against the live map in under 3 seconds for
  the calls US1–US3 and US5 name, with the map's SHA-256 unchanged across the whole session.
- **SC-302**: For each of the five ported tools, one live call through the bridge and the same call through the
  MemOS Shell return equal results with the audit-only fields removed (same rows, same counts, same order),
  recorded side by side in the quickstart for `GameRoom` symbol 1200 (`Shoe.Draw`, 7 references) and for
  `orphans` on `GameRoom`.
- **SC-303**: `type_usages(3023)` on the MemOS map returns ≥ 16 constructor calls (16 at the time of writing, all
  naming 6661 as target), `total` ≥ 20 and `fromOutside` ≥ 16, while `references(3023)` returns 0.
- **SC-304**: `map_status()` on the live map on the day of implementation reports MemOS `behind` with HEAD named
  and a count ≥ 1, GameRoom `dirty` (or `current` if the tree has been cleaned; whichever, it matches `git status`
  that minute), and after the live extraction of MemOS, MemOS `current` with run 7 named.
- **SC-305**: The gate matrix: 5 named outcomes (both off → `extract.enabled`; first on, hook → `extract.onGreenBuild`;
  first on, tool → runs; second on only, hook → `extract.enabled`; both on, hook → runs), each asserted by the gate's
  name in the refusal text or by the child's exit code.
- **SC-306**: `extract` with an unregistered `repoPath` names the path, starts 0 processes (asserted through the
  launcher seam) and leaves the map's hash unchanged; the same for every other refused `extract`.
- **SC-307**: 100% of occurrences returned by any tool during the suite carry a mapped id or the external marking
  with a doc-comment id; 0 carry neither.
- **SC-308**: 0 project files reference `rchaudio-a11y` or `MemOS`; the MemOS repository's `git status` is clean
  before and after the feature; 0 files in it differ.
- **SC-309**: On the live MemOS map, `symbol_search(name "CodeMemMapFixture", kind "class")` returns `total` 1 with
  `compiledInto` of length 2 naming ids 3556 and 4200, and the 222 twin groups measured on 2026-09-15 all present
  as one declaration each.
- **SC-310**: The hook end to end: after a green `dotnet build` in a registered repository with both gates on, the
  session shows the extractor's exit code 0, its summary line and the ten counts within the hook's timeout;
  after the same build with `extract.onGreenBuild` off, the session shows a refusal naming that gate; after a
  build in an unregistered directory, a refusal naming the path; the tool call that ran the build is never
  failed by the hook.
- **SC-311**: The full suite: every Stage A, 002 and 003 test still green; the new facts green; the suite under
  5 minutes on this machine (baseline 68 passed / 1 skipped / 2.2 min before this feature).
- **SC-312**: Review gates: header block and XML docs on every new or changed file; Option settings in the new
  project file; no SQL outside a named repository method; the tripwire, SQL-location and the new MemOS-reference
  gates green; every new guard with its FIRE line.

## Spike (before planning; description: STANDARD, one spike)

Two facts the design rests on, unverified by anyone in this repository. Each is proven on this machine with the
smallest artefact that can prove it, and the evidence — the exact files used, the exact text observed — is
recorded in `specs/004-codemem-bridge/spike.md` before `/speckit-plan` runs. The spike writes nothing to the
user's `.claude` directory it does not remove again, and nothing to either live database.

1. **Claude Code loads a stdio server from `.mcp.json` in `repos\CodeMem`.** Artefact: a minimal stdio server
   (the bridge skeleton with one tool, or a throwaway console) registered by a `.mcp.json` at the repository root.
   Proof: a new session in this repository shows the server's tool to the model and a call returns; the approval
   prompt (or the setting that pre-approves project servers) is recorded as observed, and so is the tool name as
   the model sees it (`mcp__<server>__<tool>`).
2. **A PostToolUse hook on Bash can observe a `dotnet build` exit code and call a tool.** Artefact: a hook command
   that writes the JSON it receives to a scratch file and, on a green build, invokes the stdio server's one tool
   through the one-shot entry (Q5 (a)); and, beside it, one hook of type `mcp_tool` calling the same tool (Q5 (b)).
   Proof: the recorded payload shows what identifies the command (`tool_input`), the working directory (`cwd`)
   and how success is known — a field carrying the exit code, or the documented fact that `PostToolUse` fires
   only on success while a failed `dotnet build` (exit 1, forced by a deliberate compile error) fires the failure
   event instead; the spike runs both a green and a red build and records which event fired with what payload.
   Then: the hook's invocation reaches the server, and its text reaches the session through the documented
   output shape (`hookSpecificOutput.additionalContext`, or whichever the spike observes the model actually
   sees); the time from build exit to the hook's return is measured against the per-hook `timeout` the fragment
   will set (the documented default is 600 seconds). If (b) works, the spike states what it can and cannot
   express — the command match, the exit status, the origin — so STOP 1 can choose.

If fact 2 fails as stated — the hook cannot know the exit status, or cannot report into the session — the spike
says so and STOP 1 rules between the alternatives (a stdout marker, a wrapper command, the MSBuild target 152658
sequenced as a follow-on). The spec does not proceed to plan on an assumed fact.

Documented facts the spike starts from (Claude Code hooks and MCP references, read 2026-09-15 through the
documentation agent; each to be confirmed by observation, not assumed): every hook receives `session_id`, `cwd`,
`hook_event_name`, `tool_name`, `tool_input` and `tool_response` on stdin; a command hook's JSON on stdout is
parsed for `decision`, `reason` and `hookSpecificOutput.additionalContext`; a per-hook `timeout` is set in seconds
on the handler; `.mcp.json` takes `{"mcpServers": {"<name>": {"type": "stdio", "command": …, "args": […]}}}`, a
project server is approved by the user on first use and the choice can be reset with `claude mcp
reset-project-choices`; tools are named `mcp__<server>__<tool>` to the model; project `.claude/settings.json` is
shared and applies to sessions in that repository only, which is why the fragment targets user scope (Q11).

## Constraints (the description's "Do not")

- No reference, link, copy or vendored source from MemOS (FR-302; 142362 §3a).
- The map is never opened for write, not even to record a run (FR-303, FR-325; Article IX).
- No schema bump. If a column seems necessary, the work stops and says why.
- No localhost or HTTP host; stdio only (FR-301).
- No `rename_candidates`, `unregistered`, `repo_drift`, `external_usage` (142175) or `spec_conformance` (142393)
  in this feature; second pass.
- No auto-install of the hook and no edit of the user's `.claude/settings.json` (FR-336).
- The trigger refreshes the built solution only (FR-338).

## Assumptions

- **The registered root and the solution path come from the map** (Q1, ruled 2026-09-15). The registry has no
  path the bridge can resolve on its own, and the map's `repo_root` and `last_seen_path` are refreshed by every
  run. A registry row with no binding is visible but not extractable through the bridge; today none exists.
- **The three live registry rows stay bound and active** through this feature; task 131411's anchor repoint
  changes `extraction_scope`, which the bridge does not read, so it does not affect the bridge.
- **The extractor's contract is Stage A's, as amended by 002 and 003**: exit codes 0–4, one summary line on stdout
  on 0 (prefixed on 4), one line on stderr on a refusal, `--solution-key` always explicit. The bridge relies on
  nothing else about it.
- **The hook payload identifies the command and its directory**: the spike confirms this and how the exit status
  is known; the spec's hook requirements are written to that outcome.
- **A read-only connection in the map's journal mode does not block the extractor** as long as it is closed
  before the child starts; the bridge closes every connection inside the call that opened it (FR-303, FR-334).
- **MemOS at HEAD `cbeb661` compiles green**, so the live acceptance's extraction of MemOS completes; if it does
  not, the exit-2 result is the recorded outcome and `map_status` keeps saying `behind` — which is itself the
  feature working.
- **The Shell may be open or closed** while the bridge runs; it attaches the map read-only per call and does not
  hold it, so neither process blocks the other's reads.
- **Tests that need a registry** use a throwaway store holding only `code_map_solutions` (its DDL is transcribed
  from migration 029's verbatim section into the test support of this repository, as MemOS's fixture carries
  CodeMem's schema verbatim — a copy of a contract, not a reference to a project).
- **`git` is available on this machine** (the repositories were read with it during specification); whether the
  bridge uses the executable or the library the extractor already carries is the plan's decision.

# Quickstart: CodeMem Fixpack 003

Validation guide for the two extractor rules. Rules: [contracts/extraction-rules.md](contracts/extraction-rules.md);
command line: [contracts/cli.md](contracts/cli.md); 001's and 002's quickstarts still apply for build, hand
extraction, the upgrade and the earlier refusals.

## Prerequisites

- As 002: Windows 11, .NET SDK 9 or 10 (10.0.401 observed), .NET 8 runtime and WindowsDesktop runtime.
- For the live-map steps: the three solutions restored (`dotnet restore` in each), and a file copy of
  `C:\_DB\codemem.sqlite` taken first.

## Build and test

```powershell
dotnet build CodeMem.sln -c Debug
dotnet test CodeMem.sln
```

Expected: 0 errors, 0 warnings; every Stage A and 002 test green; the new `Fixpack003/` tests green;
`AcceptanceRunner` Skipped. Baseline before this feature, measured 2026-09-13: 59 passed, 1 skipped, 1 m 54 s.

Observed on the finished tree, 2026-09-13 (T019; `dotnet build` 0 warnings, 0 errors):

```text
  Skipped CodeMem.Tests.AcceptanceRunner.ReportOnTheNamedSolution
Total tests: 69   Passed: 68   Skipped: 1   Failed: 0   Total time: 2.1693 Minutes
```

59 prior facts + 9 new: `R01_ScopeRootTests` (5: the four fixture facts and the comparison fact added for the STOP 1
addition), `R01_DuplicateRefusalTests` (1), `R02_BareNameTests` (3). Two prior facts amended in place, both Reds
named in plan §Test design before they ran: `S02_SchemaUpgradeTests` (1) (`code_edges` 137 → 144 on the Stage A
fixture map; now compared with a fresh map from the same executable) and `US1_FirstRunTests` (`extractor_version`
`0.1.0` → `0.2.0`). Fixture extraction inside the tests: 1–4 s per run (SC-010's 60 s budget holds). Starting
point confirmed (T001): tree at `4efb7f9`, only `specs/003-extractor-fixpack/` and `.agents/` untracked, baseline
59 passed / 1 skipped / 1 m 54 s.

## Fixture validation (what the new tests do; runnable by hand on a copy)

1. **Injected out-of-scope file** — copy `tests/CodeMem.Tests/Fixtures/Sample` to a temp directory; beside
   the copy create `.nuget/packages/fake.test.sdk/1.0.0/build/net8.0/Fake.Test.Sdk.Program.vb` declaring a
   module in `Namespace Global` with a `Main` and one public function; add a `<Compile Include>` for it to
   both project files; make `Consumer.Build` call the function. Extract into a temp map. Expected: exit 0; no
   `code_symbols` or `code_parts` row with a path containing `.nuget/packages/`; one `calls` edge from
   `M:Sample.Consumer.Build` to the function with `target_symbol_id` NULL; residuals 0 · 0.
2. **Repository above the solution** — same copy, `git init` in the directory above it, a `Shared/Extra.vb`
   beside the solution linked into `Sample.Lib`, the injected file moved outside the repository, everything
   committed. Expected: `T:Sample.SharedExtra` is a row at `../Shared/Extra.vb`; the injected module is not;
   `solutions.repo_root` is the directory above the copy.
3. **In-repo pair** — a `Dup.vb` declaring `Public Class Dup` in both projects; run the executable with a fresh
   `--db`. Expected: exit 1, one stderr line naming `T:Sample.Dup` at `Sample.App/Dup.vb:1,14` and
   `Sample.Lib/Dup.vb:1,14`, empty stdout, 0 user tables in the map file.
4. **Bare names on the committed fixture** — extract `Sample.sln` and read `calls` edges. Expected, all with
   non-NULL targets and span text equal to the name:

   | Source | Target | Where |
   |--------|--------|-------|
   | `M:Sample.Fields.Total` | `F:Sample.Fields.a` | `Sample.Lib/Fields.vb`, the `Return a` line |
   | `M:Sample.MainForm.MainForm_Load(System.Object,System.EventArgs)` | `P:Sample.MainForm.Timer1` | `Sample.App/MainForm.vb`, the `AddHandler` line |
   | `M:Sample.MainForm.MainForm_Load(System.Object,System.EventArgs)` | `M:Sample.MainForm.OnTick(System.Object,System.EventArgs)` | same line, at `OnTick` |
   | `M:Sample.MainForm.MainForm_Shown(System.Object,System.EventArgs)` | `P:Sample.MainForm.Timer1` | `Timer1.Start()` |
   | `M:Sample.MainForm.OnTick(System.Object,System.EventArgs)` | `P:Sample.MainForm.Timer1` | `Timer1.Stop()` |
   | `M:Sample.MainForm.Dispose(System.Boolean)` | `F:Sample.MainForm.components` | `Sample.App/MainForm.Designer.vb`, twice on the `If disposing AndAlso components …` / `components.Dispose()` lines |

   And still exactly 5 `handles` edges (I3), and I12 green over every edge.
5. **External bare name** — on a copy, add `sb.Append(vbCrLf)` to `Consumer.Build`. Expected: no edge whose
   target is `F:Microsoft.VisualBasic.Constants.vbCrLf`.
6. **Two runs** — extract the fixture twice into one map. Expected on run 2: `rename_candidates = 0`,
   `symbols_retired = 0`, residuals 0 · 0, `rename_candidates` row count unchanged; fact sets equal (I2).

## Live map: the operator steps (after the plan stop; the acceptance of tasks 142376 and 142358)

**Ruled at STOP 1: the runs go against a copy of the map, never the live file.** The copy was taken to the
session scratchpad (`codemem.003-copy.sqlite`, SHA-256 `1cae3e92…` = the live map's) and every command below
names it as `--db`. The live `C:\_DB\codemem.sqlite` and `C:\_DB\memos.sqlite` were hashed before and after
(`1cae3e92…`, `4fd627a8…`): **both unchanged**. To take a fresh copy:

```powershell
Copy-Item C:\_DB\codemem.sqlite <somewhere outside C:\_DB>\codemem.003-copy.sqlite
```

Record the orphan baseline (read-only; the 059 statement as [orphans-baseline.sql](orphans-baseline.sql),
run per solution id through a scratch read-only runner — there is no `sqlite3` CLI here; reproduced
2026-09-13):

| Solution | Total | Per project | Reproduced |
|----------|-------|-------------|------------|
| GameRoom | 306 | GameRoom 97, GameRoom.Tests 209 | yes, to the row (research R37) |
| CodeMem | 125 | Core 3, Extraction 1, Extractor 2, Tests 119 | yes |

Then the three runs, in this order (see [contracts/cli.md](contracts/cli.md) for the exact lines):

1. GameRoom, `--solution-key GameRoom`. Expected: exit 0; `symbols_retired = 2`; `rename_candidates = 0`;
   residuals 0 · 0; `extractor_version = 0.2.0`; digest equal to run 2's if the source is unchanged.
2. CodeMem, `--solution-key CodeMem`. Expected: exit 0; the same two retirements; new symbols for this
   feature's own types and tests; residuals 0 · 0.
3. MemOS, `--solution-key MemOS`. Expected: **one of** (a) exit 0 and a summary line — MemOS joins the map as
   solution 3, its id reported for the MemOS-side registry binding (not done here); (b) exit 1 and one
   refusal line naming an in-repo pair — recorded verbatim, work stops, the Article VI (A) amendment's
   trigger is reported.

### Record (2026-09-13, on the copy; T021–T024)

Summary lines, verbatim (each run exit 0; elapsed GameRoom 8 s, CodeMem 4 s, MemOS 27 s, load and compile included):

```text
solution=GameRoom run_id=4 observed=2170 matched=2170 reactivated=0 new=0 retired=2 registry_before=2172 notes_orphaned=0 candidates=0 unaccounted_observed=0 unaccounted_registry=0 digest=9883407acd732ee86808756e756ce9bd057baecf54801786ad6d4ad4aaa61ef7 sha=e1bbe024d9a49acc5f8756c2ebc79bb38092bb13
solution=CodeMem run_id=5 observed=725 matched=691 reactivated=0 new=34 retired=3 registry_before=694 notes_orphaned=0 candidates=0 unaccounted_observed=0 unaccounted_registry=0 digest=fdc2baf28ed8bea938e7981b647c8ac87543d346d8de29bcc4423e7f7acfff92 sha=4efb7f9e87e4e3ac1bd3e3e4126dac5ce1ca4dc5
solution=MemOS run_id=6 observed=14713 matched=0 reactivated=0 new=14713 retired=0 registry_before=0 notes_orphaned=0 candidates=0 unaccounted_observed=0 unaccounted_registry=0 digest=d72c3abdfa4c1b16516b08d531c904fdb5559cecd50337e0d56fce393e7fa049 sha=806f5f3fe45d05628f0c8370e337c6d16e43578c
```

**MemOS: outcome (a).** It completed and joined the copy as solution 3 (run 6): 10 project rows, 14 713 active
symbols (class 1 269, constructor 714, enum 36, enum_member 154, event 25, field 1 928, interface 71, method 7 264,
module 3, namespace 71, property 3 166, structure 2), 172 358 edges (calls 115 219, uses 35 490, part_of 14 703,
imports 4 986, implements 1 634, handles 232, extends 61, depends_on 33). 0 symbol rows and 0 edge rows carry a
`../` path anywhere in the copy; 0 edges reference the test platform's generated program (nothing calls it). The
Article VI (A) amendment stays deferred on honest grounds. The MemOS-side registry binding (`code_map_solutions`
row 1 → `codemem_solution_id`) is not done here; on the live map the id will be minted by its own run.

**GameRoom**: `extractor_version` 0.2.0, digest identical to runs 1 and 2 — the fact-set difference is the
extractor version alone (R38). Retired 2: `T:__MicrosoftTestPlatformAutoGeneratedProgram` and its `Main`
(rows kept, inactive). Edges 22 652 → 27 795 (calls 16 256 → 21 402; every other verb within ±1, the `part_of`
of the two retired rows).

**CodeMem**: `is_dirty = 1` (this feature's working tree). Retired 3: the same two injected rows plus
`M:CodeMem.Extraction.CompiledInputs.SourceTrees(Project,Compilation)` (its signature gained the scope; the
three-argument method is one of the 34 new symbols, no candidate because the body changed). Edges 7 342 → 8 542.

`rename_candidates`: 0 rows before, 0 after, on every solution.

| Orphans (059 statement) | GameRoom | CodeMem | MemOS |
|---|---|---|---|
| baseline (059 close-out; reproduced R37) | **306** = app 97 + tests 209 | **125** = Core 3 + Extraction 1 + Extractor 2 + Tests 119 | not in the map |
| after | **279** = app 73 + tests 206 | **136** = Core 3 + Extraction 1 + Extractor 2 + Tests 130 | **3 228** (ContractTests 2 485, IntegrationTests 531, Shell 76, Mcp 61, Business 41, Core 22, Data 11, DbToolkit 1) |
| delta by kind | app: method 72 → 49 (−23), event 1 → 0 (−1); tests: module 1 → 0 (−1), method 188 → 186 (−2); class, constructor, enum_member unchanged | Tests: class 34 → 37 (+3), constructor 22 → 23 (+1), method 62 → 70 (+8), module 1 → 0 (−1); libraries unchanged (6) | first measurement |
| explanation | rule 1: the generated module and its `Main` (−1 module, −1 method in tests). Rule 2: 23 app methods and 1 tests method reached only by bare `AddressOf` or bare name now have an inbound edge; 1 event reached only by bare `RaiseEvent`/`AddHandler` (Q5) leaves. Fields and properties: unchanged, still unexamined on the MemOS side | rule 1: −1 module, −1 method (`Main`). This feature's own tests: +3 test classes, +1 constructor, +9 test facts (found by reflection, no edge; 059 fact 6) = +8 methods net of `Main`. Libraries: 0 change | test code dominates (3 016 of 3 228), as 059 fact 6 predicts |

**`codemem_references` on `F:GameRoom.BlackjackControl._balance`** (symbol 174; its statement over the copy):
**0 occurrences before, 22 after**, all `calls`, all in `GameRoom/Games/Blackjack/BlackjackControl.vb`, with line
and column — lines 39, 40, 48, 49, 58, 70, 80, 81, 89, 90, 112, 114, 115, 171, 172, 173, 235, 249, 252, 253, 254,
269, from `StartGame`, `StopGame`, `UpdateHud`, `Chip_Click`, `btnClear_Click`, `btnDeal_Click`, `btnDouble_Click`,
`Settle`, `UpdateButtons`. 059's "lines 39–49" instance is among them. `M:GameRoom.DungeonControl.StepCornered`:
0 before, 1 after (`DungeonControl.vb` line 3987 column 45, from `EnqueueFoePhase` — the `AddressOf StepCornered`
site), so it leaves the orphan list.

### Live map record (2026-09-13, after the commit `f648846`; run by the Implementor at the Architect's request)

Backup first: `C:\_DB\codemem.pre-003.2026-09-13.sqlite` (SHA-256 `1cae3e92…`, the pre-003 map). Extractor rebuilt from
`f648846` (0 errors). The three runs against `C:\_DB\codemem.sqlite`, in order, each exit 0:

```text
solution=GameRoom run_id=4 observed=2170 matched=2170 reactivated=0 new=0 retired=2 registry_before=2172 notes_orphaned=0 candidates=0 unaccounted_observed=0 unaccounted_registry=0 digest=9883407acd732ee86808756e756ce9bd057baecf54801786ad6d4ad4aaa61ef7 sha=e1bbe024d9a49acc5f8756c2ebc79bb38092bb13
solution=CodeMem run_id=5 observed=725 matched=691 reactivated=0 new=34 retired=3 registry_before=694 notes_orphaned=0 candidates=0 unaccounted_observed=0 unaccounted_registry=0 digest=fdc2baf28ed8bea938e7981b647c8ac87543d346d8de29bcc4423e7f7acfff92 sha=f6488469469ed8a3e1e395355820f95d406205c0
solution=MemOS run_id=6 observed=14713 matched=0 reactivated=0 new=14713 retired=0 registry_before=0 notes_orphaned=0 candidates=0 unaccounted_observed=0 unaccounted_registry=0 digest=d72c3abdfa4c1b16516b08d531c904fdb5559cecd50337e0d56fce393e7fa049 sha=806f5f3fe45d05628f0c8370e337c6d16e43578c
```

Elapsed 16 s, 12 s, 31 s. Every count equals the copy's record above; CodeMem's run now carries the committed sha
with `is_dirty = 0`. **MemOS is solution 3 in the live map** (run 6) — the id `code_map_solutions` row 1 binds to
on the MemOS side. Orphans on the live map: GameRoom 279, CodeMem 136, MemOS 3 228 (as on the copy); `_balance`
22 occurrences; `StepCornered` 1. No `-journal` file remained; the map is 64 MB. `C:\_DB\memos.sqlite` hashed
`f876df36…` before and after the runs: untouched.

## What must not happen

- No write to `C:\_DB\memos.sqlite` or to `code_map_solutions` (FR-218). The MemOS Shell may keep running;
  it attaches the map read-only per call.
- No DELETE from, DROP of or cascade into `code_symbols` (I13 / tripwire).
- No edit to MemOS's test projects to avoid the collision.

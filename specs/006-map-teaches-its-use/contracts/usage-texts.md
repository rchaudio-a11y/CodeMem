# Contract: the shipped usage texts after 006 (the v1.5.0 gate's subjects)

**Date**: 2026-09-29 | **Spec**: [../spec.md](../spec.md)

Every text below is either verbatim from doc 191490 (hash in the spec's Q3 table) or an exact edit listed here. Nothing
else in these files changes.

## 1. `README.md`

| Where (line at `806b524`) | Before | After |
|---|---|---|
| 13, badge | `![MCP](https://img.shields.io/badge/MCP-8%20tools-FF6B35)` | `…/MCP-9%20tools-FF6B35)` |
| 14, badge | `tests-197%20passing` | the close's passing count (Q8 (d)) |
| 32, sketch | `8 MCP tools` | `9 MCP tools` (same width; the sketch's columns hold) |
| 131, tools heading | `Eight, over stdio, every one scoped to a `solutionKey`.` | `Nine, over stdio.` (Q8 (b), ruled) |
| 133–142, tools table | eight rows | nine: a row after `orphans` and before `extract`: ``\| **`rename_candidates`** \| Was this renamed? What became of a retired id? Ranked proposals; never applied. \|`` (the cell is 191490's sentence, hash `3ec2c4de…`) |
| 189–191, step 5 | the paragraph ending "…nothing here installs itself or turns itself on." | the same paragraph plus one last line: ``Replace `C:/path/to/CodeMem.Bridge.exe` in the fragment with the path your build produced.`` (hash `1d6946b3…`; Q8 (f), ruled) |
| after step 5, before `## Command lines` | — | step 6: 191490 §4's block, 16 lines (hash `f73f7029…`), one blank line before and after |
| 218, Layout | `197 passing, 9 skipped` | the close's figures |
| 229 | `v1.4.0, fifteen articles` | `v1.5.0, fifteen articles` (Q8 (e), ruled) |
| 248, Language support | `the bridge and its eight tools` | `the bridge and its nine tools` |
| 261, Status | `Features 001–005 merged · … 197 passing / 9 skipped` | `001–006` at the merge; the close's figures |

Not changed: "the eight edge rules" (250), every "eight verbs", and every other line. No pointer line (Q8 (c), ruled
out).

## 2. `docs/claude-code/`

| File | Content |
|---|---|
| `skills/codemem/SKILL.md` | 191490 §3's block + one final LF (70 lines; hash `509557ba…`) |
| `CLAUDE.snippet.md` | 191490 §2's **first** block + one final LF (13 lines; hash `0cb68826…`) |

Nothing else under `docs/claude-code/`. §2's private block and its heading appear in no file of the repository.

## 3. `src/CodeMem.Bridge/hooks/settings.fragment.json`

Line 9's command becomes `"\"C:/path/to/CodeMem.Bridge.exe\" hook"`. Everything else in the file is unchanged.
B05 (18) asserts the path and the absence of `C:/Users/`.

## 4. `src/CodeMem.Bridge/README.md` (the process document; Implementor text, not verbatim-sourced)

- **§"What the bridge is"**: "It serves eight MCP tools over stdio — `solutions`, …, `map_status` and `extract`" →
  "It serves nine MCP tools over stdio — `solutions`, `symbol_search`, `symbol_detail`, `references`, `orphans`,
  `type_usages`, `map_status`, `rename_candidates` and `extract`".
- **§"When each tool is called"** gains, after the `references`, `symbol_detail`, … entry: "- **`rename_candidates`** —
  when a tool refuses an id as `SymbolRetired`, with the id it names (`retiredSymbolId`); or after a build, to see what
  the run proposed as renames (`runId`, from `solutions`). A candidate is a proposal, never applied, and the code is read
  before acting on one. An empty answer is common: a changed signature or body is not a rename."
- **§"Refusals and remedies"**: "Twenty-nine kinds" → "Thirty-two kinds". `SymbolRetired`'s remedy → "Search again for
  the current symbol, or call `rename_candidates` with `retiredSymbolId` set to its id." Three rows are added after
  `NotAType`:

  | Kind | What it says | Remedy |
  |---|---|---|
  | `RunNotFound` | The map **holds no extract run with id** so-and-so. | Omit `runId` to list every run of the solution; `solutions` names each latest run. |
  | `RunOutOfScope` | The run **belongs to solution** another key. | Ask with that key, or pass a run of this solution. |
  | `CandidateCountMismatch` | The run **recorded N rename candidates but the map holds M**. | The run's evidence does not reconcile and the bridge does not repair the map; a new extraction writes a new run. Report it. |

- **§"What the bridge never does"**: "never returns a retired symbol" → "never returns a retired symbol as if it were
  live: `rename_candidates` names retired symbols as the retired side of a proposal, each side with its active state"
  (Q9).
- **§"What the bridge is"** (lines 17–18): the sentence "Every tool is scoped by `solutionKey`, the key of a
  `solutions` row in the map; `solutions` lists them." is **deleted**. The same ruling as Q8 (b): it is untrue of
  `solutions`, `map_status` and `extract`. *(STOP 1 decision 11, overruled.)*
- **Install step 4** (lines 54 and 64): both occurrences of
  `C:/Users/rchau/source/repos/CodeMem/src/CodeMem.Bridge/bin/Release/net8.0/CodeMem.Bridge.exe` →
  `C:/path/to/CodeMem.Bridge.exe`. This is the same fix as the fragment; the rest of step 4 is unchanged. *(STOP 1
  decision 11, overruled.)* After it, no `C:/Users/` in forward-slash form remains in the process document. The
  example hook line in §"Not in the map" names `C:\Users\…\vbCalc` in backslash form as a sample refusal and is not
  an install path; it is unchanged.

Deletions and a substitution only; no new wording.

## 5. What the gate's facts read (`UsageTextsGateTests`; research R79)

| Fact | Reads | Fails when |
|---|---|---|
| (1) | `initialize` over stdio; `BridgeServerInstructions.Text` | the advertised text differs from the constant, or holds a CR |
| (2) | the constant | a registered name is not a whole word in it |
| (3) | the constant, whitespace collapsed | "VB constructors are all named New", "Prove, don't grep", "Use text search for what the map does not hold" or "runtime behaviour" is missing |
| (4) | `README.md`, the table under `## The tools your assistant gets` | a registered tool has no row, or a row names an unregistered tool |
| (5) | `SKILL.md`, `CLAUDE.snippet.md`, whitespace collapsed | "The map proves structure; runtime behaviour needs its own proof." is missing from either |
| (6) | every file under `docs/` | a file contains "Rick" or "153204" (ordinal) |
| (7) | `README.md` | the four tool-count places say anything but nine: `MCP-9%20tools`, `9 MCP tools`, `Nine, over stdio.`, `its nine tools` |

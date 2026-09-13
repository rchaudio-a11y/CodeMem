# Data Model: CodeMem Fixpack 003

**Feature**: `003-extractor-fixpack` | **Schema**: version 2, unchanged ([../002-stage-a-fixpack/contracts/schema.sql](../002-stage-a-fixpack/contracts/schema.sql))
| **Parents**: [../001-extractor-codemem-sqlite/data-model.md](../001-extractor-codemem-sqlite/data-model.md),
[../002-stage-a-fixpack/data-model.md](../002-stage-a-fixpack/data-model.md)

No table, column, index or trigger changes. What changes is which rows a run observes and which edges it
writes. Only that is described.

## Scope root (run-time value, not stored)

| | |
|---|---|
| Source | `GitFacts.RepoRoot` when non-null (the value `SolutionsRepository.RefreshLabels` writes to `solutions.repo_root`); otherwise `SolutionPaths.BaseDirectory(solutionPath)` |
| Shape | absolute directory path, normalised, with one trailing separator for the prefix test |
| Predicate | `Contains(fullPath)`: `Path.GetFullPath(fullPath).StartsWith(root, OrdinalIgnoreCase)` |
| Consulted by | `CompiledInputs.SourceTrees` (declaring documents) and `ExtractionRun` step 8 (project files) — two call sites, one door |
| Why not stored | the repository case is already `solutions.repo_root`; the fallback is derivable from `solutions.last_seen_path`; a third label column would restate one of them (Article XII) |

## Declaring documents (staging input, changed)

Stage A: a syntax tree declares symbols when it belongs to a project document outside `obj/`
(`CompiledInputs.SourceTrees`). Now: **and its full path lies under the scope root.** The same set is the
edge rules' `EdgeContext.Trees`, so an out-of-scope tree neither declares nor sources anything.

Effects on staged records:
- `ObservedSymbol` with no in-scope part → not staged (already the rule for generated trees: `parts.Count = 0`).
- `ObservedPart` for an out-of-scope reference → not created; `BodyHash` is over in-scope parts only.
- A project whose file is out of scope → no `ProjectSymbols.Create` row and no `SymbolWalker.Walk` over its
  compilation; its `CompiledProject` still contributes to the green gate.

## Edges (observation table, unchanged shape; one more occurrence shape)

`code_edges` row for a bare-name occurrence:

| Column | Value |
|--------|-------|
| `verb` | `calls` |
| `source_symbol_id` | the nearest enclosing row symbol of the identifier (FR-016) |
| `target_symbol_id` | the member's row id — **never NULL** for this shape (FR-209) |
| `target_doc_comment_id` | the member's doc-comment id (original definition; reduced extension methods use their definition) |
| `via_symbol_id` | NULL (the CHECK admits via only on `handles`) |
| `path`, `start_offset`, `length`, `start_line`, `start_column` | the identifier token (FR-018: span text = surface name) |

A reference to an out-of-scope declaration keeps the external-target shape: `target_symbol_id` NULL,
`target_doc_comment_id` present, under whichever verb's rule produced it (`calls`, `uses`, `extends`,
`implements`, `depends_on`, `imports`, `handles`).

## Reconciliation (Article VI, unchanged algorithm; two observable effects)

- Re-extracting a solution mapped before this feature: rows whose only parts were out of scope are **not
  observed** → step 4 retires them (`is_active = 0`, row kept, `last_seen_run_id` unchanged), counted in
  `symbols_retired`. No new symbol shares their kind, container and hash, so (B) writes no candidate.
- Rule 2 changes no observed symbol: `symbols_observed`, `symbols_matched`, `symbols_new` are unaffected by
  it; only `code_edges` grows.

## Counts (Article VIII, unchanged)

All ten written as before; both residuals computed by `CountAuditor` and asserted 0 · 0 by every new test.
`rename_candidates` is 0 on every run this feature causes (FR-207, FR-215).

## Stamp (`RunStamp`, one value changes)

`ExtractorVersion` reads `0.2.0` from the extraction assembly (research R38). `SdkVersion`, digest, git facts
unchanged in meaning; the digest still covers out-of-scope compiled inputs (FR-206).

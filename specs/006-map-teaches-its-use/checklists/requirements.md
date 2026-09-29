# Specification Quality Checklist: CodeMem 006 — The Map Teaches Its Own Use

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-09-29
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- Items marked incomplete require spec updates before `/speckit-clarify` or `/speckit-plan`
- **No markers remain.** The description left thirteen decisions open or implicit. Each is answered in
  Clarifications (Q1–Q13) with its reasoning and its alternative, so the Architect can strike any of them at clarify
  or STOP 1 rather than discover them in code. Every one is a proposal until ruled.
- **The three review points written before the draft:**
  - Verbatim is Q3, with FR-511, FR-516, FR-517, FR-520 and SC-505. It defines the block boundaries, including
    §4's inner `powershell` fence, and requires LF normalisation, the plan's SHA-256s and the quickstart's zero-line
    diffs.
  - The run list under `retiredSymbolId` is Q2, with FR-504, FR-527 and SC-501. It gives the derivation of the
    retiring run and the acceptance line, 6754 → exactly run 53 against the MemOS-side reader's 97 rows.
  - Twins is Q1, with FR-502 and US1 scenario 7: bare ids and never folded, with four reasons and each side naming
    its project.
- **Items the Architect is most likely to argue:**
  - Q8 (b): "every one scoped to a `solutionKey`" is dropped from the README, because it is false for `solutions`,
    `map_status` and `extract`.
  - Q8 (c): no pointer line, because 191490 supplies no text.
  - Q8 (f): step 5 has no path to change, and a reader of the placeholder fragment is not told to substitute theirs.
  - Q9: the process document's "never returns a retired symbol" is amended.
  - FR-521's proposed reverse direction: the README table lists exactly the registered tools.
- On "no implementation details": as in 001–005, tool names and arguments, table and column names, refusal kinds,
  file paths of the shipped texts and the named tests (B01 (5), B05 (18), B07, B09 (4)) appear because they are the
  product's observable surface or pins the description itself names. `RunAsync`, `BridgeToolDescriptions.SymbolSearch`
  and `SymbolRetired` are the description's own "Change" list. The one mechanism named, "named `Read…` methods of
  Core's repositories", is Article XI's rule, not a design choice.
- On "non-technical stakeholders": the stakeholders are the Architect, the Operator, Claude Code as the consumer and
  the person who downloads CodeMem. The spec is written for them.
- Requirement numbers start at FR-501 and success criteria at SC-501. No parent spec is edited.
- §Measured records what was read on 2026-09-29: the live map through the bridge, the live candidates through the
  MemOS-side reader, the source at `806b524`, and the gates the new texts must pass. It says plainly that the map's
  CodeMem solution was 28 commits behind, so every fact about CodeMem's own code came from source and text, not from
  the map (v1.5.0's Map-first rule).
- Traps the spec had to establish, none of them in the description:
  - a fourth "eight tools" in the README;
  - README step 5 carries no path;
  - B01 (5) is vacuous for an unregistered tool;
  - B05 (18) passes with the placeholder unchanged;
  - no test pins today's `SymbolRetired` phrase;
  - `code_symbols` has no retiring-run column;
  - a verbatim multi-line constant checks out CRLF on this machine;
  - the snippet breaks the pinned sentence across a line;
  - the process document's "never returns a retired symbol" becomes false when the tool ships.

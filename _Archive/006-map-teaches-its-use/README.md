# _Archive/006-map-teaches-its-use

What feature 006 ("the map teaches its own use", PM 153204) retired on 2026-09-29. It is kept as code under Article XIV
of the constitution: archived, never deleted, never compiled. Nothing under `_Archive/` belongs to a project or to any
gate's scan.

## What moved

| Archived here | Original | What it was |
|---|---|---|
| `tests/BridgeStandaloneGateTests_RetiredFacts.vb` | `tests/CodeMem.Tests/Guards/BridgeStandaloneGateTests.vb`, fact (4) `TheConstitutionIsVersionOneFour` | asserted that the constitution's version line begins `**Version**: 1.4.0` (005 FR-431) |

## Why

The v1.5.0 amendment (`806b524`, 2026-09-29) moved the version line, and fact (4) has been red on `main` since. That
was observed at 006's plan in both baseline runs: "Expected start: `**Version**: 1.4.0`".

The Architect ruled at 006's STOP 1 (decision 1), verbatim: "fact (3) pins Article IX's text, which is what it
protected; (4) now only breaks main on every amendment."

Fact (3), `ArticleNineReadsAsVersionOneTwoOne`, stays in the live file and still pins Article IX's v1.2.1 body, the
rule 005 restored. Facts (1) and (2) are unchanged.

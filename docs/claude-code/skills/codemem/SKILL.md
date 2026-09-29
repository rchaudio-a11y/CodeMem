---
name: codemem
description: Answer questions about VB.NET/.NET code structure with the CodeMem MCP tools, and prove
  claims about callers and usages by compiler identity instead of text search. Use when asked who
  calls, constructs or uses a class or member; where a symbol is declared; whether a type or member
  is safe to rename or delete; what was renamed; what is unreferenced; whether a handler is wired;
  whether the code map is current — or before stating that nothing, exactly N, or only X uses
  something.
---

# CodeMem — how to use the map well

The map is what the compiler resolved, not what the text says. It answers structure in one or two
calls where a text search takes many and still cannot tell overloads, interface calls or
same-named symbols apart. It narrows the search; you still read the code you change.

## First move, every session

`map_status`. Read the verdict per solution:
- `current` — trust it.
- `behind` / `dirty` — it answers for an older tree. Refresh (a green build with the hook on, or
  `extract`) or say the answer came from a stale map. If only non-code files changed, say you
  judged it safe.
- `diverged` / `no_git` — treat answers as leads, not proof.
- `not_in_map` — the repo is not mapped. The result carries the exact command; a person runs it.

## The proof rule

A claim that nothing, exactly N, or only X calls, constructs or uses Y is proved with `references`
(a member) or `type_usages` (a type), and states the tool and the count. If `map_status` is not
current, refresh first or say the answer came from a stale map. A "nothing" claim also names what
the map cannot see that could still reach Y: Handles wiring, Overrides, interface dispatch,
reflection, public API. Where the map cannot answer (unmapped, C#) a text search may stand in, and
the claim says so. The map proves structure; runtime behaviour needs its own proof.

## Recipes

| Question | Calls | Read |
|---|---|---|
| Who constructs class X? | `symbol_search` name=X kind=class → `type_usages` | the constructor-call group. Never search kind=constructor by the class's name — every VB constructor is named `New`. A class with no declared constructor still shows its constructions here. |
| Who calls method M? | `symbol_search` name=M kind=method → `references` on each overload | overloads are distinct symbols; check each |
| Is type T safe to delete or rename? | `type_usages` | `fromOutside`. Zero is necessary, not sufficient — then check what the map cannot see (below) |
| Is member M safe to delete? | `references`, then `symbol_detail` | zero references; no outbound `handles`; not an `Overrides`; not an interface implementation; not public API |
| Is this handler wired? | `symbol_detail` | outbound `handles` (Handles clause and AddHandler both land here) |
| Where are all the parts of this partial type? | `symbol_detail` | `parts`; the declaring one is flagged |
| Prove "exactly one production construction" | `type_usages` | split occurrences by path (tests vs source); state the tool and the count |
| A symbol id was refused as retired — what became of it? | `rename_candidates` retiredSymbolId=N | ranked proposals of the same kind and container with an equal body hash — a proposal, never a fact; confirm in the code. **Empty is common:** a changed signature or body is not a rename (the hash differs) — re-find by name |
| What was renamed in the last build? | `rename_candidates` runId=N (from `solutions`) | same |
| Did the revert fully return the code? | `solutions` before and after | `sourceDigest` equal to the pre-change run means the compiled inputs are byte-back |
| What is unreferenced here? | `orphans` (narrow by kind or project) | a list of leads, never a delete list |

## What the map does not hold — reach for these instead

| Not in the map | Use |
|---|---|
| Values: string literals, numbers, SQL text, config keys, messages | Grep |
| Comments, markdown, configuration files | Grep / Read |
| Behaviour: what a method does, a missing Catch, a wrong result | read the method; tests |
| Dispatch the compiler cannot see: reflection, attributes, DI, event firing, interface calls at runtime | runtime observation; tests |
| History: who changed it, when | `git log` / `git blame` |
| Bare framework uses (`vbCrLf`, `Now`, `Len`) | Grep |
| Non-.NET files, unmapped repos, and C# (not mapped yet) | Grep / Read — and say the claim rests on text |

## Habits

- Narrow before you dump. `symbol_search` caps at 200 rows; filter by kind or `projectSymbolId`
  rather than parsing a large result.
- Ids are per-signature. Change a signature and the old id retires (the map refuses it by name).
  Re-find by name or ask `rename_candidates`; do not carry ids across builds in notes or plans.
- A refusal is an answer: it names the gate, the remedy, or the command. Read it before retrying.

# Contract: extraction rules (fixpack 003 amendments)

**Feature**: `003-extractor-fixpack` | **Amends**: Stage A spec FR-007, FR-010, FR-017 (`calls`), FR-019 step 1;
Stage A plan "Edge rules" table (the `calls` row); 002 FR-108 is restated, not changed.

Article IV: a verb is emitted only by a deterministic rule written in the plan. These are the rules as
amended; the plan's table repeats them in the compiler's terms.

## Scope of declaration (new; governs every symbol and every edge source)

**Scope root** — resolved once per run: the repository working directory when the solution's base directory
lies in a repository with a commit (identical to what the run writes to `solutions.repo_root`); otherwise
the solution's base directory.

**In scope** — a file whose **resolved full path** (relative segments resolved, separators normalised to the
platform's) starts with the scope root (trailing separator appended), compared **case-insensitively**
(STOP 1 ruling, 2026-09-13).

| Item | Rule |
|------|------|
| Declaring document | declares symbols only when in scope **and** outside the project's `obj/` (the existing rule) |
| Project file | the project contributes a project row, symbols and edges only when in scope; its compile errors always count toward the green gate |
| Occurrence | an edge is sourced only from an in-scope tree (they are the same set as the declaring documents) |
| Reference to an out-of-scope declaration | written like a reference to a framework symbol: `target_symbol_id` NULL, `target_doc_comment_id` present |
| Duplicate id among in-scope declarations | refused before reconciliation: exit 1, one stderr line `duplicate doc-comment id <id> at <path>:<line>,<col> and <path>:<line>,<col>`, nothing written (unchanged) |
| Compiled inputs, digest, dirty flag | unchanged: every compiled document outside `obj/`, in or out of scope |

## `calls` (amended: fourth occurrence shape)

Source: the enclosing row symbol (FR-016) of the occurrence. Target: the symbol the compiler binds. Location:
the name token.

| Occurrence shape | Bound target admitted | Excluded |
|------------------|-----------------------|----------|
| invocation expression | method, constructor, property, event, field (unchanged) | unresolved or candidate-ambiguous; locals, parameters; inside `Handles`/`Implements`/`Inherits`/`Imports` clauses |
| object creation | constructor; an implicit constructor redirects to the type (unchanged) | as above |
| member access | method, constructor, property, event, field (unchanged) | as above |
| **simple name** (identifier or generic name) that is **not** the member name of a member access, **not** part of a qualified name, and **not** an attribute name | **field** (constants and enum members included), **property** (WithEvents members included), **event**, **method** of kind ordinary, `Declare` or reduced extension — **and only when the target is a row of this run** | unresolved or candidate-ambiguous; locals, parameters, range variables, types, namespaces, type parameters; constructors (attribute names); inside `Handles`/`Implements`/`Inherits`/`Imports` clauses; a target with no row (external, or out of scope) — **a stated limit** (STOP 1): a bare external member leaves no edge, unlike the same member through member access |

Rules that follow from the shape:
- one identifier → one occurrence; get/set and read/write are not distinguished; a compound assignment is
  one occurrence;
- `AddressOf Member` with no receiver → one `calls` occurrence to the method; an `AddHandler` around it still
  writes the handler's `handles` edge (unchanged);
- a declaration (WithEvents member, field declarator, property or event statement) is never an occurrence;
- an occurrence that coincides with an invocation occurrence at the same identifier folds into one edge
  (canonical key: source, verb, target, via, path, offset, length);
- FR-018 holds: span text is the member's surface name; line and column are inside the referencing file.

## Unchanged verbs

`part_of`, `uses`, `implements`, `extends`, `imports`, `depends_on`, `handles`: rules as in Stage A; their
sources are now drawn from in-scope trees only, and their targets may be out-of-scope declarations
(external-target shape).

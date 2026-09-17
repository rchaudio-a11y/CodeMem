# _Archive/004-store

What feature 004 (the CodeMem bridge) built to read MemOS's `code_map_solutions` registry, and feature 005 ("the bridge stands
alone", PM 152687) retired on 2026-09-17. Kept as code under Article XIV of the constitution: archived, never deleted, never
compiled. Nothing under `_Archive/` belongs to a project or to any gate's scan.

## Why

Constitution v1.4.0 (2026-09-17) returned Article IX to its v1.2.1 sentence - one database, one writer - and the connection-site
gate to one file. The bridge opens exactly one database file, the map. Selecting and binding solutions through the MemOS store's
registry was MemOS's concern, not the map's: the bridge now scopes by `solutionKey` against `solutions.key` and resolves a
directory through `solutions.repo_root`, and a directory the map does not hold is answered "not in the map" with the command that
adds it. `BridgeStandaloneGateTests` (2) proves the eight names below absent from every `.vb` file under `src/` and `tests/`.

## What moved

Every file was moved with `git mv` at T024 to the same relative path under this folder; each was last touched in commit `fe59871`
(feature 004).

| Archived here | Original path | What it was |
|---|---|---|
| `src/CodeMem.Core/Repositories/StoreDatabase.vb` | `src/CodeMem.Core/Repositories/StoreDatabase.vb` | the store database: OpenReadOnly over the MemOS store file |
| `src/CodeMem.Core/Repositories/Registry/CodeMapSolutionsRepository.vb` | `src/CodeMem.Core/Repositories/Registry/CodeMapSolutionsRepository.vb` | the registry module: ReadAll over code_map_solutions |
| `src/CodeMem.Core/Repositories/Registry/RegistryTableMissingException.vb` | `src/CodeMem.Core/Repositories/Registry/RegistryTableMissingException.vb` | raised when the store holds no code_map_solutions table |
| `src/CodeMem.Core/Records/RegistryRecord.vb` | `src/CodeMem.Core/Records/RegistryRecord.vb` | one registry row: project id, solution key, state, codemem_solution_id |
| `src/CodeMem.Bridging/Reading/StoreAccess.vb` | `src/CodeMem.Bridging/Reading/StoreAccess.vb` | the bridge's one door to the store: open read-only, read the registry, close |
| `src/CodeMem.Bridging/Reading/Envelopes/UnboundEntryEnvelope.vb` | `src/CodeMem.Bridging/Reading/Envelopes/UnboundEntryEnvelope.vb` | map_status's unbound list entry |
| `src/CodeMem.Bridging/Reading/Envelopes/InactiveEntryEnvelope.vb` | `src/CodeMem.Bridging/Reading/Envelopes/InactiveEntryEnvelope.vb` | map_status's inactive list entry |
| `tests/CodeMem.Tests/Support/RegistryFixture.vb` | `tests/CodeMem.Tests/Support/RegistryFixture.vb` | a temp store with the 029 §1 DDL transcribed, seeded per test |

## The seven retired refusal texts (004 contracts/tools.md §6)

| Kind | Text |
|---|---|
| `RegistryAbsent` | The store at '{storePath}' holds no code_map_solutions table. Nothing is wrong with the map; the registry migration has not gone live on that store. |
| `ScopeConflict` | Supply exactly one of projectId or solutionKey. Both were supplied; they can disagree, so neither is chosen for you. |
| `KeyNotRegistered` | No code_map_solutions row has solution_key '{key}'. Keys are exact; solutions lists the map's, map_status the registry's. Nothing ran. |
| `KeyUnbound` | Registry row '{key}' has no codemem_solution_id: it has never been published. Run the extractor by hand once with --solution-key {key} and bind the row; the bridge does not extract an unbound solution. |
| `KeyInactive` | Registry row '{key}' is {state}, not active. Nothing ran. |
| `MapMissingSolution` | Registry row '{key}' binds map solution {id}, but the map at '{mapPath}' holds no solution {id}. Nothing ran. |
| `PathNotRegistered` | No registered solution's repository root contains '{path}'. Registered roots: {roots}. Nothing ran. |

Retired with them, without a kind of its own: the projectId branch's "no registry row names project N" text (`ProjectIdRemoved`
answers a `projectId` now, naming the argument). The bridge's `storePath` configuration key is refused by name (`ConfigKeyRetired`).

## The retired test facts

`tests/` holds the facts that asserted the registry, as `.vb` fragments with their headers - code, not compiled:

- `B02_RetiredFacts.vb` - `ScopeConflict`, `RegistryAbsent`, the "no registry row" text
- `B04_RetiredFacts.vb` - `map_missing_solution`, the unbound and inactive lists
- `B05_RetiredFacts.vb` - the unbound, inactive and ghost key refusals; the empty registry
- `B08_RetiredFacts.vb` - the live search by projectId
- `BridgeSqlGateTests_RetiredFacts.vb` - the registry module's literals; the store opened only by the bridge

## Code questions: CodeMem first
- For questions about .NET code structure — where something is declared, who calls, constructs or
  uses it, whether it is safe to change or remove, what was renamed — use the CodeMem MCP tools
  before Grep, Glob or Read. Call `map_status` first.
- A claim that nothing, exactly N, or only X calls, constructs or uses Y is proved with
  `references` (a member) or `type_usages` (a type), and states the tool and the count. If
  `map_status` is not current, refresh first or say the answer came from a stale map. A "nothing"
  claim also names what the map cannot see that could still reach Y: Handles wiring, Overrides,
  interface dispatch, reflection, public API. Where the map cannot answer (unmapped, C#) a text
  search may stand in, and the claim says so. The map proves structure; runtime behaviour needs its
  own proof.
- Text search stays right for comments, strings, markdown, SQL, configuration and unmapped files.
- Recipes and limits: the `codemem` skill.

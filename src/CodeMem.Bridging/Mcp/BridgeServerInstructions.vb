' File: BridgeServerInstructions.vb
' Project: CodeMem.Bridging
' Description: The server's instructions, advertised at initialize: MemOS doc 191490 §1 verbatim (feature 006, FR-511; spec Q3).
' Author: RCH Automation LLC
' Created: 2026-09-29
'
' Written from the store by the Q3 rule, never retyped; the two quotations are doubled for VB only. This file is pinned eol=lf in
' .gitattributes (research R75), so the literal's line breaks are LF on every checkout; UsageTextsGateTests (1) asserts no CR.
' An edit here is an edit of ratified public text: it goes back to the Documenter first (spec Q3, Constraints).

''' <summary>
''' The one constant the server advertises and the usage-texts facts read (Article XII).
''' </summary>
Public Module BridgeServerInstructions

    ''' <summary>191490 §1, 25 lines, LF.</summary>
    Public Const Text As String = "CodeMem serves a compiler-resolved map of VB.NET solutions: every symbol, and every call, use,
construction, implements, extends and Handles wiring the compiler resolved — by identity, not text.
For questions about code structure, ask the map before searching files.

First: call map_status once per session, and again before relying on a count or a ""nothing uses
this"". If the map is behind or dirty, refresh it (a green build with the hook on, or extract) or
say the answer came from a stale map.

Which tool:
- Find a symbol by name → symbol_search (keys from solutions). VB constructors are all named New:
  search the class, never the constructor by the class's name.
- What a symbol contains, touches, and where its partial parts are → symbol_detail.
- Who calls or uses a member → references.
- Who constructs or uses a type; is it safe to rename or remove → type_usages (read fromOutside).
- What nothing references → orphans. Unreferenced is not dead.
- Was it renamed; what became of a retired id → rename_candidates (proposals, never applied).

Prove, don't grep: a claim that nothing / exactly N / only X calls, constructs or uses Y is backed
by references or type_usages against a current map, naming the tool and the count. A ""nothing""
still misses Handles wiring, Overrides, interface dispatch and reflection — say so.

Use text search for what the map does not hold: comments, strings, markdown, SQL, configuration,
unmapped files, runtime behaviour, history. Bare uses of framework members (vbCrLf, Now) are not
recorded. C# is not mapped yet. Symbol ids change when a signature changes: re-find, do not carry
ids forward."

End Module

' File: SymbolKind.vb
' Project: CodeMem.Core
' Description: The kinds of symbol the map records, one per schema kind text.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' The symbol kinds the schema's <c>code_symbols.kind</c> CHECK admits. Text form in <see cref="SymbolKindNames"/>.
''' </summary>
Public Enum SymbolKind
    ''' <summary>A namespace declared by at least one Namespace block.</summary>
    Namespace_
    ''' <summary>A class.</summary>
    Class_
    ''' <summary>A module.</summary>
    Module_
    ''' <summary>A structure.</summary>
    Structure_
    ''' <summary>An interface.</summary>
    Interface_
    ''' <summary>An enum type.</summary>
    Enum_
    ''' <summary>A member of an enum.</summary>
    EnumMember
    ''' <summary>A delegate type.</summary>
    Delegate_
    ''' <summary>A method (Sub, Function, operator, Declare).</summary>
    Method
    ''' <summary>A constructor (Sub New).</summary>
    Constructor
    ''' <summary>A property, including a WithEvents member.</summary>
    Property_
    ''' <summary>A field.</summary>
    Field
    ''' <summary>An event.</summary>
    Event_
    ''' <summary>The synthetic per-project row.</summary>
    Project
End Enum

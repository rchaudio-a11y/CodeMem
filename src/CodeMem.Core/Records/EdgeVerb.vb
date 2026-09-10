' File: EdgeVerb.vb
' Project: CodeMem.Core
' Description: The eight relationship verbs the map records.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' The relationship verbs the schema's <c>code_edges.verb</c> CHECK admits. Text form in <see cref="EdgeVerbNames"/>.
''' </summary>
Public Enum EdgeVerb
    ''' <summary>Symbol to its containing symbol.</summary>
    PartOf
    ''' <summary>Invocation, object creation or member access to the bound member.</summary>
    Calls
    ''' <summary>Declaration to a type named in an As clause.</summary>
    Uses
    ''' <summary>Type to interface; member to interface member.</summary>
    Implements_
    ''' <summary>Type to base type from Inherits.</summary>
    Extends
    ''' <summary>Top-level type to namespace named by a file-level Imports.</summary>
    Imports_
    ''' <summary>Project to referenced project.</summary>
    DependsOn
    ''' <summary>Handler method to event, from a Handles clause or an AddHandler statement.</summary>
    Handles_
End Enum

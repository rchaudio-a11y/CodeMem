' File: ObservedPart.vb
' Project: CodeMem.Core
' Description: One declaring reference of an observed symbol, as staged in memory.
' Author: RCH Automation LLC
' Created: 2026-09-09

''' <summary>
''' A staged declaring part: where it is and the hash of its token text.
''' </summary>
Public Class ObservedPart

    ''' <summary>Location of the part node (the whole block for block declarations).</summary>
    Public Property Location As SourceLocation

    ''' <summary>SHA-256 hex of <see cref="HashInput"/>.</summary>
    Public Property PartHash As String

    ''' <summary>
    ''' The token text the part hash was computed over. Kept in staging only so a body hash can be recomputed
    ''' after namespaces merge across compilations (FR-014); never persisted.
    ''' </summary>
    Public Property HashInput As Byte()

    ''' <summary>Location of the identifier token that names the symbol in this part (the symbol row's evidence, FR-018).</summary>
    Public Property IdentifierLocation As SourceLocation

End Class

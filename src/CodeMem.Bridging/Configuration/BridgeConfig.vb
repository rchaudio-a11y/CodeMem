' File: BridgeConfig.vb
' Project: CodeMem.Bridging
' Description: The typed contents of bridge.config.json (contracts/cli-config-hook.md §2; spec Q8, research R49).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' One read of the configuration file: the two paths, the optional extractor path, the two gates and where it was read from. Never cached.
''' </summary>
Public Class BridgeConfig

    ''' <summary>The map file.</summary>
    Public Property MapPath As String

    ''' <summary>memos.sqlite.</summary>
    Public Property StorePath As String

    ''' <summary>The extractor executable or dll, or Nothing for the default beside the bridge.</summary>
    Public Property ExtractorPath As String

    ''' <summary>extract.enabled; the verb's gate for every origin.</summary>
    Public Property ExtractEnabled As Boolean

    ''' <summary>extract.onGreenBuild; the trigger's gate, inert while ExtractEnabled is false.</summary>
    Public Property ExtractOnGreenBuild As Boolean

    ''' <summary>The file this read came from.</summary>
    Public Property SourcePath As String

End Class

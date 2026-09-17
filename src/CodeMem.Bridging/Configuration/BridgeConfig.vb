' File: BridgeConfig.vb
' Project: CodeMem.Bridging
' Description: The typed contents of bridge.config.json (005 contracts/cli-config-hook.md §2; spec Q8, research R49).
' Author: RCH Automation LLC
' Created: 2026-09-15
'
' 2026-09-17 (feature 005, T013): storePath is no longer read - a file carrying it is refused naming the key (FR-403). The StorePath property
' stays, never set, until T024 archives the last file that reads it (StoreAccess); it leaves with the archive.

''' <summary>
''' One read of the configuration file: the map path, the optional extractor path, the two gates and where it was read from. Never cached.
''' </summary>
Public Class BridgeConfig

    ''' <summary>The map file.</summary>
    Public Property MapPath As String

    ''' <summary>Never set since feature 005 (the bridge opens no store); removed at T024 with its last reader.</summary>
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

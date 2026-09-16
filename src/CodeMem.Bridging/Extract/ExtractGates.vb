' File: ExtractGates.vb
' Project: CodeMem.Bridging
' Description: The two gates, read per call and evaluated before target resolution: extract.enabled for every origin, extract.onGreenBuild for the green-build origin only and only after the first passed (FR-330; STOP 1 ruling 2; data-model §8).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' The one place a gate is consulted. A disabled verb reveals nothing about the registry.
''' </summary>
Public Module ExtractGates

    ''' <summary>
    ''' Checks the gates for an origin.
    ''' </summary>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="origin">The request's origin.</param>
    ''' <returns>The GateOff refusal naming the gate that stopped it, or Nothing when both pass.</returns>
    Public Function Check(config As BridgeConfig, origin As ExtractOrigin) As BridgeRefusal
        If Not config.ExtractEnabled Then Return GateOff("extract.enabled", config)
        If origin = ExtractOrigin.GreenBuild AndAlso Not config.ExtractOnGreenBuild Then Return GateOff("extract.onGreenBuild", config)
        Return Nothing
    End Function

    ''' <summary>
    ''' The name of the gate a GateOff refusal names, for the result's gate column.
    ''' </summary>
    ''' <param name="config">The configuration read for this call.</param>
    ''' <param name="origin">The request's origin.</param>
    ''' <returns>extract.enabled, extract.onGreenBuild, or passed.</returns>
    Public Function GateName(config As BridgeConfig, origin As ExtractOrigin) As String
        If Not config.ExtractEnabled Then Return "extract.enabled"
        If origin = ExtractOrigin.GreenBuild AndAlso Not config.ExtractOnGreenBuild Then Return "extract.onGreenBuild"
        Return "passed"
    End Function

    Private Function GateOff(gate As String, config As BridgeConfig) As BridgeRefusal
        Return BridgeRefusal.Named(BridgeRefusalKind.GateOff, New Dictionary(Of String, String)(StringComparer.Ordinal) From {{"gate", gate}, {"configPath", config.SourcePath}})
    End Function

End Module

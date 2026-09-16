' File: BridgeConfigFile.vb
' Project: CodeMem.Bridging
' Description: Reads bridge.config.json on every call, never caches it, and refuses a missing file or key by name (FR-307, spec Q8, research R49).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.IO
Imports System.Text.Json

''' <summary>
''' The one door for configuration. Unknown keys are ignored; a file that is not JSON is Unconfigured naming the file; the two gates
''' default to false when absent.
''' </summary>
Public Module BridgeConfigFile

    ''' <summary>
    ''' The default file: bridge.config.json beside the executable.
    ''' </summary>
    ''' <returns>The path.</returns>
    Public Function DefaultPath() As String
        Return Path.Combine(AppContext.BaseDirectory, "bridge.config.json")
    End Function

    ''' <summary>
    ''' Reads the file.
    ''' </summary>
    ''' <param name="path">The file, or Nothing for <see cref="DefaultPath"/>.</param>
    ''' <returns>The configuration.</returns>
    ''' <exception cref="BridgeRefusalException">Unconfigured: the file is missing or not JSON, or mapPath or storePath is missing.</exception>
    Public Function Load(path As String) As BridgeConfig
        Dim file As String = If(String.IsNullOrWhiteSpace(path), DefaultPath(), IO.Path.GetFullPath(path))
        If Not IO.File.Exists(file) Then Throw Unconfigured("file", file)
        Dim root As JsonElement
        Try
            Using document As JsonDocument = JsonDocument.Parse(IO.File.ReadAllText(file))
                root = document.RootElement.Clone()
            End Using
        Catch ex As JsonException
            Throw Unconfigured("json", file)
        End Try
        If root.ValueKind <> JsonValueKind.Object Then Throw Unconfigured("json", file)
        Dim config As BridgeConfig = New BridgeConfig With {.SourcePath = file}
        config.MapPath = RequiredString(root, "mapPath", file)
        config.StorePath = RequiredString(root, "storePath", file)
        config.ExtractorPath = OptionalString(root, "extractorPath")
        Dim extract As JsonElement
        If root.TryGetProperty("extract", extract) AndAlso extract.ValueKind = JsonValueKind.Object Then
            config.ExtractEnabled = OptionalBoolean(extract, "enabled")
            config.ExtractOnGreenBuild = OptionalBoolean(extract, "onGreenBuild")
        End If
        Return config
    End Function

    Private Function RequiredString(root As JsonElement, key As String, file As String) As String
        Dim value As String = OptionalString(root, key)
        If String.IsNullOrWhiteSpace(value) Then Throw Unconfigured(key, file)
        Return value
    End Function

    Private Function OptionalString(root As JsonElement, key As String) As String
        Dim value As JsonElement
        If root.TryGetProperty(key, value) AndAlso value.ValueKind = JsonValueKind.String Then Return value.GetString()
        Return Nothing
    End Function

    Private Function OptionalBoolean(root As JsonElement, key As String) As Boolean
        Dim value As JsonElement
        Return root.TryGetProperty(key, value) AndAlso value.ValueKind = JsonValueKind.True
    End Function

    Private Function Unconfigured(key As String, file As String) As BridgeRefusalException
        Return New BridgeRefusalException(BridgeRefusal.Named(BridgeRefusalKind.Unconfigured, New Dictionary(Of String, String)(StringComparer.Ordinal) From {{"key", key}, {"configPath", file}}))
    End Function

End Module

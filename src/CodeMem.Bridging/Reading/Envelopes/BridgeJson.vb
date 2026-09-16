' File: BridgeJson.vb
' Project: CodeMem.Bridging
' Description: The one JSON serialiser options: camelCase, every property always written (null included), not indented (contracts/tools.md; 058's rule).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.Text.Encodings.Web
Imports System.Text.Json

''' <summary>
''' Every envelope goes through <see cref="Serialize"/>; dictionary keys (the verbs) are written as they are.
''' </summary>
Public Module BridgeJson

    Private ReadOnly Options As JsonSerializerOptions = New JsonSerializerOptions With {
        .PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        .DictionaryKeyPolicy = Nothing,
        .DefaultIgnoreCondition = Serialization.JsonIgnoreCondition.Never,
        .WriteIndented = False,
        .Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping}

    ''' <summary>
    ''' Serialises an envelope.
    ''' </summary>
    ''' <typeparam name="T">The envelope type.</typeparam>
    ''' <param name="value">The envelope.</param>
    ''' <returns>The JSON text.</returns>
    Public Function Serialize(Of T)(value As T) As String
        Return JsonSerializer.Serialize(value, Options)
    End Function

    ''' <summary>
    ''' The current UTC time in ISO-8601 round-trip form, the readAtUtc of every envelope.
    ''' </summary>
    ''' <returns>The timestamp text.</returns>
    Public Function NowUtc() As String
        Return DateTime.UtcNow.ToString("o", Globalization.CultureInfo.InvariantCulture)
    End Function

End Module

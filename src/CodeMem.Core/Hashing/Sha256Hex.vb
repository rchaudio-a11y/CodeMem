' File: Sha256Hex.vb
' Project: CodeMem.Core
' Description: SHA-256 over a sequence of byte arrays, rendered as 64 lowercase hex characters (research R4).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.Security.Cryptography

''' <summary>
''' The one hashing door for part hashes, body hashes and the source digest.
''' </summary>
Public Module Sha256Hex

    ''' <summary>
    ''' Hashes the concatenation of the given byte arrays.
    ''' </summary>
    ''' <param name="bytes">The byte arrays, in order.</param>
    ''' <returns>64 lowercase hex characters.</returns>
    Public Function Compute(bytes As IEnumerable(Of Byte())) As String
        Using hash As IncrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256)
            For Each chunk As Byte() In bytes
                hash.AppendData(chunk)
            Next
            Return Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant()
        End Using
    End Function

End Module

' File: SourceDigest.vb
' Project: CodeMem.Extraction
' Description: The source digest: SHA-256 over every compiled input's path and normalized text (FR-005, research R5).
' Author: RCH Automation LLC
' Created: 2026-09-09

Imports System.Text
Imports CodeMem.Core

''' <summary>
''' Computes the primary provenance of a run.
''' </summary>
Public Module SourceDigest

    ''' <summary>
    ''' SHA-256 over, per input in order: UTF-8(path), NUL, UTF-8(text), NUL.
    ''' </summary>
    ''' <param name="inputs">The compiled inputs in digest order.</param>
    ''' <returns>64 lowercase hex characters.</returns>
    Public Function Compute(inputs As IEnumerable(Of CompiledInput)) As String
        Dim separator As Byte() = New Byte() {0}
        Dim chunks As List(Of Byte()) = New List(Of Byte())()
        For Each input As CompiledInput In inputs
            chunks.Add(Encoding.UTF8.GetBytes(input.RelativePath))
            chunks.Add(separator)
            chunks.Add(Encoding.UTF8.GetBytes(input.Text))
            chunks.Add(separator)
        Next
        Return Sha256Hex.Compute(chunks)
    End Function

End Module

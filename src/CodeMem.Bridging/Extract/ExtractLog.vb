' File: ExtractLog.vb
' Project: CodeMem.Bridging
' Description: The append-only extract.log beside the executable, one tab-separated line per invocation, always on, never a reason to stop (FR-351; contracts/cli-config-hook.md §5; INC3).
' Author: RCH Automation LLC
' Created: 2026-09-15

Imports System.IO
Imports System.Threading

''' <summary>
''' Beside the executable whatever --config names (AppContext.BaseDirectory). Columns: UTC time; origin; target as given; resolved key or
''' refusal kind; gate outcome; exit code, a dash or timeout; the line verbatim with newlines and tabs removed. Reads are never logged.
''' </summary>
Public Module ExtractLog

    Private ReadOnly Gate As Object = New Object()

    ''' <summary>
    ''' The log file's path.
    ''' </summary>
    ''' <returns>extract.log beside the executable.</returns>
    Public Function LogPath() As String
        Return Path.Combine(AppContext.BaseDirectory, "extract.log")
    End Function

    ''' <summary>
    ''' Appends one line; three brief retries when another process holds the file; never throws.
    ''' </summary>
    ''' <param name="columns">The seven columns.</param>
    ''' <returns>Whether the line was written.</returns>
    Public Function Append(ParamArray columns As String()) As LogOutcome
        Dim cleaned As List(Of String) = New List(Of String)()
        For Each column As String In columns
            cleaned.Add(If(column, "-").Replace(vbCr, " ").Replace(vbLf, " ").Replace(vbTab, " "))
        Next
        Dim line As String = String.Join(vbTab, cleaned) & Environment.NewLine
        Dim lastError As String = Nothing
        For attempt As Integer = 1 To 3
            Try
                SyncLock Gate
                    Using stream As FileStream = New FileStream(LogPath(), FileMode.Append, FileAccess.Write, FileShare.Read)
                        Using writer As StreamWriter = New StreamWriter(stream, New Text.UTF8Encoding(False))
                            writer.Write(line)
                        End Using
                    End Using
                End SyncLock
                Return New LogOutcome With {.Logged = True}
            Catch ex As IOException
                lastError = ex.Message
                Thread.Sleep(50 * attempt)
            Catch ex As UnauthorizedAccessException
                lastError = ex.Message
                Thread.Sleep(50 * attempt)
            End Try
        Next
        Return New LogOutcome With {.Logged = False, .Reason = "extract.log could not be written at '" & LogPath() & "': " & lastError}
    End Function

End Module

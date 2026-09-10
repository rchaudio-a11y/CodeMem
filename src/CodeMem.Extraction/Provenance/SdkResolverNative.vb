' File: SdkResolverNative.vb
' Project: CodeMem.Extraction
' Description: The P/Invoke surface of the .NET host's SDK resolver (hostfxr_resolve_sdk2, hostfxr_set_error_writer) and the host lookup (research R21).
' Author: RCH Automation LLC
' Created: 2026-09-10

Imports System.Diagnostics
Imports System.IO
Imports System.Runtime.InteropServices

''' <summary>
''' Delegates matching hostfxr's C exports (cdecl; strings are char_t*, UTF-16 on Windows and UTF-8 elsewhere, marshalled as IntPtr through
''' <see cref="ToNative"/> and <see cref="FromNative"/>), plus where to find the host. No package; hostfxr is the .NET host itself (Article XV).
''' </summary>
Public Module SdkResolverNative

    ''' <summary>hostfxr_resolve_sdk2_result_fn: receives (key, value) pairs; key 0 is the resolved SDK directory.</summary>
    <UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet:=CharSet.Unicode)>
    Public Delegate Sub ResultFn(key As Integer, value As IntPtr)

    ''' <summary>hostfxr_error_writer_fn: receives one line of the host's error text.</summary>
    <UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet:=CharSet.Unicode)>
    Public Delegate Sub ErrorWriterFn(message As IntPtr)

    ''' <summary>hostfxr_resolve_sdk2(exe_dir, working_dir, flags, result): 0 on success; 0x8000809B when no compatible SDK exists.</summary>
    <UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet:=CharSet.Unicode)>
    Public Delegate Function ResolveSdk2(exeDir As IntPtr, workingDir As IntPtr, flags As Integer, result As ResultFn) As Integer

    ''' <summary>hostfxr_set_error_writer(writer): installs a writer (Nothing restores the default) and returns the previous one.</summary>
    <UnmanagedFunctionPointer(CallingConvention.Cdecl)>
    Public Delegate Function SetErrorWriter(writer As ErrorWriterFn) As IntPtr

    ''' <summary>
    ''' The hostfxr library to load: the hostfxr module of the current process; else the highest host/fxr/&lt;version&gt;/ under
    ''' DOTNET_ROOT; else under the directory of the dotnet executable on PATH.
    ''' </summary>
    ''' <returns>The full path of the hostfxr library, or Nothing when no host can be found.</returns>
    Public Function LocateHostfxr() As String
        For Each m As ProcessModule In Process.GetCurrentProcess().Modules
            If m.ModuleName.StartsWith("hostfxr", StringComparison.OrdinalIgnoreCase) OrElse m.ModuleName.StartsWith("libhostfxr", StringComparison.OrdinalIgnoreCase) Then
                Return m.FileName
            End If
        Next
        Dim root As String = Environment.GetEnvironmentVariable("DOTNET_ROOT")
        If Not String.IsNullOrEmpty(root) Then
            Dim found As String = HighestHostfxrUnder(root)
            If found IsNot Nothing Then Return found
        End If
        Dim onPath As String = DotnetOnPath()
        If onPath IsNot Nothing Then Return HighestHostfxrUnder(Path.GetDirectoryName(onPath))
        Return Nothing
    End Function

    ''' <summary>
    ''' The dotnet root of a hostfxr path: three directories above the library (&lt;root&gt;/host/fxr/&lt;version&gt;/hostfxr.*).
    ''' </summary>
    ''' <param name="hostfxrPath">The library path.</param>
    ''' <returns>The root directory (hostfxr's exe_dir argument).</returns>
    Public Function DotnetRootOf(hostfxrPath As String) As String
        Return Directory.GetParent(Path.GetDirectoryName(hostfxrPath)).Parent.Parent.FullName
    End Function

    ''' <summary>
    ''' Allocates a native char_t* copy of a string (UTF-16 on Windows, UTF-8 elsewhere). Free with <see cref="Marshal.FreeCoTaskMem"/>.
    ''' </summary>
    ''' <param name="text">The text.</param>
    ''' <returns>The native pointer.</returns>
    Public Function ToNative(text As String) As IntPtr
        If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then Return Marshal.StringToCoTaskMemUni(text)
        Return Marshal.StringToCoTaskMemUTF8(text)
    End Function

    ''' <summary>
    ''' Reads a native char_t* the host owns (UTF-16 on Windows, UTF-8 elsewhere).
    ''' </summary>
    ''' <param name="pointer">The pointer; IntPtr.Zero reads as Nothing.</param>
    ''' <returns>The managed string.</returns>
    Public Function FromNative(pointer As IntPtr) As String
        If pointer = IntPtr.Zero Then Return Nothing
        If RuntimeInformation.IsOSPlatform(OSPlatform.Windows) Then Return Marshal.PtrToStringUni(pointer)
        Return Marshal.PtrToStringUTF8(pointer)
    End Function

    Private Function HighestHostfxrUnder(root As String) As String
        Dim fxr As String = Path.Combine(root, "host", "fxr")
        If Not Directory.Exists(fxr) Then Return Nothing
        Dim best As String = Nothing
        Dim bestVersion As Version = Nothing
        For Each dir As String In Directory.GetDirectories(fxr)
            Dim candidate As String = Nothing
            For Each name As String In New String() {"hostfxr.dll", "libhostfxr.so", "libhostfxr.dylib"}
                Dim file As String = Path.Combine(dir, name)
                If IO.File.Exists(file) Then candidate = file
            Next
            If candidate Is Nothing Then Continue For
            Dim parsed As Version = Nothing
            Dim leaf As String = Path.GetFileName(dir)
            Dim numeric As String = If(leaf.IndexOf("-"c) >= 0, leaf.Substring(0, leaf.IndexOf("-"c)), leaf)
            If Not Version.TryParse(numeric, parsed) Then parsed = New Version(0, 0)
            If bestVersion Is Nothing OrElse parsed > bestVersion OrElse (parsed = bestVersion AndAlso String.CompareOrdinal(leaf, Path.GetFileName(Path.GetDirectoryName(best))) > 0) Then
                best = candidate
                bestVersion = parsed
            End If
        Next
        Return best
    End Function

    Private Function DotnetOnPath() As String
        Dim pathVariable As String = Environment.GetEnvironmentVariable("PATH")
        If String.IsNullOrEmpty(pathVariable) Then Return Nothing
        Dim names As String() = If(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), New String() {"dotnet.exe"}, New String() {"dotnet"})
        For Each dir As String In pathVariable.Split(Path.PathSeparator)
            If dir.Length = 0 Then Continue For
            For Each name As String In names
                Dim candidate As String = Path.Combine(dir, name)
                If File.Exists(candidate) Then Return Path.GetFullPath(candidate)
            Next
        Next
        Return Nothing
    End Function

End Module

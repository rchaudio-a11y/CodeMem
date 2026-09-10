' File: SdkVersion.vb
' Project: CodeMem.Extraction
' Description: The sdk_version stamp: the .NET SDK the host resolver selects for the solution directory, read in-process (FR-109, research R21).
' Author: RCH Automation LLC
' Created: 2026-09-10

Imports System.IO
Imports System.Runtime.InteropServices

''' <summary>
''' Calls hostfxr_resolve_sdk2 on the host already in the process with exe_dir = the dotnet root and working_dir = the solution's base
''' directory, honouring global.json exactly as dotnet --version does there, without launching a process (Article XV; research R21).
''' </summary>
Public Module SdkVersion

    ''' <summary>
    ''' Resolves the SDK version for a directory: the leaf name of the directory hostfxr reports under result key 0 (e.g. 10.0.401).
    ''' The host's error text is captured through hostfxr_set_error_writer and folded into the exception when nothing resolves.
    ''' </summary>
    ''' <param name="workingDirectory">The solution's base directory (where global.json lookup starts).</param>
    ''' <returns>The version text, never empty.</returns>
    ''' <exception cref="SdkResolutionException">No host found, the resolver returned non-zero, or it reported no SDK directory.</exception>
    Public Function Resolve(workingDirectory As String) As String
        Dim hostfxrPath As String = SdkResolverNative.LocateHostfxr()
        If hostfxrPath Is Nothing Then
            Throw New SdkResolutionException("no .NET host found: hostfxr is not loaded in this process, DOTNET_ROOT is unset and dotnet is not on PATH")
        End If
        Dim root As String = SdkResolverNative.DotnetRootOf(hostfxrPath)
        Dim library As IntPtr = NativeLibrary.Load(hostfxrPath)
        Try
            Dim resolveSdk As SdkResolverNative.ResolveSdk2 = Marshal.GetDelegateForFunctionPointer(Of SdkResolverNative.ResolveSdk2)(NativeLibrary.GetExport(library, "hostfxr_resolve_sdk2"))
            Dim setWriter As SdkResolverNative.SetErrorWriter = Marshal.GetDelegateForFunctionPointer(Of SdkResolverNative.SetErrorWriter)(NativeLibrary.GetExport(library, "hostfxr_set_error_writer"))
            Dim hostText As List(Of String) = New List(Of String)()
            Dim sdkDirectory As String = Nothing
            Dim writer As SdkResolverNative.ErrorWriterFn = Sub(message As IntPtr) hostText.Add(SdkResolverNative.FromNative(message))
            Dim onResult As SdkResolverNative.ResultFn = Sub(key As Integer, value As IntPtr)
                                                             If key = 0 Then sdkDirectory = SdkResolverNative.FromNative(value)
                                                         End Sub
            Dim exeDir As IntPtr = SdkResolverNative.ToNative(root)
            Dim workingDir As IntPtr = SdkResolverNative.ToNative(workingDirectory)
            Dim rc As Integer
            setWriter(writer)
            Try
                rc = resolveSdk(exeDir, workingDir, 0, onResult)
            Finally
                setWriter(Nothing)
                Marshal.FreeCoTaskMem(exeDir)
                Marshal.FreeCoTaskMem(workingDir)
                GC.KeepAlive(writer)
                GC.KeepAlive(onResult)
            End Try
            If rc <> 0 OrElse String.IsNullOrEmpty(sdkDirectory) Then
                Dim folded As String = String.Join(" | ", hostText.ConvertAll(Function(t As String) t.Replace(vbCrLf, " | ").Replace(vbLf, " | ").Trim()).FindAll(Function(t As String) t.Length > 0))
                Throw New SdkResolutionException("no .NET SDK resolved for " & workingDirectory & " (hostfxr_resolve_sdk2 returned 0x" & rc.ToString("X8") & ", host " & hostfxrPath & "): " & folded)
            End If
            Dim leaf As String = Path.GetFileName(sdkDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            If String.IsNullOrEmpty(leaf) Then Throw New SdkResolutionException("hostfxr reported an SDK directory without a version leaf: " & sdkDirectory)
            Return leaf
        Finally
            NativeLibrary.Free(library)
        End Try
    End Function

End Module

' File: IExtractorLauncher.vb
' Project: CodeMem.Bridging
' Description: The one seam of the extract door: the production launcher spawns the extractor, the test launcher records and scripts (research R54; Article XIII's zero-launch assertions).
' Author: RCH Automation LLC
' Created: 2026-09-15

''' <summary>
''' Runs one extraction to completion and reports what it returned.
''' </summary>
Public Interface IExtractorLauncher

    ''' <summary>
    ''' Runs the extractor.
    ''' </summary>
    ''' <param name="request">What to run.</param>
    ''' <returns>The outcome.</returns>
    Function Launch(request As LaunchRequest) As LaunchResult

End Interface

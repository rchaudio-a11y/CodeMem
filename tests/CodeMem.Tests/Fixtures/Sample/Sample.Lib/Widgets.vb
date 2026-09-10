Namespace Widgets

    Public Interface ISampleService
        Function Serve(input As String) As String
    End Interface

    Public Class AlphaService
        Implements ISampleService

        Public Function Serve(input As String) As String Implements ISampleService.Serve
            Return "Alpha:" & input
        End Function
    End Class

    Public Class BetaService
        Implements ISampleService

        Public Function Serve(input As String) As String Implements ISampleService.Serve
            Return "Beta:" & input
        End Function
    End Class

    Public Class BaseWidget
        Public Overridable Sub Describe()
            Console.WriteLine("widget")
        End Sub
    End Class

    Public Class MidWidget
        Inherits BaseWidget
    End Class

    Public Class LeafWidget
        Inherits MidWidget
    End Class

End Namespace

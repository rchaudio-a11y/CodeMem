Public Class OverloadSet
    Public Sub Run(x As Integer)
        Console.WriteLine(x)
    End Sub

    Public Sub Run(x As String)
        Console.WriteLine(x)
    End Sub
End Class

Imports System.Text

Public Class Consumer
    Public Sub New()
    End Sub

    Public Function Build() As String
        Dim w As Widgets.LeafWidget = New Widgets.LeafWidget()
        Dim sb As StringBuilder = New StringBuilder()
        sb.Append(w.ToString())
        Return sb.ToString()
    End Function
End Class

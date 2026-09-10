' Fixture totals (T026): Handles items = 4, AddHandler statements = 1, handler methods = 4.
Public Class MainForm

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        AddHandler Timer1.Tick, AddressOf OnTick
    End Sub

    Private Sub Buttons_Click(sender As Object, e As EventArgs) Handles Button1.Click, Button2.Click
        MessageBox.Show("clicked")
    End Sub

    Private Sub MainForm_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        Timer1.Start()
    End Sub

    Private Sub OnTick(sender As Object, e As EventArgs)
        Timer1.Stop()
    End Sub

End Class

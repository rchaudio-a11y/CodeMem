<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainForm
    Inherits System.Windows.Forms.Form

    Private components As System.ComponentModel.IContainer

    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.Button1 = New Button()
        Me.Button2 = New Button()
        Me.Timer1 = New Timer(Me.components)
        Me.SuspendLayout()
        Me.Button1.Location = New Point(12, 12)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New Size(75, 23)
        Me.Button1.Text = "One"
        Me.Button2.Location = New Point(12, 41)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New Size(90, 23)
        Me.Button2.Text = "Two"
        Me.Timer1.Interval = 1000
        Me.ClientSize = New Size(200, 100)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.Button2)
        Me.Name = "MainForm"
        Me.Text = "Sample"
        Me.ResumeLayout(False)
    End Sub

    Friend WithEvents Button1 As Button
    Friend WithEvents Button2 As Button
    Friend WithEvents Timer1 As Timer

End Class

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormTool
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        ToolStrip1 = New ToolStrip()
        PictureBox1 = New PictureBox()
        CType(PictureBox1, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' ToolStrip1
        ' 
        ToolStrip1.Location = New Point(0, 0)
        ToolStrip1.Name = "ToolStrip1"
        ToolStrip1.RenderMode = ToolStripRenderMode.System
        ToolStrip1.Size = New Size(848, 25)
        ToolStrip1.TabIndex = 0
        ToolStrip1.Text = "ToolStrip1"
        ' 
        ' PictureBox1
        ' 
        PictureBox1.BackColor = Color.Black
        PictureBox1.Dock = DockStyle.Fill
        PictureBox1.Location = New Point(0, 25)
        PictureBox1.Name = "PictureBox1"
        PictureBox1.Size = New Size(848, 665)
        PictureBox1.SizeMode = PictureBoxSizeMode.Zoom
        PictureBox1.TabIndex = 1
        PictureBox1.TabStop = False
        ' 
        ' FormTool
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(848, 690)
        Controls.Add(PictureBox1)
        Controls.Add(ToolStrip1)
        Name = "FormTool"
        Text = "Form1"
        CType(PictureBox1, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents PictureBox1 As PictureBox

End Class

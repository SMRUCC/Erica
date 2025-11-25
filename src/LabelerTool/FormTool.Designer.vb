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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(FormTool))
        ToolStrip1 = New ToolStrip()
        ToolStripButton1 = New ToolStripButton()
        PictureBox1 = New PictureBox()
        StatusStrip1 = New StatusStrip()
        ToolStripStatusLabel1 = New ToolStripStatusLabel()
        btnClear = New ToolStripButton()
        btnCompletePolygon = New ToolStripButton()
        ToolStripSeparator1 = New ToolStripSeparator()
        txtLabel = New ToolStripTextBox()
        ToolStrip1.SuspendLayout()
        CType(PictureBox1, ComponentModel.ISupportInitialize).BeginInit()
        StatusStrip1.SuspendLayout()
        SuspendLayout()
        ' 
        ' ToolStrip1
        ' 
        ToolStrip1.Items.AddRange(New ToolStripItem() {ToolStripButton1, ToolStripSeparator1, btnCompletePolygon, btnClear, txtLabel})
        ToolStrip1.Location = New Point(0, 0)
        ToolStrip1.Name = "ToolStrip1"
        ToolStrip1.RenderMode = ToolStripRenderMode.System
        ToolStrip1.Size = New Size(848, 25)
        ToolStrip1.TabIndex = 0
        ToolStrip1.Text = "ToolStrip1"
        ' 
        ' ToolStripButton1
        ' 
        ToolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Image
        ToolStripButton1.Image = CType(resources.GetObject("ToolStripButton1.Image"), Image)
        ToolStripButton1.ImageTransparentColor = Color.Magenta
        ToolStripButton1.Name = "ToolStripButton1"
        ToolStripButton1.Size = New Size(23, 22)
        ToolStripButton1.Text = "Open Cell Table"
        ' 
        ' PictureBox1
        ' 
        PictureBox1.BackColor = Color.Black
        PictureBox1.BackgroundImageLayout = ImageLayout.Zoom
        PictureBox1.Dock = DockStyle.Fill
        PictureBox1.Location = New Point(0, 25)
        PictureBox1.Name = "PictureBox1"
        PictureBox1.Size = New Size(848, 643)
        PictureBox1.SizeMode = PictureBoxSizeMode.Zoom
        PictureBox1.TabIndex = 1
        PictureBox1.TabStop = False
        ' 
        ' StatusStrip1
        ' 
        StatusStrip1.Items.AddRange(New ToolStripItem() {ToolStripStatusLabel1})
        StatusStrip1.Location = New Point(0, 668)
        StatusStrip1.Name = "StatusStrip1"
        StatusStrip1.Size = New Size(848, 22)
        StatusStrip1.TabIndex = 2
        StatusStrip1.Text = "StatusStrip1"
        ' 
        ' ToolStripStatusLabel1
        ' 
        ToolStripStatusLabel1.Name = "ToolStripStatusLabel1"
        ToolStripStatusLabel1.Size = New Size(119, 17)
        ToolStripStatusLabel1.Text = "ToolStripStatusLabel1"
        ' 
        ' btnClear
        ' 
        btnClear.DisplayStyle = ToolStripItemDisplayStyle.Image
        btnClear.Image = CType(resources.GetObject("btnClear.Image"), Image)
        btnClear.ImageTransparentColor = Color.Magenta
        btnClear.Name = "btnClear"
        btnClear.Size = New Size(23, 22)
        btnClear.Text = "Clear"
        ' 
        ' btnCompletePolygon
        ' 
        btnCompletePolygon.DisplayStyle = ToolStripItemDisplayStyle.Image
        btnCompletePolygon.Image = CType(resources.GetObject("btnCompletePolygon.Image"), Image)
        btnCompletePolygon.ImageTransparentColor = Color.Magenta
        btnCompletePolygon.Name = "btnCompletePolygon"
        btnCompletePolygon.Size = New Size(23, 22)
        btnCompletePolygon.Text = "Complete Polygon"
        ' 
        ' ToolStripSeparator1
        ' 
        ToolStripSeparator1.Name = "ToolStripSeparator1"
        ToolStripSeparator1.Size = New Size(6, 25)
        ' 
        ' txtLabel
        ' 
        txtLabel.Name = "txtLabel"
        txtLabel.Size = New Size(100, 25)
        ' 
        ' FormTool
        ' 
        AutoScaleDimensions = New SizeF(7F, 15F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(848, 690)
        Controls.Add(PictureBox1)
        Controls.Add(StatusStrip1)
        Controls.Add(ToolStrip1)
        Name = "FormTool"
        Text = "Form1"
        ToolStrip1.ResumeLayout(False)
        ToolStrip1.PerformLayout()
        CType(PictureBox1, ComponentModel.ISupportInitialize).EndInit()
        StatusStrip1.ResumeLayout(False)
        StatusStrip1.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents ToolStripButton1 As ToolStripButton
    Friend WithEvents StatusStrip1 As StatusStrip
    Friend WithEvents ToolStripStatusLabel1 As ToolStripStatusLabel
    Friend WithEvents btnClear As ToolStripButton
    Friend WithEvents btnCompletePolygon As ToolStripButton
    Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
    Friend WithEvents txtLabel As ToolStripTextBox

End Class

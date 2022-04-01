
Imports Microsoft.VisualBasic.Data.ChartPlots.Graphic.Canvas
Imports Microsoft.VisualBasic.Imaging.Driver
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.SingleCell.STdeconvolve
Imports SMRUCC.Rsharp.Runtime
Imports SMRUCC.Rsharp.Runtime.Internal.Object
Imports SparoMx

<Package("singleCell")>
Public Module singleCells

    Sub Main()
        Call Internal.generic.add("plot", GetType(Deconvolve), AddressOf plotLDA)
    End Sub

    Public Function plotLDA(lda As Deconvolve, args As list, env As Environment) As Object
        Dim theme As New Theme With {
            .background = "white"
        }
        Dim app As New LDAdeconvPlot(lda, theme)
        Dim size As String = InteropArgumentHelper.getSize(args!size, env, "2700,2100")
        Dim driver As Drivers = env.getDriver
        Dim ppi As Integer = args.getValue("ppi", env, [default]:=300)

        Return app.Plot(size, ppi, driver)
    End Function
End Module

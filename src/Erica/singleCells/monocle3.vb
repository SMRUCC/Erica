
Imports Erica.Analysis.SingleCell.Monocle3
Imports Microsoft.VisualBasic.CommandLine.Reflection
Imports Microsoft.VisualBasic.Scripting.MetaData
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

<Package("monocle3")>
Module monocle3Tool

    <ExportAPI("new")>
    Public Function monocle3_options(Optional numPCA As Integer = 10,
                Optional umapDim As Integer = 3,
                Optional knnK As Integer = 15,
                Optional resolution As Double = 1.0,
                Optional useLeiden As Boolean = False,
                Optional useCache As Boolean = True,
                Optional overwriteCache As Boolean = False,
                Optional cacheDir As String = "./cache",
                Optional pseudoVeloEnabled As Boolean = True,
                Optional pseudoVeloWindow As Integer = 2,
                Optional pseudoVeloSpan As Double = 0.3,
                Optional useVelocityProjection As Boolean = True,
                Optional numHVGenes As Integer = 3000) As Monocle3Options

        Return New Monocle3Options With {
            .numPCA = numPCA,
            .umapDim = umapDim,
            .knnK = knnK,
            .resolution = resolution,
            .useLeiden = useLeiden,
            .useCache = useCache,
            .overwriteCache = overwriteCache,
            .cacheDir = cacheDir,
            .pseudoVeloEnabled = pseudoVeloEnabled,
            .pseudoVeloWindow = pseudoVeloWindow,
            .pseudoVeloSpan = pseudoVeloSpan,
            .useVelocityProjection = useVelocityProjection,
            .numHVGenes = numHVGenes
        }
    End Function

    <ExportAPI("cell_rank")>
    Public Function cell_rank(x As Matrix, opts As Monocle3Options) As Monocle3Result
        Return Monocle3.Run(x, opts)
    End Function

    <ExportAPI("hvgenes")>
    Public Function hvgenes(x As Monocle3Result) As String()

    End Function
End Module

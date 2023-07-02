Imports Microsoft.VisualBasic.CommandLine
Imports Microsoft.VisualBasic.Serialization.JSON
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.Analysis.SingleCell.STdeconvolve

Module Program

    Public Function Main(args As String()) As Integer
        Return GetType(Program).RunCLI(App.CommandLine, executeFile:=AddressOf runOnMatrix)
    End Function

    Private Function runOnMatrix(file As String, args As CommandLine) As Integer
        Dim exports_json As String = file.TrimSuffix & "-STdeconvolve.json"
        Dim deconv_csv As String = file.TrimSuffix & "-STdeconvolve.csv"
        Dim deconv_layers As String = file.TrimSuffix & "-cell_layers.csv"
        Dim iteration As Integer = args("--iteration") Or 150
        Dim layers As Integer = args("--layers") Or 4
        Dim top_genes As Integer = args("--top_genes") Or 1000
        Dim STdataset As Matrix = Matrix.LoadData(file)
        Dim corpus As STCorpus = STdataset.CreateSpatialDocuments
        Dim result = corpus.LDAModelling(layers, iterations:=iteration).Deconvolve(corpus, topGenes:=top_genes)
        Dim deconv = result.GetSingleCellExpressionMatrix(STdataset)

        Call result.GetJson.SaveTo(exports_json)
        Call deconv.SaveMatrix(deconv_csv, "spot_xy")
        Call result.theta.SaveMatrix(deconv_layers)

        Return 0
    End Function
End Module

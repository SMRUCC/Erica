Imports Microsoft.VisualBasic.CommandLine
Imports Microsoft.VisualBasic.Serialization.JSON
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.Analysis.SingleCell.STdeconvolve

Module Program

    Public Function Main(args As String()) As Integer
        Return GetType(Program).RunCLI(App.CommandLine, executeFile:=AddressOf runOnMatrix, executeEmpty:=AddressOf help)
    End Function

    Private Function help() As Integer
        Console.WriteLine($"{GetType(Program).Assembly.Location.BaseName} <spot_expression.csv> [--iteration <default=150> --layers <default=4> --top_genes <default=1000> --n_threads <default=8> --export <outputdir>]")
        Console.WriteLine($"")
        Console.WriteLine($"spot_expression.csv should be a table file in format of:")
        Console.WriteLine($"features in column and,")
        Console.WriteLine($"spot vector in rows")

        Return 0
    End Function

    Private Function runOnMatrix(file As String, args As CommandLine) As Integer
        Dim outputdir As String = args("--export")
        Dim exports_json As String = file.TrimSuffix & "-STdeconvolve.json"
        Dim deconv_csv As String = file.TrimSuffix & "-STdeconvolve.csv"
        Dim deconv_csv2 As String = file.TrimSuffix & "-STdeconvolve2.csv"
        Dim deconv_layers As String = file.TrimSuffix & "-cell_layers.csv"

        If Not outputdir.StringEmpty Then
            exports_json = outputdir & "/" & file.BaseName & "-STdeconvolve.json"
            deconv_csv = outputdir & "/" & file.BaseName & "-STdeconvolve.csv"
            deconv_csv2 = outputdir & "/" & file.BaseName & "-STdeconvolve2.csv"
            deconv_layers = outputdir & "/" & file.BaseName & "-cell_layers.csv"
        End If

        Dim iteration As Integer = args("--iteration") Or 150
        Dim layers As Integer = args("--layers") Or 4
        Dim top_genes As Integer = args("--top_genes") Or 1000
        Dim n_threads As Integer = args("--n_threads") Or 8
        Dim STdataset As Matrix = Matrix.LoadData(file)
        Dim corpus As STCorpus = STdataset.CreateSpatialDocuments
        Dim result As Deconvolve = corpus.LDAModelling(layers, iterations:=iteration, n_threads:=n_threads).Deconvolve(corpus, topGenes:=top_genes)
        Dim deconv = result.GetSingleCellExpressionMatrix(STdataset)
        Dim deconv2 = result.GetExpressionMatrix(STdataset)

        Call result.GetJson.SaveTo(exports_json)
        Call deconv.SaveMatrix(deconv_csv, "spot_xy")
        Call deconv2.SaveMatrix(deconv_csv2, "spot_xy")
        Call result.theta.SaveMatrix(deconv_layers)

        Return 0
    End Function
End Module

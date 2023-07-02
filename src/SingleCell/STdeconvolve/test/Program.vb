Imports Microsoft.VisualBasic.Serialization.JSON
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.Analysis.SingleCell.STdeconvolve

Module Program

    Const deconv_out = "\Erica\src\SingleCell\STdeconvolve\demo\HR2MSI mouse urinary bladder S096_top3.json"

    Sub Main(args As String())
        Call dumpMatrix()
        Call createDeconv()
    End Sub

    Sub createDeconv()
        Dim STdataset As Matrix = Matrix.LoadData("\Erica\src\SingleCell\demo\HR2MSI mouse urinary bladder S096_top3.csv")
        Dim corpus As STCorpus = STdataset.CreateSpatialDocuments
        Dim result = corpus.LDAModelling(4, iterations:=120).Deconvolve(corpus, topGenes:=300)

        Call result.GetJson.SaveTo(deconv_out)

        Pause()
    End Sub

    Sub dumpMatrix()
        Dim STdataset As Matrix = Matrix.LoadData("\Erica\src\SingleCell\demo\HR2MSI mouse urinary bladder S096_top3.csv")
        Dim data = deconv_out.LoadJSON(Of Deconvolve)
        Dim deconv = data.GetSingleCellExpressionMatrix(STdataset)

        Call deconv.SaveMatrix("\Erica\src\SingleCell\STdeconvolve\demo\HR2MSI mouse urinary bladder S096-deconv.csv", "spot_xy")
        Call data.theta.SaveMatrix(deconv_out.ChangeSuffix("csv"))
        Call Pause()
    End Sub
End Module

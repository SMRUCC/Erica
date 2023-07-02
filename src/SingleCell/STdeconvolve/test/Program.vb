Imports Microsoft.VisualBasic.Data.NLP.LDA
Imports Microsoft.VisualBasic.Serialization.JSON
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports SMRUCC.genomics.Analysis.SingleCell.STdeconvolve

Module Program

    Const deconv_out = "\Erica\src\SingleCell\STdeconvolve\demo\HR2MSI mouse urinary bladder S096_top3.json"

    Sub Main(args As String())
        Dim STdataset As Matrix = Matrix.LoadData("\Erica\src\SingleCell\demo\HR2MSI mouse urinary bladder S096_top3.csv")
        Dim corpus As STCorpus = STdataset.CreateSpatialDocuments
        Dim result = corpus.LDAModelling(13).Deconvolve(corpus)

        Call result.GetJson.SaveTo(deconv_out)
        Call dumpMatrix()

        Pause()
    End Sub

    Sub dumpMatrix()
        Dim data = deconv_out.LoadJSON(Of Deconvolve)

        Call data.theta.SaveMatrix(deconv_out.ChangeSuffix("csv"))
        Call Pause()
    End Sub
End Module

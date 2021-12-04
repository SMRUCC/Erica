Imports System
Imports bgee
Imports Microsoft.VisualBasic.Serialization.JSON

Module Program
    Sub Main(args As String())

        Dim data = "E:\Erica\test\bgee.json".LoadJSON(Of DataSet)
        Dim calls = AdvancedCalls.ParseTable("P:\Bgee\Mus_musculus_expr_advanced_development.tsv").Take(10).ToArray

        Pause()
    End Sub
End Module

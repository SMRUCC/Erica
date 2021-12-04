Imports System
Imports bgee
Imports Microsoft.VisualBasic.Serialization.JSON

Module Program
    Sub Main(args As String())

        Dim data = "E:\Erica\test\bgee.json".LoadJSON(Of DataSet)
        Dim calls = AdvancedCalls.ParseTable("P:\Bgee\Mus_musculus_expr_advanced_development.tsv").Take(1000000).ToArray
        Dim Anatomical = calls.GroupBy(Function(g) g.gene_name).ToDictionary(Function(d) d.Key, Function(d) d.ToArray)

        Pause()
    End Sub
End Module

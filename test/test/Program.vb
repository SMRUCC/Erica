Imports System
Imports bgee
Imports Microsoft.VisualBasic.Serialization.JSON

Module Program
    Sub Main(args As String())

        Dim data = "E:\Erica\test\bgee.json".LoadJSON(Of DataSet)
        Dim calls = AdvancedCalls.ParseTable("P:\Bgee\Mus_musculus_expr_advanced_development.tsv").Take(50000).ToArray
        Dim Anatomical = calls.GroupBy(Function(g) g.anatomicalName.Replace("""", "'")).ToDictionary(Function(d) d.Key, Function(d) d.ToArray)
        Dim json As String = Anatomical.GetJson

        Call json.SaveTo("E:\Erica\test\small_parts.json")

        Pause()
    End Sub
End Module

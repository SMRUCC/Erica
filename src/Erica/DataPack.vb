Imports bgee
Imports Microsoft.VisualBasic.Data.IO.MessagePack.Serialization

Namespace DataPack

    Public Class AdvancedCallsSchema : Inherits SchemaProvider(Of AdvancedCalls)

        Protected Overrides Iterator Function GetObjectSchema() As IEnumerable(Of (obj As Type, schema As Dictionary(Of String, NilImplication)))
            Yield (GetType(AdvancedCalls), exprCalls)
            Yield (GetType(GeneExpression), expressionPack)
        End Function

        Private Function exprCalls() As Dictionary(Of String, NilImplication)
            Return {"geneID",
            "gene_name",
            "anatomicalID",
            "anatomicalName",
            "developmental_stageID",
            "developmental_stage",
            "expression",
            "call_quality",
            "expression_rank",
            "including_observed_data", "affymetrix As GeneExpression",
      "EST_data",
       "In_Situ",
       "RNASeq"}.ToDictionary(Function(key) key, Function(any) NilImplication.MemberDefault)
        End Function

        Private Function expressionPack() As Dictionary(Of String, NilImplication)
            Return {"data",
                "expression_high_quality",
                "expression_low_quality",
                "absence_high_quality",
                "absence_low_quality",
                "observed_data"}.ToDictionary(Function(key) key, Function(any) NilImplication.MemberDefault)
        End Function
    End Class

End Namespace

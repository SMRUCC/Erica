Imports bgee
Imports Microsoft.VisualBasic.Data.IO.MessagePack.Serialization
Imports SMRUCC.genomics.Analysis.HTS.GSEA

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

    Public Class BackgroundSchema : Inherits SchemaProvider(Of Background)

        Protected Overrides Iterator Function GetObjectSchema() As IEnumerable(Of (obj As Type, schema As Dictionary(Of String, NilImplication)))
            Yield (GetType(Background), model())
            Yield (GetType(Cluster), clusterModel())
            Yield (GetType(BackgroundGene), geneModel)
        End Function

        Private Shared Function model() As Dictionary(Of String, NilImplication)
            Return New Dictionary(Of String, NilImplication) From {
                {NameOf(Background.build), NilImplication.MemberDefault},
                {NameOf(Background.id), NilImplication.MemberDefault},
                {NameOf(Background.name), NilImplication.MemberDefault},
                {NameOf(Background.comments), NilImplication.MemberDefault},
                {NameOf(Background.size), NilImplication.MemberDefault},
                {NameOf(Background.clusters), NilImplication.MemberDefault}
            }
        End Function

        Private Shared Function clusterModel() As Dictionary(Of String, NilImplication)
            Return New Dictionary(Of String, NilImplication) From {
                {NameOf(Cluster.ID), NilImplication.MemberDefault},
                {NameOf(Cluster.names), NilImplication.MemberDefault},
                {NameOf(Cluster.description), NilImplication.MemberDefault},
                {NameOf(Cluster.members), NilImplication.MemberDefault}
            }
        End Function

        Private Shared Function geneModel() As Dictionary(Of String, NilImplication)
            Return New Dictionary(Of String, NilImplication) From {
                {NameOf(BackgroundGene.accessionID), NilImplication.MemberDefault},
                {NameOf(BackgroundGene.term_id), NilImplication.MemberDefault},
                {NameOf(BackgroundGene.name), NilImplication.MemberDefault},
                {NameOf(BackgroundGene.locus_tag), NilImplication.MemberDefault},
                {NameOf(BackgroundGene.alias), NilImplication.MemberDefault}
            }
        End Function
    End Class

End Namespace

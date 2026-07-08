Imports Microsoft.VisualBasic.Data.Framework.StorageProvider
Imports Microsoft.VisualBasic.Text

Public Class TSVParser

    ReadOnly headers As HeaderSchema
    ReadOnly filepath As String

    Sub New(filepath As String)
        Me.filepath = filepath
        Me.headers = New HeaderSchema(filepath.ReadFirstLine.Split(ASCII.TAB))
    End Sub

    Public Iterator Function ParseTable() As IEnumerable(Of AdvancedCalls)
        For Each line As String In filepath.LineIterators(tqdm:=True).Skip(1)
            Yield ParseLine(line.Split(ASCII.TAB))
        Next
    End Function

    Private Function ParseLine(tsv As String()) As AdvancedCalls
        Return New AdvancedCalls With {
            .geneID = tsv(0),
            .gene_name = tsv(1).Trim(""""c),
            .anatomicalID = tsv(2),
            .anatomicalName = tsv(3).Trim(""""c),
            .developmental_stageID = tsv(4),
            .developmental_stage = tsv(5).Trim(""""c),
            .expression = tsv(6),
            .call_quality = tsv(7),
            .expression_rank = Val(tsv(8)),
            .including_observed_data = tsv(9),
            .affymetrix = New GeneExpression With {
                .data = tsv(10),
                .expression_high_quality = Integer.Parse(tsv(11)),
                .expression_low_quality = Integer.Parse(tsv(12)),
                .absence_high_quality = Integer.Parse(tsv(13)),
                .absence_low_quality = Integer.Parse(tsv(14)),
                .observed_data = tsv(15)
            },
            .EST_data = New GeneExpression With {
                .data = tsv(16),
                .expression_high_quality = Integer.Parse(tsv(17)),
                .expression_low_quality = Integer.Parse(tsv(18)),
                .observed_data = tsv(19)
            },
            .In_Situ = New GeneExpression With {
                .data = tsv(20),
                .expression_high_quality = Integer.Parse(tsv(21)),
                .expression_low_quality = Integer.Parse(tsv(22)),
                .absence_high_quality = Integer.Parse(tsv(23)),
                .absence_low_quality = Integer.Parse(tsv(24)),
                .observed_data = tsv(25)
            },
            .RNASeq = New GeneExpression With {
                .data = tsv(26),
                .expression_high_quality = Integer.Parse(tsv(27)),
                .expression_low_quality = Integer.Parse(tsv(28)),
                .absence_high_quality = Integer.Parse(tsv(29)),
                .absence_low_quality = Integer.Parse(tsv(30)),
                .observed_data = tsv(31)
            }
        }
    End Function
End Class

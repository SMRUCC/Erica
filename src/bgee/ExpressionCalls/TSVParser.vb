Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Reflection
Imports Microsoft.VisualBasic.Data.Framework.StorageProvider
Imports Microsoft.VisualBasic.Text

Public Class TSVParser

    ReadOnly headers As HeaderSchema
    ReadOnly filepath As String

    <Column("Gene ID")> ReadOnly gene_id As Integer
    <Column("Gene name")> ReadOnly gene_name As Integer
    <Column("Anatomical entity ID")> ReadOnly anatomical_id As Integer
    <Column("Anatomical entity name")> ReadOnly anatomical_name As Integer
    <Column("Developmental stage ID")> ReadOnly develop_id As Integer
    <Column("Developmental stage name")> ReadOnly develop_stage As Integer
    <Column("Sex")> ReadOnly sex As Integer
    <Column("Strain")> ReadOnly strain As Integer

    <Column("Expression")> ReadOnly expression As Integer
    <Column("Call quality")> ReadOnly call_quality As Integer
    <Column("FDR")> ReadOnly fdr As Integer
    <Column("Expression score")> ReadOnly expression_score As Integer
    <Column("Expression rank")> ReadOnly expression_rank As Integer
    <Column("Including observed data")> ReadOnly include_observed_data As Integer
    <Column("Self observation count")> ReadOnly self_observation_count As Integer
    <Column("Descendant observation count")> ReadOnly descendant_observation_count As Integer

    <Column("Affymetrix expression")> ReadOnly affymetrix_offset As Integer
    <Column("EST expression")> ReadOnly EST_offset As Integer
    <Column("in situ hybridization expression")> ReadOnly Insitu_offset As Integer
    <Column("RNA-Seq expression")> ReadOnly rnaseq_offset As Integer
    <Column("single-cell RNA-Seq expression")> ReadOnly scrnaseq_offset As Integer

    Sub New(filepath As String)
        Static fields As Dictionary(Of String, String) = Me.GetType _
            .GetFields(bindingAttr:=BindingFlags.Instance Or BindingFlags.NonPublic) _
            .ToDictionary(Function(f) f.Name,
                          Function(f)
                              Return f.GetCustomAttribute(Of ColumnAttribute).Name
                          End Function)

        Me.filepath = filepath
        Me.headers = New HeaderSchema(filepath.ReadFirstLine.Split(ASCII.TAB))

        Me.gene_id = headers.GetOrdinal(fields(NameOf(TSVParser.gene_id)))
        Me.gene_name = headers.GetOrdinal(fields(NameOf(TSVParser.gene_name)))
        Me.anatomical_id = headers.GetOrdinal(fields(NameOf(TSVParser.anatomical_id)))
        Me.anatomical_name = headers.GetOrdinal(fields(NameOf(TSVParser.anatomical_name)))
        Me.develop_id = headers.GetOrdinal(fields(NameOf(TSVParser.develop_id)))
        Me.develop_stage = headers.GetOrdinal(fields(NameOf(TSVParser.develop_stage)))
        Me.sex = headers.GetOrdinal(fields(NameOf(TSVParser.sex)))
        Me.strain = headers.GetOrdinal(fields(NameOf(TSVParser.strain)))

        Me.expression = headers.GetOrdinal(fields(NameOf(TSVParser.expression)))
        Me.call_quality = headers.GetOrdinal(fields(NameOf(TSVParser.call_quality)))
        Me.fdr = headers.GetOrdinal(fields(NameOf(TSVParser.fdr)))
        Me.expression_score = headers.GetOrdinal(fields(NameOf(TSVParser.expression_score)))
        Me.expression_rank = headers.GetOrdinal(fields(NameOf(TSVParser.expression_rank)))
        Me.include_observed_data = headers.GetOrdinal(fields(NameOf(TSVParser.include_observed_data)))
        Me.self_observation_count = headers.GetOrdinal(fields(NameOf(TSVParser.self_observation_count)))
        Me.descendant_observation_count = headers.GetOrdinal(fields(NameOf(TSVParser.descendant_observation_count)))

        Me.affymetrix_offset = headers.GetOrdinal(fields(NameOf(TSVParser.affymetrix_offset)))
        Me.EST_offset = headers.GetOrdinal(fields(NameOf(TSVParser.EST_offset)))
        Me.Insitu_offset = headers.GetOrdinal(fields(NameOf(TSVParser.Insitu_offset)))
        Me.rnaseq_offset = headers.GetOrdinal(fields(NameOf(TSVParser.rnaseq_offset)))
        Me.scrnaseq_offset = headers.GetOrdinal(fields(NameOf(TSVParser.scrnaseq_offset)))
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

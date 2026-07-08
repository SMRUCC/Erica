Imports System.ComponentModel.DataAnnotations.Schema
Imports System.Reflection
Imports Microsoft.VisualBasic.ComponentModel.DataSourceModel
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
            .Where(Function(f)
                       Return f.GetCustomAttribute(Of ColumnAttribute) IsNot Nothing
                   End Function) _
            .ToDictionary(Function(f) f.Name,
                          Function(f)
                              Dim col As ColumnAttribute = f.GetCustomAttribute(Of ColumnAttribute)
                              Return col.Name
                          End Function)

        Me.filepath = filepath
        Me.headers = New HeaderSchema(filepath _
            .ReadFirstLine _
            .Split(ASCII.TAB) _
            .Select(Function(name)
                        Return name.Trim(""""c)
                    End Function))

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
        Dim line As New StringArrayPointer(tsv)

        Return New AdvancedCalls With {
            .geneID = line.ReadString(Me.gene_id, strip:=True),
            .gene_name = line.ReadString(Me.gene_name, strip:=True),
            .anatomicalID = line.ReadString(Me.anatomical_id, strip:=True),
            .anatomicalName = line.ReadString(Me.anatomical_name, strip:=True),
            .developmental_stageID = line.ReadString(Me.develop_id, strip:=True),
            .developmental_stage = line.ReadString(Me.develop_stage, strip:=True),
            .expression = line.ReadString(Me.expression, strip:=True),
            .call_quality = line.ReadString(Me.call_quality, strip:=True),
            .expression_rank = line.ReadDouble(Me.expression_rank),
            .including_observed_data = line.ReadString(Me.include_observed_data, strip:=True),
            .descendant_observation_count = line.ReadString(Me.descendant_observation_count, strip:=True),
            .expression_score = line.ReadDouble(Me.expression_score),
            .FDR = line.ReadDouble(Me.fdr),
            .self_observation_count = line.ReadInteger(Me.self_observation_count),
            .sex = line.ReadString(Me.sex, strip:=True),
            .strain = line.ReadString(Me.strain, strip:=True),
            .affymetrix = readGeneExpression(line, Me.affymetrix_offset),
            .EST_data = readGeneExpression(line, Me.EST_offset),
            .In_Situ = readGeneExpression(line, Me.Insitu_offset),
            .RNASeq = readGeneExpression(line, Me.rnaseq_offset),
            .SingleCellRNASeq = readGeneExpression(line, Me.scrnaseq_offset)
        }
    End Function

    Private Shared Function readGeneExpression(line As StringArrayPointer, offset As Integer) As GeneExpression
        Return New GeneExpression With {
            .data = line.ReadString(offset, strip:=True),
            .call_quality = line.ReadString(offset + 1, strip:=True),
            .FDR = line.ReadString(offset + 2, strip:=True),
            .expression_score = line.ReadString(offset + 3, strip:=True),
            .expression_rank = line.ReadString(offset + 4, strip:=True)
        }
    End Function
End Class

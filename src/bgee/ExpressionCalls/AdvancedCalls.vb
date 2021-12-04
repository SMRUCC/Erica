Imports Microsoft.VisualBasic.Text

Public Class AdvancedCalls

    Public Property geneID As String
    Public Property gene_name As String
    ''' <summary>
    ''' Anatomical entity ID
    ''' </summary>
    ''' <returns></returns>
    Public Property anatomicalID As String
    ''' <summary>
    ''' Anatomical entity name
    ''' </summary>
    ''' <returns></returns>
    Public Property anatomicalName As String
    ''' <summary>
    ''' Developmental stage ID *
    ''' </summary>
    ''' <returns></returns>
    Public Property developmental_stageID As String
    ''' <summary>
    ''' Developmental stage name *
    ''' </summary>
    ''' <returns></returns>
    Public Property developmental_stage As String
    Public Property expression As String
    Public Property call_quality As String
    Public Property expression_rank As Double
    Public Property including_observed_data As String
    Public Property affymetrix As GeneExpression
    Public Property EST_data As GeneExpression
    Public Property In_Situ As GeneExpression
    Public Property RNASeq As GeneExpression

    Public Overrides Function ToString() As String
        Return $"[{call_quality}] {gene_name}@{anatomicalName} = {expression_rank}"
    End Function

    Public Shared Iterator Function ParseTable(file As String) As IEnumerable(Of AdvancedCalls)
        For Each line As String In file.LineIterators.Skip(1)
            Yield ParseLine(line.Split(ASCII.TAB))
        Next
    End Function

    Private Shared Function ParseLine(tsv As String()) As AdvancedCalls
        Return New AdvancedCalls With {
            .geneID = tsv(0),
            .gene_name = tsv(1).Trim(""""c).Replace("""", "'"),
            .anatomicalID = tsv(2),
            .anatomicalName = tsv(3).Trim(""""c).Replace("""", "'"),
            .developmental_stageID = tsv(4),
            .developmental_stage = tsv(5).Trim(""""c).Replace("""", "'"),
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

Public Class GeneExpression

    Public Property data As String
    ''' <summary>
    ''' experiment count showing expression of this gene in 
    ''' this condition or in sub-conditions with a high quality
    ''' </summary>
    ''' <returns></returns>
    Public Property expression_high_quality As Integer
    ''' <summary>
    ''' experiment count showing expression of this gene in 
    ''' this condition or in sub-conditions with a low quality
    ''' </summary>
    ''' <returns></returns>
    Public Property expression_low_quality As Integer
    ''' <summary>
    ''' experiment count showing absence of expression of this 
    ''' gene in this condition or valid parent conditions with 
    ''' a high quality
    ''' </summary>
    ''' <returns></returns>
    Public Property absence_high_quality As Integer
    ''' <summary>
    ''' experiment count showing absence of expression of this 
    ''' gene in this condition or valid parent conditions with 
    ''' a low quality
    ''' </summary>
    ''' <returns></returns>
    Public Property absence_low_quality As Integer
    Public Property observed_data As String

End Class
Imports Microsoft.VisualBasic.Text

''' <summary>
''' 
''' </summary>
Public Class AdvancedCalls

    ''' <summary>
    ''' Unique identifier of the gene
    ''' </summary>
    ''' <returns></returns>
    Public Property geneID As String
    ''' <summary>
    ''' Name of the gene defined by Gene ID (column 1)
    ''' </summary>
    ''' <returns></returns>
    Public Property gene_name As String
    ''' <summary>
    ''' Anatomical entity ID, Unique identifier of the 
    ''' anatomical entity, from the Uberon ontology.
    ''' </summary>
    ''' <returns></returns>
    Public Property anatomicalID As String
    ''' <summary>
    ''' Anatomical entity name, Name of the anatomical entity 
    ''' defined by Anatomical entity ID (column 3)
    ''' </summary>
    ''' <returns></returns>
    Public Property anatomicalName As String
    ''' <summary>
    ''' Developmental stage ID *, Unique identifier of the 
    ''' developmental stage, from the Uberon ontology.
    ''' </summary>
    ''' <returns></returns>
    Public Property developmental_stageID As String
    ''' <summary>
    ''' Developmental stage name *, Name of the developmental 
    ''' stage defined by Developmental stage ID (column 5)
    ''' </summary>
    ''' <returns></returns>
    Public Property developmental_stage As String
    ''' <summary>
    ''' Call generated from all data types for the selected 
    ''' combination of condition parameters (anatomical or 
    ''' all conditions). Permitted values: 
    ''' 
    ''' 1. present
    ''' 2. absent
    ''' 
    ''' </summary>
    ''' <returns></returns>
    Public Property expression As String
    ''' <summary>
    ''' Call quality from all data types for the selected 
    ''' combination of condition parameters (anatomical or 
    ''' all conditions). Permitted values: 
    ''' 
    ''' 1. gold quality, 
    ''' 2. silver quality.
    ''' 
    ''' </summary>
    ''' <returns></returns>
    Public Property call_quality As String
    ''' <summary>
    ''' Rank score associated to the call. Rank scores of
    ''' expression calls are normalized across genes, 
    ''' conditions and species.
    ''' 
    ''' A low score means that the gene Is highly expressed 
    ''' In the condition.
    ''' </summary>
    ''' <returns></returns>
    Public Property expression_rank As Double
    Public Property expression_score As Double
    ''' <summary>
    ''' Permitted value: yes
    ''' 
    ''' Only calls which were actually seen In experimental 
    ''' data, at least once, are In this file.
    ''' </summary>
    ''' <returns></returns>
    Public Property including_observed_data As String
    Public Property affymetrix As GeneExpression
    Public Property EST_data As GeneExpression
    Public Property In_Situ As GeneExpression
    Public Property RNASeq As GeneExpression

    Public Overrides Function ToString() As String
        Return $"[{call_quality}] {gene_name}@{anatomicalName} = {expression_rank}"
    End Function

    Public Shared Iterator Function ParseSimpleTable(file As String, Optional quality As String = "gold quality") As IEnumerable(Of AdvancedCalls)
        Dim calls As AdvancedCalls

        If quality = "*" Then
            quality = Nothing
        End If

        For Each line As String In file.LineIterators.Skip(1)
            calls = ParseSimpleLine(line.Split(ASCII.TAB))

            If Not quality Is Nothing Then
                If calls.call_quality = quality Then
                    Yield calls
                End If
            Else
                Yield calls
            End If
        Next
    End Function

    Private Shared Function ParseSimpleLine(tsv As String()) As AdvancedCalls
        ' 0 Gene ID
        ' 1 "Gene name"
        ' 2 Anatomical entity ID
        ' 3 "Anatomical entity name"
        ' 4 Developmental stage ID
        ' 5 "Developmental stage name"
        ' 6 Sex
        ' 7 Strain
        ' 8 Expression
        ' 9 Call quality
        ' 10 FDR
        ' 11 Expression score
        ' 12 Expression rank
        Return New AdvancedCalls With {
            .geneID = tsv(0),
            .gene_name = Strings.Trim(tsv(1)).Trim(""""c),
            .anatomicalID = tsv(2),
            .anatomicalName = Strings.Trim(tsv(3)).Trim(""""c),
            .developmental_stageID = tsv(4),
            .developmental_stage = Strings.Trim(tsv(5)).Trim(""""c),
            .expression = tsv(8),
            .call_quality = tsv(9),
            .expression_rank = Double.Parse(tsv(12)),
            .expression_score = Double.Parse(tsv(11))
        }
    End Function

    Public Shared Iterator Function ParseTable(file As String) As IEnumerable(Of AdvancedCalls)
        For Each line As String In file.LineIterators.Skip(1)
            Yield ParseLine(line.Split(ASCII.TAB))
        Next
    End Function

    Private Shared Function ParseLine(tsv As String()) As AdvancedCalls
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

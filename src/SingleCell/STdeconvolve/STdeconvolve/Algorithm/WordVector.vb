Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.ApplicationServices.Terminal.ProgressBar.Tqdm
Imports Microsoft.VisualBasic.ComponentModel.Collection
Imports Microsoft.VisualBasic.ComponentModel.Ranges.Model
Imports Microsoft.VisualBasic.Language
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports SMRUCC.genomics.Analysis.HTS.DataFrame
Imports std = System.Math

Public Module WordVector

    ''' <summary>
    ''' 
    ''' </summary>
    ''' <param name="matrix">
    ''' pixels in row and genes in columns
    ''' </param>
    ''' <returns></returns>
    <Extension>
    Friend Function Documentaries(matrix As Matrix) As STCorpus
        Dim geneIds As String() = matrix.sampleID
        Dim sample As New STCorpus
        Dim document As New List(Of String)

        For Each pixel As DataFrameRow In matrix.expression
            For i As Integer = 0 To geneIds.Length - 1
                If pixel(i) > 0 Then
                    ' convert the unify levels as
                    ' document composition
                    document += geneIds(i).Replicate(CInt(pixel(i)))
                End If
            Next

            Call sample.addPixel(pixel.geneID, document)
            Call document.Clear()
        Next

        Return sample
    End Function

    ''' <summary>
    ''' removes genes that appears in less than 5% pixels or more than 95% pixels
    ''' </summary>
    ''' <param name="matrix"></param>
    ''' <param name="pmin"></param>
    ''' <param name="pmax"></param>
    ''' <returns></returns>
    <Extension>
    Friend Function GeneFilter(matrix As Matrix, pmin As Double, pmax As Double) As Index(Of String)
        Dim geneIds As New List(Of String)
        Dim totalGenes As Integer = matrix.sampleID.Length
        Dim totalPixels As Integer = matrix.size

        Call VBDebugger.EchoLine($"filter out features that less than {pmin * 100}% or greater than {pmax * 100}%...")

        For i As Integer = 0 To totalGenes - 1
            Dim v As Vector = matrix.sample(i)
            Dim zero As Integer = (v <= 0.0).Sum

            If zero / totalPixels >= 1 - pmin Then
                geneIds += matrix.sampleID(i)
            ElseIf (totalPixels - zero) / totalPixels >= pmax Then
                geneIds += matrix.sampleID(i)
            End If
        Next

        Call VBDebugger.EchoLine($"   - get {geneIds.Count} features to removes!")

        Return geneIds.Indexing
    End Function

    ''' <summary>
    ''' unify matrix by each feature columns
    ''' </summary>
    ''' <param name="matrix"></param>
    ''' <param name="unify"></param>
    ''' <returns></returns>
    <Extension>
    Friend Function UnifyMatrix(matrix As Matrix, unify As Integer, log As Boolean) As Matrix
        Dim unifyFactor As New DoubleRange(1, unify)
        Dim v As Vector

        ' make a copy of the new matrix
        matrix = New Matrix With {
            .expression = matrix.expression _
                .Select(Function(gi) New DataFrameRow(gi)) _
                .ToArray,
            .sampleID = matrix.sampleID.ToArray,
            .tag = matrix.tag
        }

        For Each i As Integer In TqdmWrapper.Range(0, matrix.sampleID.Length)
            v = matrix.sample(i)

            If log Then
                ' avoid negative value in count matrix unify procedure
                v = (From x As Double
                     In v
                     Let ln As Double = If(x <= 1, 0, std.Log(x))
                     Select ln).AsVector
            End If

            matrix.sample(i) = v.ScaleToRange(unifyFactor)
        Next

        Return matrix
    End Function
End Module

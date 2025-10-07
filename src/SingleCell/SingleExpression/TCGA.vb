Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.Data.Framework
Imports Microsoft.VisualBasic.Math.LinearAlgebra.Matrix
Imports Microsoft.VisualBasic.Math.Matrix.MatrixMarket
Imports Microsoft.VisualBasic.Text

''' <summary>
''' data reader for the TCGA dataset
''' </summary>
''' <remarks>
''' TCGA（The Cancer Genome Atlas）https://portal.gdc.cancer.gov/
''' </remarks>
Public Module TCGA

    ''' <summary>
    ''' Read expression matrix from a TCGA MTX dataset folder
    ''' </summary>
    ''' <param name="dataset">a folder path to the TCGA MTX dataset files, this folder should includes the data files:
    ''' 1. barcodes.tsv
    ''' 2. features.tsv
    ''' 3. matrix.mtx
    ''' </param>
    ''' <returns></returns>
    Public Function MTXReader(dataset As String) As DataFrame
        Dim barcodes As String() = $"{dataset}/barcodes.tsv".ReadAllLines
        Dim genes As TCGAGeneFeature() = TCGAGeneFeature.ParseFile($"{dataset}/features.tsv").ToArray
        Dim matrix As Double()() = MTXFormat.ReadMatrix($"{dataset}/matrix.mtx").ArrayPack
        Dim df As New DataFrame With {
            .description = $"TCGA dataset: {dataset}",
            .name = dataset.BaseName,
            .features = New Dictionary(Of String, FeatureVector),
            .rownames = barcodes
        }

        For i As Integer = 0 To genes.Length - 1
            Dim gene As TCGAGeneFeature = genes(i)
            Dim v As Double() = matrix(i)
        Next

        Return df
    End Function

End Module

Public Class TCGAGeneFeature

    Public Property gene_id As String
    Public Property gene_name As String
    Public Property type As String

    Public Overrides Function ToString() As String
        Return $"[{gene_id}] {gene_name}"
    End Function

    <MethodImpl(MethodImplOptions.AggressiveInlining)>
    Public Shared Function ParseFile(filepath As String) As IEnumerable(Of TCGAGeneFeature)
        Return From line As String
               In filepath.ReadAllLines
               Let cols As String() = line.Split(ASCII.TAB)
               Select New TCGAGeneFeature With {
                   .gene_id = cols(0),
                   .gene_name = cols(1),
                   .type = cols(2)
               }
    End Function

End Class
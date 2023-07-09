Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.DataMining.BinaryTree
Imports Microsoft.VisualBasic.Linq
Imports SMRUCC.genomics.Analysis.HTS.DataFrame

''' <summary>
''' Build a spatial graph via the grap clustering
''' </summary>
Public Module SpatialGraph

    <Extension>
    Public Function CreateGraph(x As Matrix, Optional eq As Double = 0.85, Optional gt As Double = 0) As ClusterTree
        Dim tree As New ClusterTree
        Dim cos As AlignmentComparison = AlignmentComparison.FromMatrix(x.expression, eq, gt)

        For Each gene As DataFrameRow In x.AsEnumerable
            Call ClusterTree.Add(tree, gene.geneID, cos, eq)
        Next

        Return tree
    End Function

    Public Function CorrelationGraph(x As Matrix, y As Matrix, Optional eq As Double = 0.85, Optional gt As Double = 0) As Object

    End Function
End Module

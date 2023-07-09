Imports System.Runtime.CompilerServices
Imports Microsoft.VisualBasic.DataMining.BinaryTree
Imports Microsoft.VisualBasic.Linq
Imports Microsoft.VisualBasic.Math
Imports Microsoft.VisualBasic.Math.LinearAlgebra
Imports Microsoft.VisualBasic.Math.LinearAlgebra.Matrix
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
            Call ClusterTree.Add(tree, gene.geneID, cos, eq, ds:=eq / 2)
        Next

        Return tree
    End Function

    <Extension>
    Private Function Sum(m As IEnumerable(Of Vector), width As Integer) As Vector
        Dim v As Vector = Vector.Zero([Dim]:=width)
        Dim i As Integer = 0

        For Each xi In m
            v += xi
            i += 1
        Next

        Return v / i
    End Function

    Public Iterator Function CorrelationGraph(x As Matrix,
                                              y As Matrix,
                                              Optional eq As Double = 0.85) As IEnumerable(Of (spotX As String(), spotY As String()))
        Dim treeX = x.CreateGraph(eq, 0)
        Dim treeY = y.CreateGraph(eq, 0)
        Dim clusterX = ClusterTree.GetClusters(treeX).ToArray
        Dim clusterY = ClusterTree.GetClusters(treeY) _
            .Select(Function(yi)
                        Dim v = yi.Members.Select(Function(id) y(id).CreateVector).Sum(width:=y.sampleID.Length)
                        Return (yi.Members.ToArray, v)
                    End Function) _
            .ToArray

        For Each xi In clusterX
            Dim vx As Vector = xi.Members.Select(Function(id) x(id).CreateVector).Sum(width:=x.sampleID.Length)
            Dim q = clusterY.AsParallel _
                .Select(Function(yi)
                            Return (yi.ToArray, cos:=SSM(yi.v, vx))
                        End Function) _
                .OrderByDescending(Function(t) t.cos) _
                .Take(3) _
                .Select(Function(a) a.ToArray) _
                .IteratesALL _
                .Distinct _
                .ToArray

            Yield (xi.Members.ToArray, q)
        Next
    End Function
End Module
